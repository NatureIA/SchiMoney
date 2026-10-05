using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchiMoney.Web.Data;
using SchiMoney.Web.Models;

namespace SchiMoney.Web.Areas.Barbershop.Controllers;

[Area("Barbershop"), Authorize]
public class ReportsController(AppDbContext db) : Controller
{
    public async Task<IActionResult> Index(int? year = null)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var selectedYear = year ?? DateTime.Today.Year;
        var start = new DateTime(selectedYear, 1, 1);
        var end = start.AddYears(1);

        var sales = await db.BarbershopSales.AsNoTracking()
            .Where(x => x.UserId == userId && x.Date >= start && x.Date < end).ToListAsync();
        var expenses = await db.BarbershopExpenses.AsNoTracking()
            .Where(x => x.UserId == userId && x.Date >= start && x.Date < end && x.Paid).ToListAsync();

        var months = Enumerable.Range(1, 12).Select(m =>
            new MonthlyPoint(
                selectedYear,
                m,
                sales.Where(x => x.Date.Month == m).Sum(x => x.Amount),
                expenses.Where(x => x.Date.Month == m).Sum(x => x.Amount)
            )).ToList();

        return View(new BarbershopReportViewModel
        {
            Year = selectedYear,
            Revenue = sales.Sum(x => x.Amount),
            Expenses = expenses.Sum(x => x.Amount),
            Months = months,
            Services = sales.GroupBy(x => x.ServiceName)
                .Select(g => new CategoryTotal(g.Key, g.Sum(x => x.Amount)))
                .OrderByDescending(x => x.Amount).ToList(),
            ExpensesByCategory = expenses.GroupBy(x => x.Category)
                .Select(g => new CategoryTotal(g.Key, g.Sum(x => x.Amount)))
                .OrderByDescending(x => x.Amount).ToList(),
            PaymentMethods = sales.GroupBy(x => x.PaymentMethod)
                .Select(g => new CategoryTotal(g.Key, g.Sum(x => x.Amount)))
                .OrderByDescending(x => x.Amount).ToList()
        });
    }
}
