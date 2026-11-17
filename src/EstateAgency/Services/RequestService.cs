using EstateAgency.Data;
using EstateAgency.Models;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace EstateAgency.Services;

// Все операции с заявками и показами. Права проверяются здесь, а не только скрытием кнопок.
public static class RequestService
{
    private const string New = "Новая";
    private const string InWork = "В работе";
    private const string Done = "Завершена";
    private const string Canceled = "Отменена";

    private static Task<int> StatusIdAsync(EstateContext db, string name) =>
        db.RequestStatuses.Where(s => s.StatusName == name).Select(s => s.StatusId).SingleAsync();

    // Менеджер работает только с заявками по своим объектам, администратор — со всеми.
    private static void RequireProcess(Property property)
    {
        Session.RequireStaff();
        if (!Session.IsAdmin && property.ManagerId != Session.CurrentUser!.UserId)
            throw new AppException("Это заявка по объекту другого менеджера.");
    }

    // Нарушено ограничение UNIQUE (менеджер, время): время занял другой пользователь.
    private static bool IsSlotConflict(DbUpdateException error) =>
        error.InnerException is SqlException sql && (sql.Number == 2627 || sql.Number == 2601);

    // Свободные часы менеджера на дату: прошедшие и занятые не предлагаются.
    public static async Task<List<string>> GetFreeHoursAsync(int managerId, DateOnly date)
    {
        DateTime from = date.ToDateTime(TimeOnly.MinValue);
        DateTime to = from.AddDays(1);
        using var db = Db.Create();
        List<DateTime> busy = await db.Viewings
            .Where(v => v.ManagerId == managerId && v.ScheduledAt >= from && v.ScheduledAt < to)
            .Select(v => v.ScheduledAt)
            .ToListAsync();

        var free = new List<string>();
        for (int hour = PriceRules.FirstHour; hour <= PriceRules.LastHour; hour++)
        {
            DateTime at = from.AddHours(hour);
            if (at > DateTime.Now && !busy.Contains(at))
                free.Add($"{hour:00}:00");
        }
        return free;
    }

    // ---------- 1. Оформление заявки с первым показом. Возвращает номер заявки. ----------
    public static async Task<int> CreateRequestAsync(int propertyId, int typeId, int clientId,
        DateTime scheduledAt, decimal shownCommission, string? notes)
    {
        // Проверки, которым не нужна база данных.
        if (Session.CurrentUser == null)
            throw new AppException("Гость не может подать заявку. Войдите в систему.");
        if (!Session.IsStaff && clientId != Session.CurrentUser.UserId)
            throw new AppException("Можно подать заявку только на себя.");
        string? slotError = PriceRules.SlotError(scheduledAt, DateTime.Now);
        if (slotError != null)
            throw new AppException(slotError);

        using var db = Db.Create();
        // Транзакция: всё, что ниже, сохранится ВМЕСТЕ или не сохранится ВООБЩЕ.
        using var transaction = await db.Database.BeginTransactionAsync();

        Property? property = await db.Properties
            .Include(p => p.Status)
            .AsNoTracking()
            .SingleOrDefaultAsync(p => p.PropertyId == propertyId);
        if (property == null)
            throw new AppException("Объект не найден. Вернитесь в каталог.");
        RequestType? type = await db.RequestTypes.FindAsync(typeId);
        if (type == null)
            throw new AppException("Выберите тип заявки.");
        if (!PriceRules.IsOpen(property.Status.StatusName))
            throw new AppException("Объект уже недоступен: сделка закрыта.");
        if (!PriceRules.TypeAllowed(property.Status.StatusName, type.TypeName))
            throw new AppException($"Для объекта со статусом «{property.Status.StatusName}» нельзя подать заявку «{type.TypeName}».");

        // Контроль дублей: одна активная заявка клиента на один объект.
        bool duplicate = await db.Requests.AnyAsync(r =>
            r.ClientId == clientId && r.PropertyId == propertyId &&
            (r.Status.StatusName == New || r.Status.StatusName == InWork));
        if (duplicate)
            throw new AppException("По этому объекту у клиента уже есть активная заявка.");

        // Пересчитываем комиссию: вдруг она изменилась, пока форма была открыта.
        decimal commission = await CatalogService.GetCommissionAsync(db, property, type);
        if (commission != shownCommission)
            throw new AppException("Комиссия изменилась. Нажмите «Обновить карточку».");

        // Занятое время отклоняем сразу с понятным текстом.
        if (await db.Viewings.AnyAsync(v => v.ManagerId == property.ManagerId && v.ScheduledAt == scheduledAt))
            throw new AppException("Это время у менеджера уже занято. Выберите другое.");

        // Уменьшаем остаток ОДНОЙ командой SQL и только если он есть:
        // UPDATE Properties SET free_viewings = free_viewings - 1
        // WHERE property_id = ... AND free_viewings >= 1 AND status_id = ...
        int changed = await db.Properties
            .Where(p => p.PropertyId == propertyId && p.FreeViewings >= 1 && p.StatusId == property.StatusId)
            .ExecuteUpdateAsync(s => s.SetProperty(p => p.FreeViewings, p => p.FreeViewings - 1));
        if (changed == 0) // ни одна строка не изменилась — показов не осталось
            throw new AppException("Показы по этому объекту закончились. Обновите карточку.");

        DateTime now = DateTime.Now;
        var request = new Request
        {
            ClientId = clientId,
            PropertyId = propertyId,
            TypeId = typeId,
            StatusId = await StatusIdAsync(db, New),
            CommissionAmount = commission, // запоминаем комиссию на момент заявки
            Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim(),
            CreatedAt = now,
            UpdatedAt = now
        };
        request.Viewings.Add(new Viewing { ManagerId = property.ManagerId, ScheduledAt = scheduledAt });
        db.Requests.Add(request);
        try
        {
            await db.SaveChangesAsync();  // INSERT в Requests, затем в Viewings
        }
        catch (DbUpdateException error) when (IsSlotConflict(error))
        {
            throw new AppException("Это время только что занял другой пользователь. Выберите другое.");
        }
        await transaction.CommitAsync();  // заявка, показ и списание остатка — вместе
        return request.RequestId;
    }

    // ---------- 2. Список заявок: клиент — свои, менеджер — по своим объектам, админ — все. ----------
    public static async Task<List<Request>> GetRequestsAsync()
    {
        if (Session.CurrentUser == null)
            throw new AppException("Войдите в систему.");
        int me = Session.CurrentUser.UserId;
        using var db = Db.Create();
        IQueryable<Request> query = db.Requests
            .Include(r => r.Client)
            .Include(r => r.Property)
            .Include(r => r.Type)
            .Include(r => r.Status);
        if (Session.IsStaff && !Session.IsAdmin)
            query = query.Where(r => r.Property.ManagerId == me);
        else if (!Session.IsStaff)
            query = query.Where(r => r.ClientId == me);
        return await query
            .OrderByDescending(r => r.CreatedAt)
            .ThenByDescending(r => r.RequestId)
            .AsNoTracking()
            .ToListAsync();
    }

    // ---------- 3. Одна заявка с показами. null — если заявки уже нет. ----------
    public static async Task<Request?> GetRequestAsync(int requestId)
    {
        if (Session.CurrentUser == null)
            throw new AppException("Войдите в систему.");
        using var db = Db.Create();
        Request? request = await db.Requests
            .Include(r => r.Client)
            .Include(r => r.Property).ThenInclude(p => p.Manager)
            .Include(r => r.Type)
            .Include(r => r.Status)
            .Include(r => r.Viewings).ThenInclude(v => v.Manager)
            .AsNoTracking()
            .SingleOrDefaultAsync(r => r.RequestId == requestId);
        if (request == null)
            return null;
        int me = Session.CurrentUser.UserId;
        bool allowed = Session.IsAdmin
            || (Session.IsStaff && request.Property.ManagerId == me)
            || request.ClientId == me;
        if (!allowed)
            throw new AppException("Нет доступа к этой заявке.");
        return request;
    }

    // ---------- 4. Назначить ещё один показ по заявке (сотрудник). ----------
    public static async Task AddViewingAsync(int requestId, DateTime scheduledAt)
    {
        Session.RequireStaff();
        string? slotError = PriceRules.SlotError(scheduledAt, DateTime.Now);
        if (slotError != null)
            throw new AppException(slotError);

        using var db = Db.Create();
        using var transaction = await db.Database.BeginTransactionAsync();
        Request? request = await db.Requests
            .Include(r => r.Property)
            .Include(r => r.Status)
            .SingleOrDefaultAsync(r => r.RequestId == requestId);
        if (request == null)
            throw new AppException("Заявка уже удалена. Обновите список.");
        RequireProcess(request.Property);
        if (request.Status.StatusName != New && request.Status.StatusName != InWork)
            throw new AppException("Заявка закрыта: новый показ назначить нельзя.");
        int managerId = request.Property.ManagerId;
        if (await db.Viewings.AnyAsync(v => v.ManagerId == managerId && v.ScheduledAt == scheduledAt))
            throw new AppException("Это время у менеджера уже занято. Выберите другое.");

        int changed = await db.Properties
            .Where(p => p.PropertyId == request.PropertyId && p.FreeViewings >= 1)
            .ExecuteUpdateAsync(s => s.SetProperty(p => p.FreeViewings, p => p.FreeViewings - 1));
        if (changed == 0)
            throw new AppException("Показы по этому объекту закончились.");

        db.Viewings.Add(new Viewing { RequestId = requestId, ManagerId = managerId, ScheduledAt = scheduledAt });
        request.UpdatedAt = DateTime.Now;
        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateException error) when (IsSlotConflict(error))
        {
            throw new AppException("Это время только что занял другой пользователь. Выберите другое.");
        }
        await transaction.CommitAsync();
    }

    // ---------- 5. Перенести показ (только администратор). ----------
    public static async Task ChangeViewingDateAsync(int viewingId, DateTime newTime)
    {
        Session.RequireAdmin();
        string? slotError = PriceRules.SlotError(newTime, DateTime.Now);
        if (slotError != null)
            throw new AppException(slotError);

        using var db = Db.Create();
        Viewing? viewing = await db.Viewings.FindAsync(viewingId);
        if (viewing == null)
            throw new AppException("Показ уже удалён. Обновите страницу.");
        if (viewing.ActualStartAt != null)
            throw new AppException("Проведённый показ перенести нельзя.");
        if (await db.Viewings.AnyAsync(v => v.ManagerId == viewing.ManagerId
                && v.ScheduledAt == newTime && v.ViewingId != viewingId))
            throw new AppException("Это время у менеджера уже занято. Выберите другое.");
        viewing.ScheduledAt = newTime;
        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateException error) when (IsSlotConflict(error))
        {
            throw new AppException("Это время только что занял другой пользователь. Выберите другое.");
        }
    }

    // ---------- 6. Удалить показ (только администратор) с возвратом в остаток. ----------
    // Если показов в заявке не осталось — удаляется и сама заявка.
    public static async Task DeleteViewingAsync(int viewingId)
    {
        Session.RequireAdmin();
        using var db = Db.Create();
        using var transaction = await db.Database.BeginTransactionAsync();
        Viewing? viewing = await db.Viewings
            .Include(v => v.Request)
            .AsNoTracking()
            .SingleOrDefaultAsync(v => v.ViewingId == viewingId);
        if (viewing == null)
            throw new AppException("Показ уже удалён. Обновите страницу.");
        if (viewing.ActualStartAt != null)
            throw new AppException("Проведённый показ удалить нельзя: это история работы с клиентом.");

        int requestId = viewing.RequestId;
        int propertyId = viewing.Request.PropertyId;
        // Удаляем одной командой: при повторной попытке изменится 0 строк, и остаток не вернётся дважды.
        int deleted = await db.Viewings.Where(v => v.ViewingId == viewingId).ExecuteDeleteAsync();
        if (deleted == 0)
            throw new AppException("Показ уже удалён. Обновите страницу.");
        await db.Properties
            .Where(p => p.PropertyId == propertyId)
            .ExecuteUpdateAsync(s => s.SetProperty(p => p.FreeViewings, p => p.FreeViewings + 1));

        if (!await db.Viewings.AnyAsync(v => v.RequestId == requestId))
            await db.Requests.Where(r => r.RequestId == requestId).ExecuteDeleteAsync();
        await transaction.CommitAsync();
    }

    // Отменить заявку внутри открытой транзакции: статус и возврат запланированных показов.
    // Возвращает false, если заявка уже закрыта другим пользователем.
    private static async Task<bool> CancelCoreAsync(EstateContext db, int requestId, int propertyId,
        int canceledId, int newId, int inWorkId)
    {
        // Условие на статус защищает от двойной отмены: второй раз изменится 0 строк.
        int changed = await db.Requests
            .Where(r => r.RequestId == requestId && (r.StatusId == newId || r.StatusId == inWorkId))
            .ExecuteUpdateAsync(s => s
                .SetProperty(r => r.StatusId, canceledId)
                .SetProperty(r => r.UpdatedAt, DateTime.Now));
        if (changed == 0)
            return false;
        int returned = await db.Viewings
            .Where(v => v.RequestId == requestId && v.ActualStartAt == null)
            .ExecuteDeleteAsync();
        if (returned > 0)
            await db.Properties
                .Where(p => p.PropertyId == propertyId)
                .ExecuteUpdateAsync(s => s.SetProperty(p => p.FreeViewings, p => p.FreeViewings + returned));
        return true;
    }

    // ---------- 7. Отмена заявки. Клиент — только свою новую, сотрудник — по правилам объекта. ----------
    public static async Task CancelRequestAsync(int requestId)
    {
        if (Session.CurrentUser == null)
            throw new AppException("Войдите в систему.");
        using var db = Db.Create();
        using var transaction = await db.Database.BeginTransactionAsync();
        Request? request = await db.Requests
            .Include(r => r.Property)
            .Include(r => r.Status)
            .AsNoTracking()
            .SingleOrDefaultAsync(r => r.RequestId == requestId);
        if (request == null)
            throw new AppException("Заявка уже удалена. Обновите список.");
        if (Session.IsStaff)
            RequireProcess(request.Property);
        else if (request.ClientId != Session.CurrentUser.UserId)
            throw new AppException("Можно отменить только свою заявку.");
        else if (request.Status.StatusName == InWork)
            throw new AppException("Заявка уже в работе: для отмены обратитесь к менеджеру.");

        bool ok = await CancelCoreAsync(db, requestId, request.PropertyId,
            await StatusIdAsync(db, Canceled), await StatusIdAsync(db, New), await StatusIdAsync(db, InWork));
        if (!ok)
            throw new AppException("Заявка уже закрыта. Обновите список.");
        await transaction.CommitAsync();
    }

    // ---------- 8. Записать результат проведённого показа (сотрудник). ----------
    public static async Task RecordResultAsync(int viewingId, DateTime start, DateTime end,
        string result, string? feedback)
    {
        Session.RequireStaff();
        if (string.IsNullOrWhiteSpace(result))
            throw new AppException("Укажите результат показа.");
        if (end < start)
            throw new AppException("Окончание показа не может быть раньше начала.");
        if (start > DateTime.Now)
            throw new AppException("Нельзя записать результат показа, который ещё не начался.");

        using var db = Db.Create();
        Viewing? viewing = await db.Viewings
            .Include(v => v.Request).ThenInclude(r => r.Property)
            .Include(v => v.Request).ThenInclude(r => r.Status)
            .SingleOrDefaultAsync(v => v.ViewingId == viewingId);
        if (viewing == null)
            throw new AppException("Показ уже удалён. Обновите страницу.");
        RequireProcess(viewing.Request.Property);
        viewing.ActualStartAt = start;
        viewing.ActualEndAt = end;
        viewing.Result = result.Trim();
        viewing.Feedback = string.IsNullOrWhiteSpace(feedback) ? null : feedback.Trim();
        // После первого проведённого показа заявка переходит в работу.
        if (viewing.Request.Status.StatusName == New)
            viewing.Request.StatusId = await StatusIdAsync(db, InWork);
        viewing.Request.UpdatedAt = DateTime.Now;
        await db.SaveChangesAsync();
    }

    // ---------- 9. Завершить сделку: заявка, статус объекта и чужие заявки меняются вместе. ----------
    public static async Task CompleteAsync(int requestId)
    {
        Session.RequireStaff();
        using var db = Db.Create();
        using var transaction = await db.Database.BeginTransactionAsync();
        Request? request = await db.Requests
            .Include(r => r.Property)
            .Include(r => r.Type)
            .Include(r => r.Status)
            .Include(r => r.Viewings)
            .AsNoTracking()
            .SingleOrDefaultAsync(r => r.RequestId == requestId);
        if (request == null)
            throw new AppException("Заявка уже удалена. Обновите список.");
        RequireProcess(request.Property);
        if (request.Status.StatusName != New && request.Status.StatusName != InWork)
            throw new AppException("Заявка уже закрыта.");
        // Переход по этапам возможен только при выполнении условия: был хотя бы один показ.
        if (!request.Viewings.Any(v => v.ActualStartAt != null))
            throw new AppException("Сначала проведите показ и запишите его результат.");

        int newId = await StatusIdAsync(db, New);
        int inWorkId = await StatusIdAsync(db, InWork);
        int canceledId = await StatusIdAsync(db, Canceled);
        int doneId = await StatusIdAsync(db, Done);

        string? nextStatus = PriceRules.StatusAfterDeal(request.Type.TypeName);
        if (nextStatus != null)
        {
            // Объект закрывается одной условной командой: продать его дважды нельзя.
            int nextId = await db.PropertyStatuses.Where(s => s.StatusName == nextStatus)
                .Select(s => s.StatusId).SingleAsync();
            int closed = await db.Properties
                .Where(p => p.PropertyId == request.PropertyId
                    && (p.Status.StatusName == PriceRules.ForSale || p.Status.StatusName == PriceRules.ForRent))
                .ExecuteUpdateAsync(s => s.SetProperty(p => p.StatusId, nextId));
            if (closed == 0)
                throw new AppException("Объект уже недоступен: сделка закрыта другим менеджером.");

            // Остальные активные заявки по объекту отменяются, их показы возвращаются.
            List<int> others = await db.Requests
                .Where(r => r.PropertyId == request.PropertyId && r.RequestId != requestId
                    && (r.StatusId == newId || r.StatusId == inWorkId))
                .Select(r => r.RequestId)
                .ToListAsync();
            foreach (int other in others)
                await CancelCoreAsync(db, other, request.PropertyId, canceledId, newId, inWorkId);
        }

        // Запланированные показы самой заявки больше не нужны.
        int returned = await db.Viewings
            .Where(v => v.RequestId == requestId && v.ActualStartAt == null)
            .ExecuteDeleteAsync();
        if (returned > 0)
            await db.Properties.Where(p => p.PropertyId == request.PropertyId)
                .ExecuteUpdateAsync(s => s.SetProperty(p => p.FreeViewings, p => p.FreeViewings + returned));

        await db.Requests.Where(r => r.RequestId == requestId)
            .ExecuteUpdateAsync(s => s
                .SetProperty(r => r.StatusId, doneId)
                .SetProperty(r => r.UpdatedAt, DateTime.Now));
        await transaction.CommitAsync();
    }
}
