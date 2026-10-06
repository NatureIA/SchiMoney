using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SchiMoney.Web.Models;

namespace SchiMoney.Web.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options)
    : IdentityDbContext<ApplicationUser>(options)
{
    public DbSet<PersonalTransaction> PersonalTransactions => Set<PersonalTransaction>();
    public DbSet<PersonalAccount> PersonalAccounts => Set<PersonalAccount>();
    public DbSet<PersonalCreditCard> PersonalCreditCards => Set<PersonalCreditCard>();
    public DbSet<BarbershopSale> BarbershopSales => Set<BarbershopSale>();
    public DbSet<BarbershopRecurringSale> BarbershopRecurringSales => Set<BarbershopRecurringSale>();
    public DbSet<BarbershopExpense> BarbershopExpenses => Set<BarbershopExpense>();
    public DbSet<BarbershopService> BarbershopServices => Set<BarbershopService>();
    public DbSet<FinancialGoal> FinancialGoals => Set<FinancialGoal>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<PersonalTransaction>().Property(x => x.Amount).HasPrecision(18, 2);
        builder.Entity<PersonalAccount>().Property(x => x.InitialBalance).HasPrecision(18, 2);
        builder.Entity<PersonalCreditCard>().Property(x => x.Limit).HasPrecision(18, 2);
        builder.Entity<BarbershopSale>().Property(x => x.Amount).HasPrecision(18, 2);
        builder.Entity<BarbershopRecurringSale>().Property(x => x.Amount).HasPrecision(18, 2);
        builder.Entity<BarbershopExpense>().Property(x => x.Amount).HasPrecision(18, 2);
        builder.Entity<BarbershopService>().Property(x => x.Price).HasPrecision(18, 2);
        builder.Entity<FinancialGoal>().Property(x => x.TargetAmount).HasPrecision(18, 2);
        builder.Entity<FinancialGoal>().Property(x => x.CurrentAmount).HasPrecision(18, 2);

        builder.Entity<PersonalTransaction>().HasIndex(x => new { x.UserId, x.Date });
        builder.Entity<PersonalAccount>().HasIndex(x => new { x.UserId, x.Name });
        builder.Entity<PersonalCreditCard>().HasIndex(x => new { x.UserId, x.Name });
        builder.Entity<BarbershopSale>().HasIndex(x => new { x.UserId, x.Date });
        builder.Entity<BarbershopSale>().HasIndex(x => new { x.RecurringSeriesId, x.RecurringOccurrenceKey });
        builder.Entity<BarbershopRecurringSale>().HasIndex(x => new { x.UserId, x.Active });
        builder.Entity<BarbershopExpense>().HasIndex(x => new { x.UserId, x.Date });
    }
}
