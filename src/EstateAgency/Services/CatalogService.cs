using EstateAgency.Data;
using EstateAgency.Models;
using EstateAgency.Views;
using Microsoft.EntityFrameworkCore;

namespace EstateAgency.Services;

// Чтение данных для каталога и расчёт текущей комиссии.
public static class CatalogService
{
    // Были ли заявки на объект в прошлом календарном месяце.
    public static async Task<bool> HadRequestsAsync(EstateContext db, int propertyId)
    {
        DateOnly today = DateOnly.FromDateTime(DateTime.Today);
        var (start, end) = PriceRules.PreviousMonth(today);
        DateTime from = start.ToDateTime(TimeOnly.MinValue);
        DateTime to = end.ToDateTime(TimeOnly.MinValue);
        return await db.Requests.AnyAsync(r =>
            r.PropertyId == propertyId && r.CreatedAt >= from && r.CreatedAt < to);
    }

    // Текущая комиссия по объекту для заявки данного типа.
    public static async Task<decimal> GetCommissionAsync(EstateContext db, Property property, RequestType type)
    {
        bool hadRequests = await HadRequestsAsync(db, property.PropertyId);
        return PriceRules.Commission(property.Price, type.CommissionRate, hadRequests);
    }

    // Все карточки каталога.
    public static async Task<List<PropertyCard>> LoadCardsAsync()
    {
        using var db = Db.Create();
        List<RequestType> types = await db.RequestTypes.AsNoTracking().ToListAsync();
        List<Property> properties = await db.Properties
            .Include(p => p.Type)
            .Include(p => p.Status)
            .AsNoTracking()
            .ToListAsync();

        var cards = new List<PropertyCard>();
        foreach (Property p in properties)
        {
            string status = p.Status.StatusName;
            bool open = PriceRules.IsOpen(status);
            RequestType deal = types.Single(t => t.TypeName == PriceRules.DealType(status));
            bool hadRequests = await HadRequestsAsync(db, p.PropertyId);
            decimal commission = PriceRules.Commission(p.Price, deal.CommissionRate, hadRequests);

            cards.Add(new PropertyCard
            {
                Id = p.PropertyId,
                Code = p.PropertyCode,
                Title = p.Title,
                Address = p.Address,
                Type = p.Type.TypeName,
                Status = status,
                IsOpen = open,
                Params = $"Площадь: {p.Area:0.##} м² · комнат: {p.Rooms}",
                Viewings = p.FreeViewings,
                ViewingsText = open ? "Показов: " + PriceRules.Availability(p.FreeViewings) : "Сделка закрыта",
                Price = p.Price,
                PriceText = PriceRules.DealType(status) == PriceRules.Rent
                    ? $"{p.Price:N2} руб./мес." : $"{p.Price:N2} руб.",
                CommissionText = !open ? "" : hadRequests
                    ? $"Комиссия: {commission:N2} руб."
                    : $"Комиссия: {commission:N2} руб. (скидка 25 %)",
                // Подсветка важна только там, где ещё можно записаться на показ.
                Background = UiSupport.Brush(open && PriceRules.LowStock(p.FreeViewings)
                    ? "LowStockBrush" : "NormalBrush"),
                Image = UiSupport.Picture(p.ImagePath)
            });
        }
        return cards;
    }
}
