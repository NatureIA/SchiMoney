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

        var existing = await db.BarbershopSales
            .AsNoTracking()
            .Where(x => x.UserId == userId &&
                        x.RecurringSeriesId != null &&
                        seriesIds.Contains(x.RecurringSeriesId.Value) &&
                        x.RecurringOccurrenceKey != null)
            .Select(x => new
            {
                SeriesId = x.RecurringSeriesId!.Value,
                x.RecurringOccurrenceKey
            })
            .ToListAsync();

        var existingKeys = existing
            .Select(x => $"{x.SeriesId}:{x.RecurringOccurrenceKey}")
            .ToHashSet();

        foreach (var item in series)
        {
            var startMonth = new DateTime(item.StartDate.Year, item.StartDate.Month, 1);
            var lastMonth = currentMonth;

            if (item.EndDate.HasValue)
            {
                var endMonth = new DateTime(item.EndDate.Value.Year, item.EndDate.Value.Month, 1);
                if (endMonth < lastMonth)
                    lastMonth = endMonth;
            }

            if (lastMonth < startMonth)
                continue;

            for (var month = startMonth; month <= lastMonth; month = month.AddMonths(1))
            {
                var occurrenceKey = month.ToString("yyyy-MM");
                var uniqueKey = $"{item.Id}:{occurrenceKey}";

                if (existingKeys.Contains(uniqueKey))
                    continue;

                var day = Math.Min(item.StartDate.Day, DateTime.DaysInMonth(month.Year, month.Month));
                var occurrenceDate = new DateTime(month.Year, month.Month, day);

                db.BarbershopSales.Add(new BarbershopSale
                {
                    UserId = item.UserId,
                    ServiceName = item.ServiceName,
                    Amount = item.Amount,
                    PaymentMethod = item.PaymentMethod,
                    Date = occurrenceDate,
                    CustomerName = item.CustomerName,
                    Notes = item.Notes,
                    RecurringSeriesId = item.Id,
                    RecurringOccurrenceKey = occurrenceKey
                });

                existingKeys.Add(uniqueKey);
            }
        }

        if (db.ChangeTracker.HasChanges())
            await db.SaveChangesAsync();
    }

    public async Task<BarbershopRecurringSale?> GetSeriesAsync(int seriesId, string userId)
    {
        return await db.BarbershopRecurringSales
            .FirstOrDefaultAsync(x => x.Id == seriesId && x.UserId == userId);
    }

    public async Task EndSeriesAsync(int seriesId, string userId)
    {
        var series = await db.BarbershopRecurringSales
            .FirstOrDefaultAsync(x => x.Id == seriesId && x.UserId == userId);

        if (series is null || !series.Active)
            return;

        series.Active = false;
        series.EndDate = DateTime.Today;
        series.UpdatedAt = DateTime.UtcNow;

        db.AuditLogs.Add(new AuditLog
        {
            UserId = userId,
            Action = "END",
            Entity = "BarbershopRecurringSale",
            EntityId = series.Id.ToString(),
            Details = $"{series.ServiceName} - recorrência encerrada"
        });

        await db.SaveChangesAsync();
    }
}
