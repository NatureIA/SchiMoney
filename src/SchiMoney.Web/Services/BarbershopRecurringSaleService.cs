using Microsoft.EntityFrameworkCore;
using SchiMoney.Web.Data;
using SchiMoney.Web.Models;

namespace SchiMoney.Web.Services;

public class BarbershopRecurringSaleService(AppDbContext db)
{
    public async Task EnsureCurrentOccurrencesAsync(string userId)
    {
        var today = DateTime.Today;
        var currentMonth = new DateTime(today.Year, today.Month, 1);

        var series = await db.BarbershopRecurringSales
            .Where(x => x.UserId == userId && x.StartDate <= today)
            .OrderBy(x => x.StartDate)
            .ToListAsync();

        if (series.Count == 0)
            return;

        var seriesIds = series.Select(x => x.Id).ToList();

        var existingRows = await db.BarbershopSales
            .AsNoTracking()
            .Where(x => x.UserId == userId &&
                        x.RecurringSeriesId.HasValue &&
                        seriesIds.Contains(x.RecurringSeriesId.Value) &&
                        x.RecurringOccurrenceKey != null)
            .Select(x => new
            {
                SeriesId = x.RecurringSeriesId!.Value,
                OccurrenceKey = x.RecurringOccurrenceKey!
            })
            .ToListAsync();

        var existingKeys = existingRows
            .Select(x => $"{x.SeriesId}:{x.OccurrenceKey}")
            .ToHashSet();

        foreach (var recurring in series)
        {
            var startMonth = new DateTime(recurring.StartDate.Year, recurring.StartDate.Month, 1);
            var lastMonth = currentMonth;

            if (recurring.EndDate.HasValue)
            {
                var endMonth = new DateTime(
                    recurring.EndDate.Value.Year,
                    recurring.EndDate.Value.Month,
                    1);

                if (endMonth < lastMonth)
                    lastMonth = endMonth;
            }

            if (lastMonth < startMonth)
                continue;

            for (var month = startMonth; month <= lastMonth; month = month.AddMonths(1))
            {
                var occurrenceKey = month.ToString("yyyy-MM");
                var key = $"{recurring.Id}:{occurrenceKey}";

                if (existingKeys.Contains(key))
                    continue;

                var day = Math.Min(
                    recurring.StartDate.Day,
                    DateTime.DaysInMonth(month.Year, month.Month));

                db.BarbershopSales.Add(new BarbershopSale
                {
                    UserId = recurring.UserId,
                    ServiceName = recurring.ServiceName,
                    Amount = recurring.Amount,
                    PaymentMethod = recurring.PaymentMethod,
                    Date = new DateTime(month.Year, month.Month, day),
                    CustomerName = recurring.CustomerName,
                    Notes = recurring.Notes,
                    RecurringSeriesId = recurring.Id,
                    RecurringOccurrenceKey = occurrenceKey
                });

                existingKeys.Add(key);
            }
        }

        if (db.ChangeTracker.HasChanges())
            await db.SaveChangesAsync();
    }

    public Task<BarbershopRecurringSale?> GetSeriesAsync(int seriesId, string userId) =>
        db.BarbershopRecurringSales
            .FirstOrDefaultAsync(x => x.Id == seriesId && x.UserId == userId);

    public async Task EndSeriesAsync(int seriesId, string userId)
    {
        var recurring = await db.BarbershopRecurringSales
            .FirstOrDefaultAsync(x => x.Id == seriesId && x.UserId == userId);

        if (recurring is null || !recurring.Active)
            return;

        recurring.Active = false;
        recurring.EndDate = DateTime.Today;
        recurring.UpdatedAt = DateTime.UtcNow;

        db.AuditLogs.Add(new AuditLog
        {
            UserId = userId,
            Action = "END",
            Entity = "BarbershopRecurringSale",
            EntityId = recurring.Id.ToString(),
            Details = $"{recurring.ServiceName} - recorrência encerrada"
        });

        await db.SaveChangesAsync();
    }
}
