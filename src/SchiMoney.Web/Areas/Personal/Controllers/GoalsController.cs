using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchiMoney.Web.Data;
using SchiMoney.Web.Models;

namespace SchiMoney.Web.Areas.Personal.Controllers;

[Area("Personal"), Authorize]
public class GoalsController(AppDbContext db) : Controller
{
    public async Task<IActionResult> Index()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        return View(await db.FinancialGoals.AsNoTracking()
            .Where(x => x.UserId == userId && x.Module == "Pessoal")
            .OrderBy(x => x.Deadline).ThenBy(x => x.Name)
            .ToListAsync());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(string name, decimal targetAmount, decimal currentAmount, DateTime? deadline)
    {
        if (!string.IsNullOrWhiteSpace(name) && targetAmount > 0)
        {
            db.FinancialGoals.Add(new FinancialGoal
            {
                UserId = User.FindFirstValue(ClaimTypes.NameIdentifier)!,
                Module = "Pessoal",
                Name = name.Trim(),
                TargetAmount = targetAmount,
                CurrentAmount = Math.Max(0, currentAmount),
                Deadline = deadline
            });
            await db.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(int id, decimal currentAmount)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var goal = await db.FinancialGoals.FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId && x.Module == "Pessoal");
        if (goal is not null)
        {
            goal.CurrentAmount = Math.Max(0, currentAmount);
            await db.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var goal = await db.FinancialGoals.FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId && x.Module == "Pessoal");
        if (goal is not null) { db.Remove(goal); await db.SaveChangesAsync(); }
        return RedirectToAction(nameof(Index));
    }
}
