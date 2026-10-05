using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchiMoney.Web.Data;
using SchiMoney.Web.Models;

namespace SchiMoney.Web.Areas.Personal.Controllers;

[Area("Personal"), Authorize]
public class DashboardController(AppDbContext db) : Controller
{
    public async Task<IActionResult> Index(int? year = null, int? month = null)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var selected = new DateTime(year ?? DateTime.Today.Year, month ?? DateTime.Today.Month, 1);
        var start = selected;
        var end = start.AddMonths(1);
        var prevStart = start.AddMonths(-1);

        var allMonth = await db.PersonalTransactions.AsNoTracking()
            .Where(x => x.UserId == userId && x.Date >= start && x.Date < end)
            .OrderByDescending(x => x.Date)
            .ToListAsync();

        var income = allMonth.Where(x => x.Type == "Receita" && x.Status == "Pago").Sum(x => x.Amount);
        var expenses = allMonth.Where(x => x.Type == "Despesa" && x.Status == "Pago").Sum(x => x.Amount);
        var pending = allMonth.Where(x => x.Type == "Despesa" && x.Status != "Pago").Sum(x => x.Amount);

        var previous = await db.PersonalTransactions.AsNoTracking()
            .Where(x => x.UserId == userId && x.Date >= prevStart && x.Date < start && x.Status == "Pago")
            .ToListAsync();

        var previousBalance =
            previous.Where(x => x.Type == "Receita").Sum(x => x.Amount) -
            previous.Where(x => x.Type == "Despesa").Sum(x => x.Amount);

        var categories = allMonth
            .Where(x => x.Type == "Despesa" && x.Status == "Pago")
            .GroupBy(x => x.Category)
            .Select(g => new CategoryTotal(g.Key, g.Sum(x => x.Amount)))
            .OrderByDescending(x => x.Amount)
            .Take(7)
            .ToList();

        var recent = await db.PersonalTransactions.AsNoTracking()
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.Date).ThenByDescending(x => x.Id)
            .Take(8).ToListAsync();

        var today = DateTime.Today;
        var upcomingEnd = today.AddDays(30);
        var upcoming = await db.PersonalTransactions.AsNoTracking()
            .Where(x => x.UserId == userId &&
                        x.Type == "Despesa" &&
                        x.Status != "Pago" &&
                        x.DueDate != null &&
                        x.DueDate >= today &&
                        x.DueDate <= upcomingEnd)
            .OrderBy(x => x.DueDate)
            .Take(8)
            .ToListAsync();

        var accounts = await db.PersonalAccounts.AsNoTracking()
            .Where(x => x.UserId == userId && x.Active)
            .ToListAsync();

        var paidMovements = await db.PersonalTransactions.AsNoTracking()
            .Where(x => x.UserId == userId && x.Status == "Pago" && x.AccountName != null)
            .ToListAsync();

        var accountBalance = accounts.Sum(a =>
            a.InitialBalance
            + paidMovements.Where(x => x.AccountName == a.Name && x.Type == "Receita").Sum(x => x.Amount)
            - paidMovements.Where(x => x.AccountName == a.Name && x.Type == "Despesa").Sum(x => x.Amount));

        var trendStart = start.AddMonths(-5);
        var trendRaw = await db.PersonalTransactions.AsNoTracking()
            .Where(x => x.UserId == userId && x.Date >= trendStart && x.Date < end && x.Status == "Pago")
            .ToListAsync();

        var trend = Enumerable.Range(0, 6).Select(i =>
        {
            var m = trendStart.AddMonths(i);
            var rows = trendRaw.Where(x => x.Date.Year == m.Year && x.Date.Month == m.Month);
            return new MonthlyPoint(
                m.Year,
                m.Month,
                rows.Where(x => x.Type == "Receita").Sum(x => x.Amount),
                rows.Where(x => x.Type == "Despesa").Sum(x => x.Amount)
            );
        }).ToList();

        string insight;
        if (income <= 0 && expenses <= 0)
            insight = "Este mês ainda não tem movimentações pagas. Registre receitas e despesas para ativar as análises.";
        else if (income > 0 && expenses > income)
            insight = $"As despesas já consumiram {(expenses / income * 100):N1}% da renda do mês. O resultado está negativo em {Math.Abs(income - expenses):C}.";
        else if (income > 0)
            insight = $"Você preservou {((income - expenses) / income * 100):N1}% da renda registrada neste mês.";
        else
            insight = $"Há {expenses:C} em despesas pagas e nenhuma receita paga registrada neste mês.";

        return View(new PersonalDashboardViewModel
        {
            Month = selected,
            Income = income,
            Expenses = expenses,
            Pending = pending,
            AccountBalance = accountBalance,
            PreviousBalance = previousBalance,
            Categories = categories,
            Recent = recent,
            Upcoming = upcoming,
            Trend = trend,
            Insight = insight
        });
    }
}
