using EstateAgency.Data;
using EstateAgency.Models;
using Microsoft.EntityFrameworkCore;

namespace EstateAgency.Services;

// Управление каталогом объектов: добавить, изменить, удалить.
public static class PropertyService
{
    public static async Task<List<Property>> GetPropertiesAsync()
    {
        Session.RequireStaff();
        int me = Session.CurrentUser!.UserId;
        using var db = Db.Create();
        IQueryable<Property> query = db.Properties.Include(p => p.Status).Include(p => p.Manager);
        if (!Session.IsAdmin)
            query = query.Where(p => p.ManagerId == me); // менеджер видит только свои объекты
        return await query.OrderBy(p => p.PropertyCode).AsNoTracking().ToListAsync();
    }

    public static async Task<List<PropertyType>> GetTypesAsync()
    {
        using var db = Db.Create();
        return await db.PropertyTypes.OrderBy(t => t.TypeName).AsNoTracking().ToListAsync();
    }

    public static async Task<List<PropertyStatus>> GetStatusesAsync()
    {
        using var db = Db.Create();
        return await db.PropertyStatuses.OrderBy(s => s.StatusId).AsNoTracking().ToListAsync();
    }

    public static async Task<List<User>> GetManagersAsync()
    {
        Session.RequireStaff();
        using var db = Db.Create();
        return await db.Users.Where(u => u.Role.RoleName == "Менеджер")
            .OrderBy(u => u.LastName).AsNoTracking().ToListAsync();
    }

    // Сохранить объект: PropertyId = 0 — новый, иначе изменение существующего.
    public static async Task<int> SaveAsync(Property input)
    {
        Session.RequireStaff();
        input.PropertyCode = input.PropertyCode.Trim();
        input.Title = input.Title.Trim();
        input.Address = input.Address.Trim();
        // Обязательные поля: без адреса, площади, цены и статуса карточка не сохраняется.
        if (input.PropertyCode.Length == 0 || input.Title.Length == 0 || input.Address.Length == 0)
            throw new AppException("Заполните код, название и адрес.");
        if (input.Price <= 0)
            throw new AppException("Цена должна быть больше нуля.");
        if (input.Area <= 0)
            throw new AppException("Площадь должна быть больше нуля.");
        if (input.Rooms < 0)
            throw new AppException("Число комнат не может быть отрицательным.");
        if (input.FreeViewings < 0)
            throw new AppException("Число показов не может быть отрицательным.");

        int me = Session.CurrentUser!.UserId;
        using var db = Db.Create();
        // Контроль дублей по коду и адресу.
        if (await db.Properties.AnyAsync(p => p.PropertyCode == input.PropertyCode && p.PropertyId != input.PropertyId))
            throw new AppException("Объект с таким кодом уже есть.");
        if (await db.Properties.AnyAsync(p => p.Address == input.Address && p.PropertyId != input.PropertyId))
            throw new AppException("Объект с таким адресом уже есть. Используйте существующую карточку.");

        Property target;
        if (input.PropertyId == 0)
        {
            target = new Property { CreatedAt = DateTime.Now, ManagerId = me };
            db.Properties.Add(target);
        }
        else
        {
            target = await db.Properties.FindAsync(input.PropertyId)
                ?? throw new AppException("Объект уже удалён. Обновите список.");
            if (!Session.IsAdmin && target.ManagerId != me)
                throw new AppException("Это объект другого менеджера.");
        }
        if (Session.IsAdmin)
            target.ManagerId = input.ManagerId; // ответственного назначает администратор

        target.PropertyCode = input.PropertyCode;
        target.Title = input.Title;
        target.Address = input.Address;
        target.TypeId = input.TypeId;
        target.StatusId = input.StatusId;
        target.Price = input.Price;
        target.Area = input.Area;
        target.Rooms = input.Rooms;
        target.FreeViewings = input.FreeViewings;
        target.Description = string.IsNullOrWhiteSpace(input.Description) ? null : input.Description.Trim();
        target.ImagePath = string.IsNullOrWhiteSpace(input.ImagePath) ? null : input.ImagePath.Trim();
        await db.SaveChangesAsync();
        return target.PropertyId;
    }

    // Удалить объект (только администратор). Объект с заявками удалить нельзя.
    public static async Task DeleteAsync(int propertyId)
    {
        Session.RequireAdmin();
        using var db = Db.Create();
        if (await db.Requests.AnyAsync(r => r.PropertyId == propertyId))
            throw new AppException("Нельзя удалить объект: по нему есть заявки.");
        int deleted = await db.Properties.Where(p => p.PropertyId == propertyId).ExecuteDeleteAsync();
        if (deleted == 0)
            throw new AppException("Объект уже удалён. Обновите список.");
    }
}
