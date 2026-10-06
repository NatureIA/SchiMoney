using Microsoft.EntityFrameworkCore;
using SchiMoney.Web.Data;
using SchiMoney.Web.Models;

namespace SchiMoney.Web.Services;

public class BarbershopRecurringExpenseService(AppDbContext db)
{
    public async Task EnsureCurrentOccurrencesAsync(string userId)
    {
        var today = DateTime.Today;
        var currentMonth = new DateTime(today.Year, today.Month, 1);

        var series = await db.BarbershopRecurringExpenses
            .Where(x => x.UserId == userId && x.StartDate <= today)
            .OrderBy(x => x.StartDate)
            .ToListAsync();

        if (series.Count == 0)
            return;

        var seriesIds = series.Select(x => x.Id).ToList();

        var existingRows = await db.BarbershopExpenses
            .AsNoTracking()
            .Where(x =>
                x.UserId == userId &&
                x.RecurringSeriesId.HasValue &&
                seriesIds.Contains(x.RecurringSeriesId.Value) &&
                x.RecurringOccurrenceKey != null)
            .Select(x => new
            {
                SeriesId = x.RecurringSeriesId!.Value,
                OccurrenceKey = x.RecurringOccurrenceKey!
            })
            .ToListAsync();

        var existing = existingRows
            .Select(x => $"{x.SeriesId}:{x.OccurrenceKey}")
            .ToHashSet();

        var addedAny = false;

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

                if (existing.Contains(uniqueKey))
                    continue;

                var day = Math.Min(item.StartDate.Day, DateTime.DaysInMonth(month.Year, month.Month));

                db.BarbershopExpenses.Add(new BarbershopExpense
                {
                    UserId = item.UserId,
                    Description = item.Description,
                    Category = item.Category,
                    ExpenseType = item.ExpenseType,
                    Amount = item.Amount,
                    PaymentMethod = item.PaymentMethod,
                    Date = new DateTime(month.Year, month.Month, day),
                    DueDate = null,
                    Paid = false,
                    RecurringSeriesId = item.Id,
                    RecurringOccurrenceKey = occurrenceKey,
                    Notes = item.Notes
                });

                existing.Add(uniqueKey);
                addedAny = true;
            }
        }

        if (addedAny)
            await db.SaveChangesAsync();
    }

    public Task<BarbershopRecurringExpense?> GetSeriesAsync(int seriesId, string userId) =>
        db.BarbershopRecurringExpenses
            .FirstOrDefaultAsync(x => x.Id == seriesId && x.UserId == userId);

    public async Task EndSeriesAsync(int seriesId, string userId)
    {
        var series = await db.BarbershopRecurringExpenses
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
            Entity = "BarbershopRecurringExpense",
            EntityId = series.Id.ToString(),
            Details = $"{series.Description} - recorrência encerrada"
        });

        await db.SaveChangesAsync();
    }
}
