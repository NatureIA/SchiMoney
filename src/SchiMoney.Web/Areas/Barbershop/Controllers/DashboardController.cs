using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchiMoney.Web.Data;
using SchiMoney.Web.Models;
using SchiMoney.Web.Services;

namespace SchiMoney.Web.Areas.Barbershop.Controllers;

[Area("Barbershop"), Authorize]
public class DashboardController(
    AppDbContext db,
    BarbershopRecurringSaleService recurringSales) : Controller
{
    public async Task<IActionResult> Index(int? year = null, int? month = null)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        await recurringSales.EnsureCurrentOccurrencesAsync(userId);

        var selected = new DateTime(year ?? DateTime.Today.Year, month ?? DateTime.Today.Month, 1);
        var start = selected;
        var end = start.AddMonths(1);

        var sales = await db.BarbershopSales.AsNoTracking()
            .Where(x => x.UserId == userId && x.Date >= start && x.Date < end)
            .OrderByDescending(x => x.Date).ThenByDescending(x => x.Id)
            .ToListAsync();

        var expenses = await db.BarbershopExpenses.AsNoTracking()
            .Where(x => x.UserId == userId && x.Date >= start && x.Date < end)
            .ToListAsync();

        var revenue = sales.Sum(x => x.Amount);
        var paidExpenses = expenses.Where(x => x.Paid).Sum(x => x.Amount);
        var count = sales.Count;

        var isCurrent = selected.Year == DateTime.Today.Year && selected.Month == DateTime.Today.Month;
        var daysInMonth = DateTime.DaysInMonth(selected.Year, selected.Month);
        var elapsed = isCurrent ? Math.Max(1, DateTime.Today.Day) : daysInMonth;
        var projection = revenue / elapsed * daysInMonth;

        var goal = await db.FinancialGoals.AsNoTracking()
            .Where(x => x.UserId == userId && x.Module == "Barbearia" &&
                        (x.Deadline == null || (x.Deadline.Value.Year == selected.Year && x.Deadline.Value.Month == selected.Month)))
            .OrderByDescending(x => x.Deadline).ThenByDescending(x => x.Id)
            .FirstOrDefaultAsync();

        var target = goal?.TargetAmount ?? 0;
        var daysRemaining = isCurrent ? Math.Max(1, daysInMonth - DateTime.Today.Day + 1) : 1;
        var dailyNeeded = target > 0 && isCurrent ? Math.Max(0, target - revenue) / daysRemaining : 0;

        var expenseCategories = expenses.Where(x => x.Paid)
            .GroupBy(x => x.Category)
            .Select(g => new CategoryTotal(g.Key, g.Sum(x => x.Amount)))
            .OrderByDescending(x => x.Amount).Take(7).ToList();

        var topServices = sales.GroupBy(x => x.ServiceName)
            .Select(g => new CategoryTotal(g.Key, g.Sum(x => x.Amount)))
            .OrderByDescending(x => x.Amount).Take(6).ToList();

        var paymentMethods = sales.GroupBy(x => x.PaymentMethod)
            .Select(g => new CategoryTotal(g.Key, g.Sum(x => x.Amount)))
            .OrderByDescending(x => x.Amount).ToList();

        var trendStart = start.AddMonths(-5);
        var trendSales = await db.BarbershopSales.AsNoTracking()
            .Where(x => x.UserId == userId && x.Date >= trendStart && x.Date < end).ToListAsync();
        var trendExpenses = await db.BarbershopExpenses.AsNoTracking()
            .Where(x => x.UserId == userId && x.Date >= trendStart && x.Date < end && x.Paid).ToListAsync();

        var trend = Enumerable.Range(0, 6).Select(i =>
        {
            var m = trendStart.AddMonths(i);
            return new MonthlyPoint(
                m.Year, m.Month,
                trendSales.Where(x => x.Date.Year == m.Year && x.Date.Month == m.Month).Sum(x => x.Amount),
                trendExpenses.Where(x => x.Date.Year == m.Year && x.Date.Month == m.Month).Sum(x => x.Amount)
            );
        }).ToList();

        string insight;
        if (target > 0 && revenue >= target)
            insight = $"Meta de {target:C} atingida. O faturamento está {revenue - target:C} acima do objetivo.";
        else if (target > 0 && isCurrent)
            insight = $"Faltam {target - revenue:C} para a meta. O ritmo necessário é de {dailyNeeded:C} por dia até o fim do mês.";
        else if (revenue == 0)
            insight = "Ainda não há vendas neste período. Registre vendas para ativar ticket médio, projeção, mix de serviços e margem.";
        else
            insight = $"A margem operacional do período está em {(revenue == 0 ? 0 : (revenue - paidExpenses) / revenue * 100):N1}%.";

        return View(new BarbershopDashboardViewModel
        {
            Month = selected,
            Revenue = revenue,
            Expenses = paidExpenses,
            SalesCount = count,
            ProjectedRevenue = projection,
            TargetRevenue = target,
            DailyNeeded = dailyNeeded,
            ExpenseCategories = expenseCategories,
            TopServices = topServices,
            PaymentMethods = paymentMethods,
            RecentSales = sales.Take(8).ToList(),
            Trend = trend,
            Insight = insight
        });
    }
}
