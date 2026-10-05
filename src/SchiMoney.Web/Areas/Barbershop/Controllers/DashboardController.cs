using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchiMoney.Web.Data;
using SchiMoney.Web.Models;

namespace SchiMoney.Web.Areas.Barbershop.Controllers;

[Area("Barbershop"), Authorize]
public class DashboardController(AppDbContext db) : Controller
{
    public async Task<IActionResult> Index()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var start = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        var end = start.AddMonths(1);

        var sales = db.BarbershopSales.AsNoTracking()
            .Where(x => x.UserId == userId && x.Date >= start && x.Date < end);
        var expenses = db.BarbershopExpenses.AsNoTracking()
            .Where(x => x.UserId == userId && x.Date >= start && x.Date < end);

        var revenue = await sales.SumAsync(x => (decimal?)x.Amount) ?? 0;
        var paidExpenses = await expenses.Where(x => x.Paid).SumAsync(x => (decimal?)x.Amount) ?? 0;
        var count = await sales.CountAsync();

        var elapsed = Math.Max(1, DateTime.Today.Day);
        var days = DateTime.DaysInMonth(DateTime.Today.Year, DateTime.Today.Month);
        var projection = revenue / elapsed * days;

        var categories = await expenses.Where(x => x.Paid)
            .GroupBy(x => x.Category)
            .Select(g => new CategoryTotal(g.Key, g.Sum(x => x.Amount)))
            .OrderByDescending(x => x.Amount)
            .Take(6).ToListAsync();

        var recent = await db.BarbershopSales.AsNoTracking()
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.Date).ThenByDescending(x => x.Id)
            .Take(8).ToListAsync();

        var insight = revenue == 0
            ? "Registre as primeiras vendas para o SchiMoney calcular margem, ticket médio e projeção de fechamento."
            : $"No ritmo atual, o faturamento projetado para o mês é de {projection:C}.";

        return View(new BarbershopDashboardViewModel
        {
            Revenue = revenue,
            Expenses = paidExpenses,
            SalesCount = count,
            ProjectedRevenue = projection,
            ExpenseCategories = categories,
            RecentSales = recent,
            Insight = insight
        });
    }
}
