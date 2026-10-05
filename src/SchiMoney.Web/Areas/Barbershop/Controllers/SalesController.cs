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

        ViewBag.Q = q; ViewBag.Payment = payment; ViewBag.Year = year; ViewBag.Month = month;

        return View(await query.OrderByDescending(x => x.Date).ThenByDescending(x => x.Id).Take(500).ToListAsync());
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        await LoadServices();
        return View(new BarbershopSale { Date = DateTime.Today });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(BarbershopSale model)
    {
        ModelState.Remove(nameof(model.UserId));
        if (!ModelState.IsValid) { await LoadServices(); return View(model); }

        model.UserId = UserId;
        db.BarbershopSales.Add(model);
        db.AuditLogs.Add(new AuditLog { UserId = UserId, Action = "CREATE", Entity = "BarbershopSale", Details = $"{model.ServiceName} - {model.Amount:C}" });
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
    public async Task<IActionResult> Edit(int id, BarbershopSale model)
    {
        var item = await db.BarbershopSales.FirstOrDefaultAsync(x => x.Id == id && x.UserId == UserId);
        if (item is null) return NotFound();
        ModelState.Remove(nameof(model.UserId));
        if (!ModelState.IsValid) { await LoadServices(); return View(model); }

        item.ServiceName = model.ServiceName;
        item.Amount = model.Amount;
        item.PaymentMethod = model.PaymentMethod;
        item.Date = model.Date;
        item.CustomerName = model.CustomerName;
        item.Notes = model.Notes;
        db.AuditLogs.Add(new AuditLog { UserId = UserId, Action = "UPDATE", Entity = "BarbershopSale", EntityId = id.ToString(), Details = item.ServiceName });
        await db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var item = await db.BarbershopSales.FirstOrDefaultAsync(x => x.Id == id && x.UserId == UserId);
        if (item is not null)
        {
            db.AuditLogs.Add(new AuditLog { UserId = UserId, Action = "DELETE", Entity = "BarbershopSale", EntityId = id.ToString(), Details = item.ServiceName });
            db.Remove(item); await db.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Index));
    }

    private async Task LoadServices() =>
        ViewBag.Services = await db.BarbershopServices.AsNoTracking()
            .Where(x => x.UserId == UserId && x.Active).OrderBy(x => x.Name).ToListAsync();
}
