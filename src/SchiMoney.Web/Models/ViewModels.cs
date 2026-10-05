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
    public DateTime Month { get; set; }
    public decimal Income { get; set; }
    public decimal Expenses { get; set; }
    public decimal Balance => Income - Expenses;
    public decimal Pending { get; set; }
    public decimal AccountBalance { get; set; }
    public decimal PreviousBalance { get; set; }
    public decimal SavingsRate => Income <= 0 ? 0 : (Balance / Income) * 100;
    public List<PersonalTransaction> Recent { get; set; } = [];
    public List<PersonalTransaction> Upcoming { get; set; } = [];
    public List<CategoryTotal> Categories { get; set; } = [];
    public List<MonthlyPoint> Trend { get; set; } = [];
    public string Insight { get; set; } = string.Empty;
}

public class PersonalAccountsViewModel
{
    public List<PersonalAccount> Accounts { get; set; } = [];
    public List<PersonalCreditCard> Cards { get; set; } = [];
    public Dictionary<int, decimal> Balances { get; set; } = [];
    public decimal TotalBalance => Balances.Values.Sum();
    public decimal TotalCardLimit => Cards.Sum(x => x.Limit);
}

public class PersonalReportViewModel
{
    public int Year { get; set; }
    public decimal Income { get; set; }
    public decimal Expenses { get; set; }
    public decimal Result => Income - Expenses;
    public List<MonthlyPoint> Months { get; set; } = [];
    public List<CategoryTotal> Categories { get; set; } = [];
}

public class BarbershopDashboardViewModel
{
    public DateTime Month { get; set; }
    public decimal Revenue { get; set; }
    public decimal Expenses { get; set; }
    public decimal Profit => Revenue - Expenses;
    public decimal Margin => Revenue <= 0 ? 0 : (Profit / Revenue) * 100;
    public int SalesCount { get; set; }
    public decimal AverageTicket => SalesCount == 0 ? 0 : Revenue / SalesCount;
    public decimal ProjectedRevenue { get; set; }
    public decimal TargetRevenue { get; set; }
    public decimal RemainingToTarget => Math.Max(0, TargetRevenue - Revenue);
    public decimal DailyNeeded { get; set; }
    public List<BarbershopSale> RecentSales { get; set; } = [];
    public List<CategoryTotal> ExpenseCategories { get; set; } = [];
    public List<CategoryTotal> TopServices { get; set; } = [];
    public List<CategoryTotal> PaymentMethods { get; set; } = [];
    public List<MonthlyPoint> Trend { get; set; } = [];
    public string Insight { get; set; } = string.Empty;
}

public class BarbershopReportViewModel
{
    public int Year { get; set; }
    public decimal Revenue { get; set; }
    public decimal Expenses { get; set; }
    public decimal Profit => Revenue - Expenses;
    public decimal Margin => Revenue <= 0 ? 0 : Profit / Revenue * 100;
    public List<MonthlyPoint> Months { get; set; } = [];
    public List<CategoryTotal> Services { get; set; } = [];
    public List<CategoryTotal> ExpensesByCategory { get; set; } = [];
    public List<CategoryTotal> PaymentMethods { get; set; } = [];
}

public record CategoryTotal(string Name, decimal Amount);
public record MonthlyPoint(int Year, int Month, decimal Income, decimal Expense)
{
    public decimal Result => Income - Expense;
    public string Label => new DateTime(Year, Month, 1).ToString("MMM");
}
