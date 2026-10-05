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
    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    public async Task<IActionResult> Index(string? q = null, string? type = null, string? status = null, int? year = null, int? month = null)
    {
        var query = db.PersonalTransactions.AsNoTracking().Where(x => x.UserId == UserId);

        if (!string.IsNullOrWhiteSpace(q))
            query = query.Where(x => x.Description.Contains(q) || x.Category.Contains(q) || (x.AccountName != null && x.AccountName.Contains(q)));
        if (!string.IsNullOrWhiteSpace(type))
            query = query.Where(x => x.Type == type);
        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(x => x.Status == status);
        if (year.HasValue)
            query = query.Where(x => x.Date.Year == year.Value);
        if (month.HasValue)
            query = query.Where(x => x.Date.Month == month.Value);

        ViewBag.Q = q;
        ViewBag.Type = type;
        ViewBag.Status = status;
        ViewBag.Year = year;
        ViewBag.Month = month;

        return View(await query
            .OrderByDescending(x => x.Date).ThenByDescending(x => x.Id)
            .Take(500).ToListAsync());
    }

    [HttpGet]
    public IActionResult Create(string type = "Despesa") =>
        View(new PersonalTransaction { Type = type, Date = DateTime.Today, Status = "Pago", RecurrenceType = "Avulso" });

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(PersonalTransaction model)
    {
        ModelState.Remove(nameof(model.UserId));
        if (!ModelState.IsValid) return View(model);

        model.UserId = UserId;
        if (model.Status == "Pago" && model.PaidAt is null) model.PaidAt = model.Date;

        if (model.RecurrenceType == "Parcelado" && model.InstallmentTotal is > 1)
        {
            var total = Math.Min(model.InstallmentTotal.Value, 120);
            var installmentAmount = decimal.Round(model.Amount / total, 2);

            for (var i = 1; i <= total; i++)
            {
                db.PersonalTransactions.Add(CloneForOccurrence(
                    model,
                    i == total ? model.Amount - installmentAmount * (total - 1) : installmentAmount,
                    model.Date.AddMonths(i - 1),
                    model.DueDate?.AddMonths(i - 1),
                    i == 1 ? model.Status : "Pendente",
                    i,
                    total));
            }
        }
        else if (model.RecurrenceType == "Recorrente" && model.InstallmentTotal is > 1)
        {
            var total = Math.Min(model.InstallmentTotal.Value, 120);
            for (var i = 1; i <= total; i++)
            {
                db.PersonalTransactions.Add(CloneForOccurrence(
                    model,
                    model.Amount,
                    model.Date.AddMonths(i - 1),
                    model.DueDate?.AddMonths(i - 1),
                    i == 1 ? model.Status : "Pendente",
                    i,
                    total));
            }
        }
        else
        {
            db.PersonalTransactions.Add(model);
        }

        db.AuditLogs.Add(new AuditLog
        {
            UserId = UserId,
            Action = "CREATE",
            Entity = "PersonalTransaction",
            Details = $"{model.Type}: {model.Description} - {model.Amount:C}"
        });

        await db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var item = await db.PersonalTransactions.FirstOrDefaultAsync(x => x.Id == id && x.UserId == UserId);
        return item is null ? NotFound() : View(item);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, PersonalTransaction model)
    {
        var item = await db.PersonalTransactions.FirstOrDefaultAsync(x => x.Id == id && x.UserId == UserId);
        if (item is null) return NotFound();

        ModelState.Remove(nameof(model.UserId));
        if (!ModelState.IsValid) return View(model);

        item.Type = model.Type;
        item.Description = model.Description;
        item.Category = model.Category;
        item.Subcategory = model.Subcategory;
        item.Amount = model.Amount;
        item.Date = model.Date;
        item.DueDate = model.DueDate;
        item.PaymentMethod = model.PaymentMethod;
        item.AccountName = model.AccountName;
        item.Status = model.Status;
        item.PaidAt = model.Status == "Pago" ? model.PaidAt ?? DateTime.Today : null;
        item.Notes = model.Notes;

        db.AuditLogs.Add(new AuditLog { UserId = UserId, Action = "UPDATE", Entity = "PersonalTransaction", EntityId = id.ToString(), Details = item.Description });
        await db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkPaid(int id)
    {
        var item = await db.PersonalTransactions.FirstOrDefaultAsync(x => x.Id == id && x.UserId == UserId);
        if (item is not null)
        {
            item.Status = "Pago";
            item.PaidAt = DateTime.Today;
            db.AuditLogs.Add(new AuditLog { UserId = UserId, Action = "PAY", Entity = "PersonalTransaction", EntityId = id.ToString(), Details = item.Description });
            await db.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var item = await db.PersonalTransactions.FirstOrDefaultAsync(x => x.Id == id && x.UserId == UserId);
        if (item is not null)
        {
            db.AuditLogs.Add(new AuditLog { UserId = UserId, Action = "DELETE", Entity = "PersonalTransaction", EntityId = id.ToString(), Details = item.Description });
            db.PersonalTransactions.Remove(item);
            await db.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Index));
    }

    private PersonalTransaction CloneForOccurrence(
        PersonalTransaction source,
        decimal amount,
        DateTime date,
        DateTime? dueDate,
        string status,
        int number,
        int total)
    {
        return new PersonalTransaction
        {
            UserId = UserId,
            Type = source.Type,
            Description = source.Description,
            Category = source.Category,
            Subcategory = source.Subcategory,
            Amount = amount,
            Date = date,
            DueDate = dueDate,
            PaidAt = status == "Pago" ? source.PaidAt ?? date : null,
            PaymentMethod = source.PaymentMethod,
            AccountName = source.AccountName,
            Status = status,
            RecurrenceType = source.RecurrenceType,
            InstallmentNumber = number,
            InstallmentTotal = total,
            Notes = source.Notes
        };
    }
}
