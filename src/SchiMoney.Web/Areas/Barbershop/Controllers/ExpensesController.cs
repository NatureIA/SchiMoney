using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchiMoney.Web.Data;
using SchiMoney.Web.Models;
using SchiMoney.Web.Services;

namespace SchiMoney.Web.Areas.Barbershop.Controllers;

[Area("Barbershop"), Authorize]
public class ExpensesController(
    AppDbContext db,
    BarbershopRecurringExpenseService recurringExpenses) : Controller
{
    private static readonly string[] DefaultCategories =
    [
        "Aluguel",
        "Água",
        "Energia",
        "Internet",
        "Produtos",
        "Materiais",
        "Marketing",
        "Impostos",
        "Manutenção",
        "Equipamentos",
        "Taxas"
    ];

    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    public async Task<IActionResult> Index(
        string? q = null,
        string? status = null,
        string? category = null,
        int? year = null,
        int? month = null)
    {
        DateTime? projectionThrough = null;

        if (year.HasValue || month.HasValue)
        {
            projectionThrough = new DateTime(
                year ?? DateTime.Today.Year,
                month ?? (year.HasValue ? 12 : DateTime.Today.Month),
                1);
        }

        await recurringExpenses.EnsureCurrentOccurrencesAsync(
            UserId,
            projectionThrough);

        var query = db.BarbershopExpenses.AsNoTracking().Where(x => x.UserId == UserId);

        if (!string.IsNullOrWhiteSpace(q))
            query = query.Where(x => x.Description.Contains(q));

        if (status == "Pago")
            query = query.Where(x => x.Paid);

        if (status == "Pendente")
            query = query.Where(x => !x.Paid);

        if (!string.IsNullOrWhiteSpace(category))
            query = query.Where(x => x.Category == category);

        if (year.HasValue)
            query = query.Where(x => x.Date.Year == year.Value);

        if (month.HasValue)
            query = query.Where(x => x.Date.Month == month.Value);

        ViewBag.Q = q;
        ViewBag.Status = status;
        ViewBag.Category = category;
        ViewBag.Year = year;
        ViewBag.Month = month;
        ViewBag.Categories = await GetCategoriesAsync();

        var activeRecurringSeriesIds = await db.BarbershopRecurringExpenses
            .AsNoTracking()
            .Where(x => x.UserId == UserId && x.Active)
            .Select(x => x.Id)
            .ToListAsync();

        ViewBag.ActiveRecurringSeriesIds = new HashSet<int>(activeRecurringSeriesIds);

        return View(await query
            .OrderByDescending(x => x.Date)
            .ThenByDescending(x => x.Id)
            .Take(500)
            .ToListAsync());
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        await LoadCategoriesAsync();

        ViewBag.InstallmentCount = 1;
        ViewBag.FirstBoletoDate = string.Empty;
        ViewBag.IsRecurring = false;

        return View(new BarbershopExpense
        {
            Date = DateTime.Today,
            Paid = true,
            Category = DefaultCategories[0],
            PaymentMethod = "Pix"
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        BarbershopExpense model,
        string? newCategoryName,
        int installmentCount = 1,
        DateTime? firstBoletoDate = null,
        bool isRecurring = false)
    {
        ModelState.Remove(nameof(model.UserId));
        ModelState.Remove(nameof(model.Date));
        ModelState.Remove(nameof(model.DueDate));
        ModelState.Remove(nameof(model.InstallmentGroupId));
        ModelState.Remove(nameof(model.InstallmentNumber));
        ModelState.Remove(nameof(model.InstallmentTotal));
        ModelState.Remove(nameof(model.RecurringSeriesId));
        ModelState.Remove(nameof(model.RecurringOccurrenceKey));

        ResolveCustomCategory(model, newCategoryName);

        var isCredit = model.PaymentMethod == "Crédito";
        var isBoleto = model.PaymentMethod == "Boleto";
        var isInstallmentPayment = !isRecurring && (isCredit || isBoleto);

        if (isInstallmentPayment &&
            (installmentCount < 1 || installmentCount > 120))
        {
            ModelState.AddModelError(
                nameof(model.PaymentMethod),
                "Informe entre 1 e 120 parcelas.");
        }

        if (isBoleto && !firstBoletoDate.HasValue)
        {
            ModelState.AddModelError(
                nameof(model.PaymentMethod),
                "Informe a data do primeiro boleto.");
        }

        if (!ModelState.IsValid)
        {
            ViewBag.NewCategoryName = newCategoryName;
            ViewBag.InstallmentCount = installmentCount < 1 ? 1 : installmentCount;
            ViewBag.FirstBoletoDate = firstBoletoDate?.ToString("yyyy-MM-dd") ?? string.Empty;
            ViewBag.IsRecurring = isRecurring;

            await LoadCategoriesAsync(model.Category);
            return View(model);
        }

        model.UserId = UserId;
        model.DueDate = null;

        if (isRecurring)
        {
            var startDate = isCredit
                ? AddMonthsPreservingDay(DateTime.Today, 1)
                : isBoleto
                    ? firstBoletoDate!.Value.Date
                    : DateTime.Today;

            var recurring = new BarbershopRecurringExpense
            {
                UserId = UserId,
                Description = model.Description,
                Category = model.Category,
                ExpenseType = model.ExpenseType,
                Amount = model.Amount,
                PaymentMethod = model.PaymentMethod,
                StartDate = startDate,
                Active = true,
                Notes = model.Notes
            };

            db.BarbershopRecurringExpenses.Add(recurring);
            await db.SaveChangesAsync();

            db.BarbershopExpenses.Add(new BarbershopExpense
            {
                UserId = UserId,
                Description = model.Description,
                Category = model.Category,
                ExpenseType = model.ExpenseType,
                Amount = model.Amount,
                PaymentMethod = model.PaymentMethod,
                Date = startDate,
                DueDate = null,
                Paid = !isCredit && !isBoleto && model.Paid,
                RecurringSeriesId = recurring.Id,
                RecurringOccurrenceKey = startDate.ToString("yyyy-MM"),
                Notes = model.Notes
            });

            db.AuditLogs.Add(new AuditLog
            {
                UserId = UserId,
                Action = "CREATE",
                Entity = "BarbershopRecurringExpense",
                EntityId = recurring.Id.ToString(),
                Details = $"{model.Description} - recorrência mensal de {model.Amount:C} iniciada"
            });

            await db.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        if (!isInstallmentPayment)
        {
            model.Date = DateTime.Today;
            model.InstallmentGroupId = null;
            model.InstallmentNumber = null;
            model.InstallmentTotal = null;
            model.RecurringSeriesId = null;
            model.RecurringOccurrenceKey = null;

            db.BarbershopExpenses.Add(model);

            db.AuditLogs.Add(new AuditLog
            {
                UserId = UserId,
                Action = "CREATE",
                Entity = "BarbershopExpense",
                Details = $"{model.Description} - {model.Amount:C}"
            });
        }
        else
        {
            var total = Math.Clamp(installmentCount, 1, 120);
            var groupId = Guid.NewGuid().ToString("N");
            var installmentAmount = model.Amount;

            var anchorDate = isCredit
                ? DateTime.Today
                : firstBoletoDate!.Value.Date;

            for (var number = 1; number <= total; number++)
            {
                var occurrenceDate = isCredit
                    ? AddMonthsPreservingDay(anchorDate, number)
                    : AddMonthsPreservingDay(anchorDate, number - 1);

                db.BarbershopExpenses.Add(new BarbershopExpense
                {
                    UserId = UserId,
                    Description = model.Description,
                    Category = model.Category,
                    ExpenseType = model.ExpenseType,
                    Amount = installmentAmount,
                    PaymentMethod = model.PaymentMethod,
                    Date = occurrenceDate,
                    DueDate = null,
                    Paid = false,
                    InstallmentGroupId = groupId,
                    InstallmentNumber = number,
                    InstallmentTotal = total,
                    Notes = model.Notes
                });
            }

            db.AuditLogs.Add(new AuditLog
            {
                UserId = UserId,
                Action = "CREATE",
                Entity = "BarbershopExpenseInstallments",
                EntityId = groupId,
                Details = $"{model.Description} - {total}x de {model.Amount:C} via {model.PaymentMethod} (total {(model.Amount * total):C})"
            });
        }

        await db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        await recurringExpenses.EnsureCurrentOccurrencesAsync(UserId);

        var item = await db.BarbershopExpenses
            .FirstOrDefaultAsync(x => x.Id == id && x.UserId == UserId);

        if (item is null)
            return NotFound();

        await LoadRecurrenceState(item);
        await LoadCategoriesAsync(item.Category);

        return View(item);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        int id,
        BarbershopExpense model,
        string? newCategoryName)
    {
        var item = await db.BarbershopExpenses
            .FirstOrDefaultAsync(x => x.Id == id && x.UserId == UserId);

        if (item is null)
            return NotFound();

        ModelState.Remove(nameof(model.UserId));
        ModelState.Remove(nameof(model.Date));
        ModelState.Remove(nameof(model.DueDate));
        ModelState.Remove(nameof(model.InstallmentGroupId));
        ModelState.Remove(nameof(model.InstallmentNumber));
        ModelState.Remove(nameof(model.InstallmentTotal));
        ModelState.Remove(nameof(model.RecurringSeriesId));
        ModelState.Remove(nameof(model.RecurringOccurrenceKey));

        ResolveCustomCategory(model, newCategoryName);

        if (!ModelState.IsValid)
        {
            model.Date = item.Date;
            model.InstallmentGroupId = item.InstallmentGroupId;
            model.InstallmentNumber = item.InstallmentNumber;
            model.InstallmentTotal = item.InstallmentTotal;
            model.RecurringSeriesId = item.RecurringSeriesId;
            model.RecurringOccurrenceKey = item.RecurringOccurrenceKey;

            ViewBag.NewCategoryName = newCategoryName;

            await LoadRecurrenceState(item);
            await LoadCategoriesAsync(model.Category);

            return View(model);
        }

        if (item.RecurringSeriesId.HasValue)
        {
            var seriesId = item.RecurringSeriesId.Value;

            var occurrences = await db.BarbershopExpenses
                .Where(x =>
                    x.UserId == UserId &&
                    x.RecurringSeriesId == seriesId &&
                    x.Date >= item.Date)
                .ToListAsync();

            foreach (var occurrence in occurrences)
            {
                occurrence.Description = model.Description;
                occurrence.Category = model.Category;
                occurrence.ExpenseType = model.ExpenseType;
                occurrence.Amount = model.Amount;
                occurrence.PaymentMethod = model.PaymentMethod;
                occurrence.Notes = model.Notes;
            }

            item.Paid = model.Paid;

            var recurring = await recurringExpenses.GetSeriesAsync(seriesId, UserId);

            if (recurring is not null)
            {
                recurring.Description = model.Description;
                recurring.Category = model.Category;
                recurring.ExpenseType = model.ExpenseType;
                recurring.Amount = model.Amount;
                recurring.PaymentMethod = model.PaymentMethod;
                recurring.Notes = model.Notes;
                recurring.UpdatedAt = DateTime.UtcNow;
            }
        }
        else
        {
            item.Description = model.Description;
            item.Category = model.Category;
            item.ExpenseType = model.ExpenseType;
            item.Amount = model.Amount;
            item.PaymentMethod = model.PaymentMethod;
            item.DueDate = null;
            item.Paid = model.Paid;
            item.Notes = model.Notes;
        }

        db.AuditLogs.Add(new AuditLog
        {
            UserId = UserId,
            Action = "UPDATE",
            Entity = item.RecurringSeriesId.HasValue
                ? "BarbershopRecurringExpense"
                : "BarbershopExpense",
            EntityId = item.RecurringSeriesId?.ToString() ?? id.ToString(),
            Details = model.Description
        });

        await db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> EndRecurring(int seriesId)
    {
        await recurringExpenses.EnsureCurrentOccurrencesAsync(UserId);
        await recurringExpenses.EndSeriesAsync(seriesId, UserId);

        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkPaid(int id)
    {
        var item = await db.BarbershopExpenses
            .FirstOrDefaultAsync(x => x.Id == id && x.UserId == UserId);

        if (item is not null)
        {
            item.Paid = true;

            db.AuditLogs.Add(new AuditLog
            {
                UserId = UserId,
                Action = "PAY",
                Entity = "BarbershopExpense",
                EntityId = id.ToString(),
                Details = item.Description
            });

            await db.SaveChangesAsync();
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var item = await db.BarbershopExpenses
            .FirstOrDefaultAsync(x => x.Id == id && x.UserId == UserId);

        if (item is not null && !item.RecurringSeriesId.HasValue)
        {
            db.AuditLogs.Add(new AuditLog
            {
                UserId = UserId,
                Action = "DELETE",
                Entity = "BarbershopExpense",
                EntityId = id.ToString(),
                Details = item.Description
            });

            db.Remove(item);
            await db.SaveChangesAsync();
        }

        return RedirectToAction(nameof(Index));
    }

    private async Task LoadRecurrenceState(BarbershopExpense item)
    {
        if (!item.RecurringSeriesId.HasValue)
            return;

        var recurring = await recurringExpenses.GetSeriesAsync(
            item.RecurringSeriesId.Value,
            UserId);

        ViewBag.IsRecurring = true;
        ViewBag.RecurrenceActive = recurring?.Active == true;
        ViewBag.RecurrenceEndDate = recurring?.EndDate;
    }

    private void ResolveCustomCategory(
        BarbershopExpense model,
        string? newCategoryName)
    {
        if (model.Category != "__new__")
            return;

        if (string.IsNullOrWhiteSpace(newCategoryName))
        {
            ModelState.AddModelError(
                nameof(model.Category),
                "Informe o nome da nova categoria.");
            return;
        }

        model.Category = newCategoryName.Trim();
    }

    private async Task LoadCategoriesAsync(string? currentCategory = null)
    {
        ViewBag.Categories = await GetCategoriesAsync(currentCategory);
    }

    private async Task<List<string>> GetCategoriesAsync(string? currentCategory = null)
    {
        var savedCategories = await db.BarbershopExpenses
            .AsNoTracking()
            .Where(x =>
                x.UserId == UserId &&
                x.Category != "" &&
                x.Category != "Outros")
            .Select(x => x.Category)
            .Distinct()
            .ToListAsync();

        var categories = DefaultCategories
            .Concat(savedCategories)
            .Where(x => !string.IsNullOrWhiteSpace(x) && x != "Outros")
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (!string.IsNullOrWhiteSpace(currentCategory) &&
            currentCategory != "Outros" &&
            !categories.Contains(currentCategory, StringComparer.OrdinalIgnoreCase))
        {
            categories.Add(currentCategory);
        }

        return categories
            .OrderBy(x =>
            {
                var index = Array.IndexOf(DefaultCategories, x);
                return index < 0 ? int.MaxValue : index;
            })
            .ThenBy(x => x)
            .ToList();
    }

    private static DateTime AddMonthsPreservingDay(DateTime anchor, int months)
    {
        var targetMonth = new DateTime(anchor.Year, anchor.Month, 1).AddMonths(months);
        var day = Math.Min(anchor.Day, DateTime.DaysInMonth(targetMonth.Year, targetMonth.Month));

        return new DateTime(targetMonth.Year, targetMonth.Month, day);
    }
}
