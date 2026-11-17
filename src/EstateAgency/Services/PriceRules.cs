namespace EstateAgency.Services;

// Правила предметной области. Здесь нет ни базы данных, ни окон — только арифметика и условия.
// Поэтому эти методы легко проверить отдельно от интерфейса.
public static class PriceRules
{
    public const string ForSale = "В продаже";
    public const string ForRent = "Сдаётся";
    public const string Sold = "Продан";
    public const string Rented = "Сдан";
    public const string Purchase = "Покупка";
    public const string Rent = "Аренда";
    public const string ViewOnly = "Просмотр";
    public const int FirstHour = 10;  // первый показ дня
    public const int LastHour = 19;   // последний показ дня

    // Границы предыдущего календарного месяца для даты day.
    // Пример: day = 05.11.2026 → Start = 01.10.2026, End = 01.11.2026.
    // Start входит в период, End — уже нет.
    public static (DateOnly Start, DateOnly End) PreviousMonth(DateOnly day)
    {
        DateOnly end = new DateOnly(day.Year, day.Month, 1); // 1-е число этого месяца
        DateOnly start = end.AddMonths(-1);                  // 1-е число прошлого месяца
        return (start, end);
    }

    // Комиссия агентства: цена × ставка / 100; скидка 25 %, если в прошлом месяце заявок не было.
    public static decimal Commission(decimal price, decimal ratePercent, bool hadRequests)
    {
        decimal commission = price * ratePercent / 100m;
        if (!hadRequests)
            commission *= 0.75m;
        // Округляем до копеек: половина копейки — вверх.
        return decimal.Round(commission, 2, MidpointRounding.AwayFromZero);
    }

    // Текст о показах: больше 5 — «много», иначе «мало».
    public static string Availability(int viewings) => viewings > 5 ? "много" : "мало";

    // Красная подсветка карточки: осталось 3 показа или меньше.
    public static bool LowStock(int viewings) => viewings <= 3;

    // Открыт ли объект для заявок: проданный или сданный уже недоступен.
    public static bool IsOpen(string status) => status == ForSale || status == ForRent;

    // Основной вид сделки по статусу объекта.
    public static string DealType(string status) =>
        status == ForRent || status == Rented ? Rent : Purchase;

    // Можно ли подать заявку такого типа на объект с таким статусом.
    public static bool TypeAllowed(string status, string type) =>
        IsOpen(status) && (type == ViewOnly || type == DealType(status));

    // Статус объекта после завершённой сделки. null — объект не меняется (просмотр).
    public static string? StatusAfterDeal(string type) =>
        type == Purchase ? Sold : type == Rent ? Rented : null;

    // Проверка времени показа. null — время подходит, иначе текст ошибки.
    public static string? SlotError(DateTime at, DateTime now)
    {
        if (at <= now)
            return "Нельзя назначить показ на прошедшее время.";
        if (at.Minute != 0 || at.Second != 0)
            return "Показы назначаются на начало часа.";
        if (at.Hour < FirstHour || at.Hour > LastHour)
            return $"Показы проводятся с {FirstHour}:00 до {LastHour}:00.";
        return null;
    }

    // Конверсия заявок в сделки, %. Без заявок — 0, а не деление на ноль.
    public static decimal Conversion(int done, int total) =>
        total == 0 ? 0m : decimal.Round(done * 100m / total, 1, MidpointRounding.AwayFromZero);
}
