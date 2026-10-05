using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchiMoney.Web.Data;
using SchiMoney.Web.Models;

namespace SchiMoney.Web.Areas.Barbershop.Controllers;

[Area("Barbershop"), Authorize]
public class GoalsController(AppDbContext db) : Controller
{
    public async Task<IActionResult> Index()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        return View(await db.FinancialGoals.AsNoTracking()
            .Where(x => x.UserId == userId && x.Module == "Barbearia")
            .OrderByDescending(x => x.Deadline).ThenByDescending(x => x.Id)
            .ToListAsync());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(string name, decimal targetAmount, DateTime? deadline)
    {
        if (!string.IsNullOrWhiteSpace(name) && targetAmount > 0)
        {
            db.FinancialGoals.Add(new FinancialGoal
            {
                UserId = User.FindFirstValue(ClaimTypes.NameIdentifier)!,
                Module = "Barbearia",
                Name = name.Trim(),
                TargetAmount = targetAmount,
                Deadline = deadline
            });
            await db.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var goal = await db.FinancialGoals.FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId && x.Module == "Barbearia");
        if (goal is not null) { db.Remove(goal); await db.SaveChangesAsync(); }
        return RedirectToAction(nameof(Index));
    }
}
