using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchiMoney.Web.Data;
using SchiMoney.Web.Models;

namespace SchiMoney.Web.Areas.Personal.Controllers;

[Area("Personal"), Authorize]
public class TransactionsController(AppDbContext db) : Controller
{
    public async Task<IActionResult> Index(string? type = null)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var query = db.PersonalTransactions.AsNoTracking().Where(x => x.UserId == userId);
        if (!string.IsNullOrWhiteSpace(type)) query = query.Where(x => x.Type == type);
        return View(await query.OrderByDescending(x => x.Date).ThenByDescending(x => x.Id).Take(250).ToListAsync());
    }

    [HttpGet]
    public IActionResult Create(string type = "Despesa") =>
        View(new PersonalTransaction { Type = type, Date = DateTime.Today, Status = type == "Receita" ? "Pago" : "Pago" });

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(PersonalTransaction model)
    {
        ModelState.Remove(nameof(model.UserId));
        if (!ModelState.IsValid) return View(model);

        model.UserId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        if (model.Status == "Pago" && model.PaidAt is null) model.PaidAt = model.Date;

        if (model.RecurrenceType == "Parcelado" && model.InstallmentTotal is > 1)
        {
            var total = model.InstallmentTotal.Value;
            var installmentAmount = decimal.Round(model.Amount / total, 2);
            for (var i = 1; i <= total; i++)
            {
                db.PersonalTransactions.Add(new PersonalTransaction
                {
                    UserId = model.UserId,
                    Type = model.Type,
                    Description = model.Description,
                    Category = model.Category,
                    Subcategory = model.Subcategory,
                    Amount = i == total ? model.Amount - installmentAmount * (total - 1) : installmentAmount,
                    Date = model.Date.AddMonths(i - 1),
                    DueDate = model.DueDate?.AddMonths(i - 1),
                    PaymentMethod = model.PaymentMethod,
                    AccountName = model.AccountName,
                    Status = i == 1 ? model.Status : "Pendente",
                    RecurrenceType = "Parcelado",
                    InstallmentNumber = i,
                    InstallmentTotal = total,
                    Notes = model.Notes
                });
            }
        }
        else
        {
            db.PersonalTransactions.Add(model);
        }

        await db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var item = await db.PersonalTransactions.FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId);
        if (item is not null)
        {
            db.PersonalTransactions.Remove(item);
            await db.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Index));
    }
}
