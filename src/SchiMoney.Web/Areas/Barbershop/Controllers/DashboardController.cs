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
        var daysInMonth = DateTime.DaysInMonth(DateTime.Today.Year, DateTime.Today.Month);
        var projection = revenue / elapsed * daysInMonth;

        var goal = await db.FinancialGoals.AsNoTracking()
            .Where(x => x.UserId == userId && x.Module == "Barbearia")
            .OrderByDescending(x => x.Deadline).ThenByDescending(x => x.Id)
            .FirstOrDefaultAsync();

        var target = goal?.TargetAmount ?? 0;
        var daysRemaining = Math.Max(1, daysInMonth - DateTime.Today.Day + 1);
        var dailyNeeded = target > 0 ? Math.Max(0, target - revenue) / daysRemaining : 0;

        var categories = await expenses.Where(x => x.Paid)
            .GroupBy(x => x.Category)
            .Select(g => new CategoryTotal(g.Key, g.Sum(x => x.Amount)))
            .OrderByDescending(x => x.Amount)
            .Take(6).ToListAsync();

        var recent = await db.BarbershopSales.AsNoTracking()
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.Date).ThenByDescending(x => x.Id)
            .Take(8).ToListAsync();

        string insight;
        if (target > 0 && revenue >= target)
            insight = $"Meta de {target:C} atingida. O faturamento atual está {revenue - target:C} acima da meta.";
        else if (target > 0)
            insight = $"Faltam {target - revenue:C} para a meta. O ritmo necessário é de {dailyNeeded:C} por dia até o fim do mês.";
        else if (revenue == 0)
            insight = "Registre as primeiras vendas e uma meta mensal para o SchiMoney calcular margem, ritmo e projeção.";
        else
            insight = $"No ritmo atual, o faturamento projetado para o mês é de {projection:C}.";

        return View(new BarbershopDashboardViewModel
        {
            Revenue = revenue,
            Expenses = paidExpenses,
            SalesCount = count,
            ProjectedRevenue = projection,
            TargetRevenue = target,
            DailyNeeded = dailyNeeded,
            ExpenseCategories = categories,
            RecentSales = recent,
            Insight = insight
        });
    }
}
