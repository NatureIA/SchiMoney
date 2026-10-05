using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchiMoney.Web.Data;
using SchiMoney.Web.Models;

namespace SchiMoney.Web.Areas.Personal.Controllers;

[Area("Personal"), Authorize]
public class ReportsController(AppDbContext db) : Controller
{
    public async Task<IActionResult> Index(int? year = null)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var selectedYear = year ?? DateTime.Today.Year;
        var start = new DateTime(selectedYear, 1, 1);
        var end = start.AddYears(1);

        var rows = await db.PersonalTransactions.AsNoTracking()
            .Where(x => x.UserId == userId && x.Date >= start && x.Date < end && x.Status == "Pago")
            .ToListAsync();

        var months = Enumerable.Range(1, 12).Select(m =>
        {
            var monthly = rows.Where(x => x.Date.Month == m);
            return new MonthlyPoint(
                selectedYear,
                m,
                monthly.Where(x => x.Type == "Receita").Sum(x => x.Amount),
                monthly.Where(x => x.Type == "Despesa").Sum(x => x.Amount)
            );
        }).ToList();

        var categories = rows.Where(x => x.Type == "Despesa")
            .GroupBy(x => x.Category)
            .Select(g => new CategoryTotal(g.Key, g.Sum(x => x.Amount)))
            .OrderByDescending(x => x.Amount)
            .ToList();

        return View(new PersonalReportViewModel
        {
            Year = selectedYear,
            Income = rows.Where(x => x.Type == "Receita").Sum(x => x.Amount),
            Expenses = rows.Where(x => x.Type == "Despesa").Sum(x => x.Amount),
            Months = months,
            Categories = categories
        });
    }
}
