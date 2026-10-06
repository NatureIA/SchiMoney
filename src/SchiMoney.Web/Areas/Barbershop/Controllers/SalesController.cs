using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchiMoney.Web.Data;
using SchiMoney.Web.Models;

namespace SchiMoney.Web.Areas.Barbershop.Controllers;

[Area("Barbershop"), Authorize]
public class SalesController(AppDbContext db) : Controller
{
    private static readonly string[] PreferredServices =
    [
        "Barba",
        "Corte",
        "Corte + Barba",
        "Pomada"
    ];

    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    public async Task<IActionResult> Index(string? q = null, string? payment = null, int? year = null, int? month = null)
    {
        var query = db.BarbershopSales.AsNoTracking().Where(x => x.UserId == UserId);
        if (!string.IsNullOrWhiteSpace(q))
            query = query.Where(x => x.ServiceName.Contains(q) || (x.CustomerName != null && x.CustomerName.Contains(q)));
        if (!string.IsNullOrWhiteSpace(payment))
            query = query.Where(x => x.PaymentMethod == payment);
        if (year.HasValue) query = query.Where(x => x.Date.Year == year.Value);
        if (month.HasValue) query = query.Where(x => x.Date.Month == month.Value);

        ViewBag.Q = q;
        ViewBag.Payment = payment;
        ViewBag.Year = year;
        ViewBag.Month = month;

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
    public async Task<IActionResult> Create(BarbershopSale model, string? newServiceName)
    {
        ModelState.Remove(nameof(model.UserId));

        if (model.ServiceName == "__new__")
        {
            if (string.IsNullOrWhiteSpace(newServiceName))
                ModelState.AddModelError(nameof(model.ServiceName), "Informe o nome do novo serviço.");
            else
                model.ServiceName = newServiceName.Trim();
        }

        if (!ModelState.IsValid)
        {
            ViewBag.NewServiceName = newServiceName;
            await LoadServices();
            return View(model);
        }

        model.UserId = UserId;

        await EnsureServiceCatalogAsync(model.ServiceName, model.Amount);

        db.BarbershopSales.Add(model);
        db.AuditLogs.Add(new AuditLog
        {
            UserId = UserId,
            Action = "CREATE",
            Entity = "BarbershopSale",
            Details = $"{model.ServiceName} - {model.Amount:C}"
        });

        await db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var item = await db.BarbershopSales.FirstOrDefaultAsync(x => x.Id == id && x.UserId == UserId);
        if (item is null) return NotFound();

        await LoadServices();
        return View(item);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, BarbershopSale model, string? newServiceName)
    {
        var item = await db.BarbershopSales.FirstOrDefaultAsync(x => x.Id == id && x.UserId == UserId);
        if (item is null) return NotFound();

        ModelState.Remove(nameof(model.UserId));

        if (model.ServiceName == "__new__")
        {
            if (string.IsNullOrWhiteSpace(newServiceName))
                ModelState.AddModelError(nameof(model.ServiceName), "Informe o nome do novo serviço.");
            else
                model.ServiceName = newServiceName.Trim();
        }

        if (!ModelState.IsValid)
        {
            ViewBag.NewServiceName = newServiceName;
            await LoadServices();
            return View(model);
        }

        await EnsureServiceCatalogAsync(model.ServiceName, model.Amount);

        item.ServiceName = model.ServiceName;
        item.Amount = model.Amount;
        item.PaymentMethod = model.PaymentMethod;
        item.Date = model.Date;
        item.CustomerName = model.CustomerName;
        item.Notes = model.Notes;

        db.AuditLogs.Add(new AuditLog
        {
            UserId = UserId,
            Action = "UPDATE",
            Entity = "BarbershopSale",
            EntityId = id.ToString(),
            Details = item.ServiceName
        });

        await db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var item = await db.BarbershopSales.FirstOrDefaultAsync(x => x.Id == id && x.UserId == UserId);
        if (item is not null)
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

    private async Task LoadServices()
    {
        var savedServices = await db.BarbershopServices
            .AsNoTracking()
            .Where(x => x.UserId == UserId && x.Active)
            .ToListAsync();

        var services = new List<BarbershopService>();

        foreach (var preferredName in PreferredServices)
        {
            var saved = savedServices.FirstOrDefault(x => x.Name == preferredName);

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

    private async Task EnsureServiceCatalogAsync(string serviceName, decimal saleAmount)
    {
        if (string.IsNullOrWhiteSpace(serviceName))
            return;

        var existing = await db.BarbershopServices
            .FirstOrDefaultAsync(x => x.UserId == UserId && x.Name == serviceName);

        if (existing is not null)
        {
            if (!existing.Active)
                existing.Active = true;

            return;
        }

        if (PreferredServices.Contains(serviceName) && serviceName == "Pomada")
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
