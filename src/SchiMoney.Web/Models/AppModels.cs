using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace SchiMoney.Web.Models;

public class ApplicationUser : IdentityUser
{
    [MaxLength(120)]
    public string DisplayName { get; set; } = "Administrador";
}

public class PersonalTransaction
{
    public int Id { get; set; }
    [Required] public string UserId { get; set; } = string.Empty;
    [Required, MaxLength(20)] public string Type { get; set; } = "Despesa";
    [Required, MaxLength(140)] public string Description { get; set; } = string.Empty;
    [Required, MaxLength(80)] public string Category { get; set; } = "Outros";
    [MaxLength(80)] public string? Subcategory { get; set; }
    [Range(0.01, 999999999)] public decimal Amount { get; set; }
    [DataType(DataType.Date)] public DateTime Date { get; set; } = DateTime.Today;
    [DataType(DataType.Date)] public DateTime? DueDate { get; set; }
    [DataType(DataType.Date)] public DateTime? PaidAt { get; set; }
    [Required, MaxLength(30)] public string PaymentMethod { get; set; } = "Pix";
    [MaxLength(80)] public string? AccountName { get; set; }
    [Required, MaxLength(20)] public string Status { get; set; } = "Pago";
    [Required, MaxLength(20)] public string RecurrenceType { get; set; } = "Avulso";
    public int? InstallmentNumber { get; set; }
    public int? InstallmentTotal { get; set; }
    [MaxLength(500)] public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class PersonalAccount
{
    public int Id { get; set; }
    [Required] public string UserId { get; set; } = string.Empty;
    [Required, MaxLength(100)] public string Name { get; set; } = string.Empty;
    [Required, MaxLength(40)] public string Type { get; set; } = "Conta digital";
    [MaxLength(100)] public string? Institution { get; set; }
    public decimal InitialBalance { get; set; }
    public bool Active { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class PersonalCreditCard
{
    public int Id { get; set; }
    [Required] public string UserId { get; set; } = string.Empty;
    [Required, MaxLength(100)] public string Name { get; set; } = string.Empty;
    [MaxLength(100)] public string? Institution { get; set; }
    public decimal Limit { get; set; }
    [Range(1,31)] public int ClosingDay { get; set; } = 1;
    [Range(1,31)] public int DueDay { get; set; } = 10;
    public bool Active { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class BarbershopSale
{
    public int Id { get; set; }
    [Required] public string UserId { get; set; } = string.Empty;
    [Required, MaxLength(120)] public string ServiceName { get; set; } = string.Empty;
    [Range(0.01, 999999999)] public decimal Amount { get; set; }
    [Required, MaxLength(30)] public string PaymentMethod { get; set; } = "Pix";
    [DataType(DataType.Date)] public DateTime Date { get; set; } = DateTime.Today;
    [MaxLength(120)] public string? CustomerName { get; set; }
    [MaxLength(500)] public string? Notes { get; set; }
    public int? RecurringSeriesId { get; set; }
    [MaxLength(7)] public string? RecurringOccurrenceKey { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class BarbershopRecurringSale
{
    public int Id { get; set; }
    [Required] public string UserId { get; set; } = string.Empty;
    [Required, MaxLength(120)] public string ServiceName { get; set; } = string.Empty;
    [Range(0.01, 999999999)] public decimal Amount { get; set; }
    [Required, MaxLength(30)] public string PaymentMethod { get; set; } = "Pix";
    [MaxLength(120)] public string? CustomerName { get; set; }
    [MaxLength(500)] public string? Notes { get; set; }
    [DataType(DataType.Date)] public DateTime StartDate { get; set; } = DateTime.Today;
    [DataType(DataType.Date)] public DateTime? EndDate { get; set; }
    public bool Active { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public class BarbershopExpense
{
    public int Id { get; set; }
    [Required] public string UserId { get; set; } = string.Empty;
    [Required, MaxLength(140)] public string Description { get; set; } = string.Empty;
    [Required, MaxLength(80)] public string Category { get; set; } = "Outros";
    [Required, MaxLength(20)] public string ExpenseType { get; set; } = "Variável";
    [Range(0.01, 999999999)] public decimal Amount { get; set; }
    [Required, MaxLength(30)] public string PaymentMethod { get; set; } = "Pix";
    [DataType(DataType.Date)] public DateTime Date { get; set; } = DateTime.Today;
    [DataType(DataType.Date)] public DateTime? DueDate { get; set; }
    public bool Paid { get; set; } = true;
    [MaxLength(500)] public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class BarbershopService
{
    public int Id { get; set; }
    [Required] public string UserId { get; set; } = string.Empty;
    [Required, MaxLength(120)] public string Name { get; set; } = string.Empty;
    [Range(0.01, 999999999)] public decimal Price { get; set; }
    public bool Active { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class FinancialGoal
{
    public int Id { get; set; }
    [Required] public string UserId { get; set; } = string.Empty;
    [Required, MaxLength(20)] public string Module { get; set; } = "Pessoal";
    [Required, MaxLength(120)] public string Name { get; set; } = string.Empty;
    [Range(0.01, 999999999)] public decimal TargetAmount { get; set; }
    public decimal CurrentAmount { get; set; }
    [DataType(DataType.Date)] public DateTime? Deadline { get; set; }
}

public class AuditLog
{
    public long Id { get; set; }
    public string? UserId { get; set; }
    [Required, MaxLength(80)] public string Action { get; set; } = string.Empty;
    [Required, MaxLength(80)] public string Entity { get; set; } = string.Empty;
    [MaxLength(100)] public string? EntityId { get; set; }
    [MaxLength(1000)] public string? Details { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
