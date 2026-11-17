using EstateAgency.Data;
using Microsoft.EntityFrameworkCore;

namespace EstateAgency.Services;

// Одна строка отчёта по менеджеру.
public class ReportRow
{
    public string Manager { get; set; } = "";
    public int Total { get; set; }
    public int Active { get; set; }
    public int Done { get; set; }
    public int Canceled { get; set; }
    public decimal Conversion { get; set; }
    public decimal Commission { get; set; }
    public string ConversionText => $"{Conversion:0.0} %";
    public string CommissionText => $"{Commission:N2} руб.";
}

// Отчёт: заявки и комиссия по менеджерам. Менеджер видит только свою строку.
public static class ReportService
{
    public static async Task<List<ReportRow>> GetReportAsync()
    {
        Session.RequireStaff();
        int me = Session.CurrentUser!.UserId;
        bool all = Session.IsAdmin;
        using var db = Db.Create();
        var rows = await db.Requests
            .Where(r => all || r.Property.ManagerId == me)
            .GroupBy(r => new { r.Property.Manager.LastName, r.Property.Manager.FirstName })
            .Select(g => new
            {
                g.Key.LastName,
                g.Key.FirstName,
                Total = g.Count(),
                Active = g.Count(r => r.Status.StatusName == "Новая" || r.Status.StatusName == "В работе"),
                Done = g.Count(r => r.Status.StatusName == "Завершена"),
                Canceled = g.Count(r => r.Status.StatusName == "Отменена"),
                Commission = g.Where(r => r.Status.StatusName == "Завершена")
                    .Sum(r => (decimal?)r.CommissionAmount) ?? 0m
            })
            .ToListAsync();
        return rows
            .OrderBy(r => r.LastName)
            .Select(r => new ReportRow
            {
                Manager = $"{r.LastName} {r.FirstName}",
                Total = r.Total,
                Active = r.Active,
                Done = r.Done,
                Canceled = r.Canceled,
                Conversion = PriceRules.Conversion(r.Done, r.Total),
                Commission = r.Commission
            })
            .ToList();
    }
}
