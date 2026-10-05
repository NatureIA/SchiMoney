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
    public async Task<IActionResult> Index()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var start = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        var end = start.AddMonths(1);

        var month = db.PersonalTransactions.AsNoTracking()
            .Where(x => x.UserId == userId && x.Date >= start && x.Date < end);

        var income = await month.Where(x => x.Type == "Receita" && x.Status == "Pago").SumAsync(x => (decimal?)x.Amount) ?? 0;
        var expenses = await month.Where(x => x.Type == "Despesa" && x.Status == "Pago").SumAsync(x => (decimal?)x.Amount) ?? 0;
        var pending = await month.Where(x => x.Type == "Despesa" && x.Status != "Pago").SumAsync(x => (decimal?)x.Amount) ?? 0;

        var categories = await month.Where(x => x.Type == "Despesa" && x.Status == "Pago")
            .GroupBy(x => x.Category)
            .Select(g => new CategoryTotal(g.Key, g.Sum(x => x.Amount)))
            .OrderByDescending(x => x.Amount)
            .Take(6)
            .ToListAsync();

        var recent = await db.PersonalTransactions.AsNoTracking()
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.Date).ThenByDescending(x => x.Id)
            .Take(8).ToListAsync();

        var insight = income == 0
            ? "Cadastre suas receitas para o SchiMoney calcular sua taxa de economia e projeções."
            : expenses > income
                ? $"Suas despesas já representam {(expenses / income * 100):N1}% da renda registrada neste mês."
                : $"Você preservou {((income - expenses) / income * 100):N1}% da renda registrada neste mês.";

        return View(new PersonalDashboardViewModel
        {
            Income = income,
            Expenses = expenses,
            Pending = pending,
            Categories = categories,
            Recent = recent,
            Insight = insight
        });
    }
}
