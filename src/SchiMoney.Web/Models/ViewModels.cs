using System.ComponentModel.DataAnnotations;

namespace SchiMoney.Web.Models;

public class LoginViewModel
{
    [Required, EmailAddress, Display(Name = "E-mail")]
    public string Email { get; set; } = string.Empty;

    [Required, DataType(DataType.Password), Display(Name = "Senha")]
    public string Password { get; set; } = string.Empty;

    public bool RememberMe { get; set; }
}

public class PersonalDashboardViewModel
{
    public decimal Income { get; set; }
    public decimal Expenses { get; set; }
    public decimal Balance => Income - Expenses;
    public decimal Pending { get; set; }
    public decimal SavingsRate => Income <= 0 ? 0 : (Balance / Income) * 100;
    public List<PersonalTransaction> Recent { get; set; } = [];
    public List<CategoryTotal> Categories { get; set; } = [];
    public string Insight { get; set; } = string.Empty;
}

public class BarbershopDashboardViewModel
{
    public decimal Revenue { get; set; }
    public decimal Expenses { get; set; }
    public decimal Profit => Revenue - Expenses;
    public decimal Margin => Revenue <= 0 ? 0 : (Profit / Revenue) * 100;
    public int SalesCount { get; set; }
    public decimal AverageTicket => SalesCount == 0 ? 0 : Revenue / SalesCount;
    public decimal ProjectedRevenue { get; set; }
    public List<BarbershopSale> RecentSales { get; set; } = [];
    public List<CategoryTotal> ExpenseCategories { get; set; } = [];
    public string Insight { get; set; } = string.Empty;
}

public record CategoryTotal(string Name, decimal Amount);
