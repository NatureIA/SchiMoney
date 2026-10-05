using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchiMoney.Web.Data;
using SchiMoney.Web.Models;

namespace SchiMoney.Web.Areas.Personal.Controllers;

[Area("Personal"), Authorize]
public class AccountsController(AppDbContext db) : Controller
{
    public async Task<IActionResult> Index()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        var accounts = await db.PersonalAccounts.AsNoTracking()
            .Where(x => x.UserId == userId && x.Active)
            .OrderBy(x => x.Name).ToListAsync();

        var cards = await db.PersonalCreditCards.AsNoTracking()
            .Where(x => x.UserId == userId && x.Active)
            .OrderBy(x => x.Name).ToListAsync();

        var movements = await db.PersonalTransactions.AsNoTracking()
            .Where(x => x.UserId == userId && x.Status == "Pago" && x.AccountName != null)
            .ToListAsync();

        var balances = accounts.ToDictionary(
            a => a.Id,
            a => a.InitialBalance
               + movements.Where(x => x.AccountName == a.Name && x.Type == "Receita").Sum(x => x.Amount)
               - movements.Where(x => x.AccountName == a.Name && x.Type == "Despesa").Sum(x => x.Amount)
        );

        return View(new PersonalAccountsViewModel
        {
            Accounts = accounts,
            Cards = cards,
            Balances = balances
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateAccount(string name, string type, string? institution, decimal initialBalance)
    {
        if (!string.IsNullOrWhiteSpace(name))
        {
            db.PersonalAccounts.Add(new PersonalAccount
            {
                UserId = User.FindFirstValue(ClaimTypes.NameIdentifier)!,
                Name = name.Trim(),
                Type = string.IsNullOrWhiteSpace(type) ? "Conta digital" : type,
                Institution = institution?.Trim(),
                InitialBalance = initialBalance
            });
            await db.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateCard(string name, string? institution, decimal limit, int closingDay, int dueDay)
    {
        if (!string.IsNullOrWhiteSpace(name) && limit >= 0 && closingDay is >= 1 and <= 31 && dueDay is >= 1 and <= 31)
        {
            db.PersonalCreditCards.Add(new PersonalCreditCard
            {
                UserId = User.FindFirstValue(ClaimTypes.NameIdentifier)!,
                Name = name.Trim(),
                Institution = institution?.Trim(),
                Limit = limit,
                ClosingDay = closingDay,
                DueDay = dueDay
            });
            await db.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteAccount(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var item = await db.PersonalAccounts.FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId);
        if (item is not null) { db.Remove(item); await db.SaveChangesAsync(); }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteCard(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var item = await db.PersonalCreditCards.FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId);
        if (item is not null) { db.Remove(item); await db.SaveChangesAsync(); }
        return RedirectToAction(nameof(Index));
    }
}
