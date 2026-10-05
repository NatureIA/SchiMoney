using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchiMoney.Web.Data;
using SchiMoney.Web.Models;

namespace SchiMoney.Web.Areas.Barbershop.Controllers;

[Area("Barbershop"), Authorize]
public class ExpensesController(AppDbContext db) : Controller
{
    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    public async Task<IActionResult> Index(string? q = null, string? status = null, string? category = null, int? year = null, int? month = null)
    {
        var query = db.BarbershopExpenses.AsNoTracking().Where(x => x.UserId == UserId);
        if (!string.IsNullOrWhiteSpace(q)) query = query.Where(x => x.Description.Contains(q));
        if (status == "Pago") query = query.Where(x => x.Paid);
        if (status == "Pendente") query = query.Where(x => !x.Paid);
        if (!string.IsNullOrWhiteSpace(category)) query = query.Where(x => x.Category == category);
        if (year.HasValue) query = query.Where(x => x.Date.Year == year.Value);
        if (month.HasValue) query = query.Where(x => x.Date.Month == month.Value);

        ViewBag.Q = q; ViewBag.Status = status; ViewBag.Category = category; ViewBag.Year = year; ViewBag.Month = month;
        return View(await query.OrderByDescending(x => x.Date).ThenByDescending(x => x.Id).Take(500).ToListAsync());
    }

    [HttpGet]
    public IActionResult Create() => View(new BarbershopExpense { Date = DateTime.Today, Paid = true });

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(BarbershopExpense model)
    {
        ModelState.Remove(nameof(model.UserId));
        if (!ModelState.IsValid) return View(model);
        model.UserId = UserId;
        db.BarbershopExpenses.Add(model);
        db.AuditLogs.Add(new AuditLog { UserId = UserId, Action = "CREATE", Entity = "BarbershopExpense", Details = $"{model.Description} - {model.Amount:C}" });
        await db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var item = await db.BarbershopExpenses.FirstOrDefaultAsync(x => x.Id == id && x.UserId == UserId);
        return item is null ? NotFound() : View(item);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, BarbershopExpense model)
    {
        var item = await db.BarbershopExpenses.FirstOrDefaultAsync(x => x.Id == id && x.UserId == UserId);
        if (item is null) return NotFound();
        ModelState.Remove(nameof(model.UserId));
        if (!ModelState.IsValid) return View(model);

        item.Description = model.Description;
        item.Category = model.Category;
        item.ExpenseType = model.ExpenseType;
        item.Amount = model.Amount;
        item.PaymentMethod = model.PaymentMethod;
        item.Date = model.Date;
        item.DueDate = model.DueDate;
        item.Paid = model.Paid;
        item.Notes = model.Notes;
        db.AuditLogs.Add(new AuditLog { UserId = UserId, Action = "UPDATE", Entity = "BarbershopExpense", EntityId = id.ToString(), Details = item.Description });
        await db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkPaid(int id)
    {
        var item = await db.BarbershopExpenses.FirstOrDefaultAsync(x => x.Id == id && x.UserId == UserId);
        if (item is not null)
        {
            item.Paid = true;
            db.AuditLogs.Add(new AuditLog { UserId = UserId, Action = "PAY", Entity = "BarbershopExpense", EntityId = id.ToString(), Details = item.Description });
            await db.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var item = await db.BarbershopExpenses.FirstOrDefaultAsync(x => x.Id == id && x.UserId == UserId);
        if (item is not null)
        {
            db.AuditLogs.Add(new AuditLog { UserId = UserId, Action = "DELETE", Entity = "BarbershopExpense", EntityId = id.ToString(), Details = item.Description });
            db.Remove(item); await db.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Index));
    }
}
