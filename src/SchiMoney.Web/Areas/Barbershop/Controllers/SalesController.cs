using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchiMoney.Web.Data;
using SchiMoney.Web.Models;
using SchiMoney.Web.Services;

namespace SchiMoney.Web.Areas.Barbershop.Controllers;

[Area("Barbershop"), Authorize]
public class SalesController(
    AppDbContext db,
    BarbershopRecurringSaleService recurringSales) : Controller
{
    private static readonly string[] PreferredServices =
    [
        "Barba",
        "Corte",
        "Corte + Barba",
        "Pomada"
    ];

    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    public async Task<IActionResult> Index(
        string? q = null,
        string? payment = null,
        int? year = null,
        int? month = null)
    {
        await recurringSales.EnsureCurrentOccurrencesAsync(UserId);

        var query = db.BarbershopSales
            .AsNoTracking()
            .Where(x => x.UserId == UserId);

        if (!string.IsNullOrWhiteSpace(q))
            query = query.Where(x =>
                x.ServiceName.Contains(q) ||
                (x.CustomerName != null && x.CustomerName.Contains(q)));

        if (!string.IsNullOrWhiteSpace(payment))
            query = query.Where(x => x.PaymentMethod == payment);

        if (year.HasValue)
            query = query.Where(x => x.Date.Year == year.Value);

        if (month.HasValue)
            query = query.Where(x => x.Date.Month == month.Value);

        ViewBag.Q = q;
        ViewBag.Payment = payment;
        ViewBag.Year = year;
        ViewBag.Month = month;

        var activeSeriesIds = await db.BarbershopRecurringSales
            .AsNoTracking()
            .Where(x => x.UserId == UserId && x.Active)
            .Select(x => x.Id)
            .ToListAsync();

        ViewBag.ActiveRecurringSeriesIds = new HashSet<int>(activeSeriesIds);

        return View(await query
            .OrderByDescending(x => x.Date)
            .ThenByDescending(x => x.Id)
            .Take(500)
            .ToListAsync());
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        await LoadServices();
        return View(new BarbershopSale { Date = DateTime.Today });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        BarbershopSale model,
        string? newServiceName,
        bool isRecurring = false)
    {
        model.Date = DateTime.Today;

        ModelState.Remove(nameof(model.UserId));
        ModelState.Remove(nameof(model.Date));
        ModelState.Remove(nameof(model.RecurringSeriesId));
        ModelState.Remove(nameof(model.RecurringOccurrenceKey));

        if (model.ServiceName == "__new__")
        {
            if (string.IsNullOrWhiteSpace(newServiceName))
                ModelState.AddModelError(
                    nameof(model.ServiceName),
                    "Informe o nome do novo serviço.");
            else
                model.ServiceName = newServiceName.Trim();
        }

        if (!ModelState.IsValid)
        {
            ViewBag.NewServiceName = newServiceName;
            ViewBag.IsRecurring = isRecurring;
            await LoadServices();
            return View(model);
        }

        model.UserId = UserId;
        await EnsureServiceCatalogAsync(model.ServiceName, model.Amount);

        await using var transaction = await db.Database.BeginTransactionAsync();

        if (isRecurring)
        {
            var recurring = new BarbershopRecurringSale
            {
                UserId = UserId,
                ServiceName = model.ServiceName,
                Amount = model.Amount,
                PaymentMethod = model.PaymentMethod,
                CustomerName = model.CustomerName,
                Notes = model.Notes,
                StartDate = DateTime.Today,
                Active = true
            };

            db.BarbershopRecurringSales.Add(recurring);
            await db.SaveChangesAsync();

            model.RecurringSeriesId = recurring.Id;
            model.RecurringOccurrenceKey = DateTime.Today.ToString("yyyy-MM");

            db.AuditLogs.Add(new AuditLog
            {
                UserId = UserId,
                Action = "CREATE",
                Entity = "BarbershopRecurringSale",
                EntityId = recurring.Id.ToString(),
                Details = $"{model.ServiceName} - recorrência mensal iniciada"
            });
        }

        db.BarbershopSales.Add(model);

        db.AuditLogs.Add(new AuditLog
        {
            UserId = UserId,
            Action = "CREATE",
            Entity = "BarbershopSale",
            Details = $"{model.ServiceName} - {model.Amount:C}"
        });

        await db.SaveChangesAsync();
        await transaction.CommitAsync();

        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        await recurringSales.EnsureCurrentOccurrencesAsync(UserId);

        var item = await db.BarbershopSales
            .FirstOrDefaultAsync(x => x.Id == id && x.UserId == UserId);

        if (item is null)
            return NotFound();

        await LoadRecurrenceState(item);
        await LoadServices();

        return View(item);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        int id,
        BarbershopSale model,
        string? newServiceName)
    {
        var item = await db.BarbershopSales
            .FirstOrDefaultAsync(x => x.Id == id && x.UserId == UserId);

        if (item is null)
            return NotFound();

        ModelState.Remove(nameof(model.UserId));
        ModelState.Remove(nameof(model.Date));
        ModelState.Remove(nameof(model.RecurringSeriesId));
        ModelState.Remove(nameof(model.RecurringOccurrenceKey));

        if (model.ServiceName == "__new__")
        {
            if (string.IsNullOrWhiteSpace(newServiceName))
                ModelState.AddModelError(
                    nameof(model.ServiceName),
                    "Informe o nome do novo serviço.");
            else
                model.ServiceName = newServiceName.Trim();
        }

        if (!ModelState.IsValid)
        {
            model.RecurringSeriesId = item.RecurringSeriesId;
            model.RecurringOccurrenceKey = item.RecurringOccurrenceKey;
            ViewBag.NewServiceName = newServiceName;

            await LoadRecurrenceState(item);
            await LoadServices();

            return View(model);
        }

        await EnsureServiceCatalogAsync(model.ServiceName, model.Amount);

        if (item.RecurringSeriesId.HasValue)
        {
            var seriesId = item.RecurringSeriesId.Value;

            var occurrences = await db.BarbershopSales
                .Where(x =>
                    x.UserId == UserId &&
                    x.RecurringSeriesId == seriesId &&
                    x.Date >= item.Date)
                .ToListAsync();

            foreach (var occurrence in occurrences)
            {
                occurrence.ServiceName = model.ServiceName;
                occurrence.Amount = model.Amount;
                occurrence.PaymentMethod = model.PaymentMethod;
                occurrence.CustomerName = model.CustomerName;
                occurrence.Notes = model.Notes;
            }

            var recurring = await recurringSales.GetSeriesAsync(seriesId, UserId);

            if (recurring is not null)
            {
                recurring.ServiceName = model.ServiceName;
                recurring.Amount = model.Amount;
                recurring.PaymentMethod = model.PaymentMethod;
                recurring.CustomerName = model.CustomerName;
                recurring.Notes = model.Notes;
                recurring.UpdatedAt = DateTime.UtcNow;
            }
        }
        else
        {
            item.ServiceName = model.ServiceName;
            item.Amount = model.Amount;
            item.PaymentMethod = model.PaymentMethod;
            item.CustomerName = model.CustomerName;
            item.Notes = model.Notes;
        }

        db.AuditLogs.Add(new AuditLog
        {
            UserId = UserId,
            Action = "UPDATE",
            Entity = item.RecurringSeriesId.HasValue
                ? "BarbershopRecurringSale"
                : "BarbershopSale",
            EntityId = item.RecurringSeriesId?.ToString() ?? id.ToString(),
            Details = model.ServiceName
        });

        await db.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> EndRecurring(int seriesId)
    {
        await recurringSales.EnsureCurrentOccurrencesAsync(UserId);
        await recurringSales.EndSeriesAsync(seriesId, UserId);

        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var item = await db.BarbershopSales
            .FirstOrDefaultAsync(x => x.Id == id && x.UserId == UserId);

        if (item is not null && !item.RecurringSeriesId.HasValue)
        {
            db.AuditLogs.Add(new AuditLog
            {
                UserId = UserId,
                Action = "DELETE",
                Entity = "BarbershopSale",
                EntityId = id.ToString(),
                Details = item.ServiceName
            });

            db.Remove(item);
            await db.SaveChangesAsync();
        }

        return RedirectToAction(nameof(Index));
    }

    private async Task LoadRecurrenceState(BarbershopSale item)
    {
        if (!item.RecurringSeriesId.HasValue)
            return;

        var recurring = await recurringSales.GetSeriesAsync(
            item.RecurringSeriesId.Value,
            UserId);

        ViewBag.IsRecurring = true;
        ViewBag.RecurrenceActive = recurring?.Active == true;
        ViewBag.RecurrenceEndDate = recurring?.EndDate;
    }

    private async Task LoadServices()
    {
        var savedServices = await db.BarbershopServices
            .AsNoTracking()
            .Where(x => x.UserId == UserId && x.Active)
            .ToListAsync();

        var services = new List<BarbershopService>();

        foreach (var preferredName in PreferredServices)
        {
            var saved = savedServices.FirstOrDefault(
                x => x.Name == preferredName);

            services.Add(saved ?? new BarbershopService
            {
                UserId = UserId,
                Name = preferredName,
                Price = 0,
                Active = true
            });
        }

        services.AddRange(
            savedServices
                .Where(x => !PreferredServices.Contains(x.Name))
                .OrderBy(x => x.Name));

        ViewBag.Services = services;
    }

    private async Task EnsureServiceCatalogAsync(
        string serviceName,
        decimal saleAmount)
    {
        if (string.IsNullOrWhiteSpace(serviceName))
            return;

        var existing = await db.BarbershopServices
            .FirstOrDefaultAsync(x =>
                x.UserId == UserId &&
                x.Name == serviceName);

        if (existing is not null)
        {
            if (!existing.Active)
                existing.Active = true;

            return;
        }

        if (serviceName == "Pomada")
            return;

        db.BarbershopServices.Add(new BarbershopService
        {
            UserId = UserId,
            Name = serviceName.Trim(),
            Price = saleAmount,
            Active = true
        });
    }
}
