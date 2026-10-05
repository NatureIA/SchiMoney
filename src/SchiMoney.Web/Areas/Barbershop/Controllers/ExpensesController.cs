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
    public async Task<IActionResult> Index()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        return View(await db.BarbershopExpenses.AsNoTracking()
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.Date).ThenByDescending(x => x.Id)
            .Take(250).ToListAsync());
    }

    [HttpGet]
    public IActionResult Create() => View(new BarbershopExpense { Date = DateTime.Today, Paid = true });

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(BarbershopExpense model)
    {
        ModelState.Remove(nameof(model.UserId));
        if (!ModelState.IsValid) return View(model);
        model.UserId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        db.BarbershopExpenses.Add(model);
        await db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var item = await db.BarbershopExpenses.FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId);
        if (item is not null) { db.Remove(item); await db.SaveChangesAsync(); }
        return RedirectToAction(nameof(Index));
    }
}
