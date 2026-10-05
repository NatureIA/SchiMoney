using Microsoft.EntityFrameworkCore;

namespace SchiMoney.Web.Data;

public static class SchemaBootstrapper
{
    public static async Task EnsureAsync(AppDbContext db)
    {
        var sql = """
IF OBJECT_ID(N'[PersonalTransactions]', N'U') IS NULL
BEGIN
    CREATE TABLE [PersonalTransactions](
        [Id] int IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [UserId] nvarchar(450) NOT NULL,
        [Type] nvarchar(20) NOT NULL,
        [Description] nvarchar(140) NOT NULL,
        [Category] nvarchar(80) NOT NULL,
        [Subcategory] nvarchar(80) NULL,
        [Amount] decimal(18,2) NOT NULL,
        [Date] datetime2 NOT NULL,
        [DueDate] datetime2 NULL,
        [PaidAt] datetime2 NULL,
        [PaymentMethod] nvarchar(30) NOT NULL,
        [AccountName] nvarchar(80) NULL,
        [Status] nvarchar(20) NOT NULL,
        [RecurrenceType] nvarchar(20) NOT NULL,
        [InstallmentNumber] int NULL,
        [InstallmentTotal] int NULL,
        [Notes] nvarchar(500) NULL,
        [CreatedAt] datetime2 NOT NULL
    );
    CREATE INDEX [IX_PersonalTransactions_UserId_Date] ON [PersonalTransactions]([UserId],[Date]);
END;

IF OBJECT_ID(N'[PersonalAccounts]', N'U') IS NULL
BEGIN
    CREATE TABLE [PersonalAccounts](
        [Id] int IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [UserId] nvarchar(450) NOT NULL,
        [Name] nvarchar(100) NOT NULL,
        [Type] nvarchar(40) NOT NULL,
        [Institution] nvarchar(100) NULL,
        [InitialBalance] decimal(18,2) NOT NULL,
        [Active] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL
    );
    CREATE INDEX [IX_PersonalAccounts_UserId_Name] ON [PersonalAccounts]([UserId],[Name]);
END;

IF OBJECT_ID(N'[PersonalCreditCards]', N'U') IS NULL
BEGIN
    CREATE TABLE [PersonalCreditCards](
        [Id] int IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [UserId] nvarchar(450) NOT NULL,
        [Name] nvarchar(100) NOT NULL,
        [Institution] nvarchar(100) NULL,
        [Limit] decimal(18,2) NOT NULL,
        [ClosingDay] int NOT NULL,
        [DueDay] int NOT NULL,
        [Active] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL
    );
    CREATE INDEX [IX_PersonalCreditCards_UserId_Name] ON [PersonalCreditCards]([UserId],[Name]);
END;

IF OBJECT_ID(N'[BarbershopSales]', N'U') IS NULL
BEGIN
    CREATE TABLE [BarbershopSales](
        [Id] int IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [UserId] nvarchar(450) NOT NULL,
        [ServiceName] nvarchar(120) NOT NULL,
        [Amount] decimal(18,2) NOT NULL,
        [PaymentMethod] nvarchar(30) NOT NULL,
        [Date] datetime2 NOT NULL,
        [CustomerName] nvarchar(120) NULL,
        [Notes] nvarchar(500) NULL,
        [CreatedAt] datetime2 NOT NULL
    );
    CREATE INDEX [IX_BarbershopSales_UserId_Date] ON [BarbershopSales]([UserId],[Date]);
END;

IF OBJECT_ID(N'[BarbershopExpenses]', N'U') IS NULL
BEGIN
    CREATE TABLE [BarbershopExpenses](
        [Id] int IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [UserId] nvarchar(450) NOT NULL,
        [Description] nvarchar(140) NOT NULL,
        [Category] nvarchar(80) NOT NULL,
        [ExpenseType] nvarchar(20) NOT NULL,
        [Amount] decimal(18,2) NOT NULL,
        [PaymentMethod] nvarchar(30) NOT NULL,
        [Date] datetime2 NOT NULL,
        [DueDate] datetime2 NULL,
        [Paid] bit NOT NULL,
        [Notes] nvarchar(500) NULL,
        [CreatedAt] datetime2 NOT NULL
    );
    CREATE INDEX [IX_BarbershopExpenses_UserId_Date] ON [BarbershopExpenses]([UserId],[Date]);
END;

IF OBJECT_ID(N'[BarbershopServices]', N'U') IS NULL
BEGIN
    CREATE TABLE [BarbershopServices](
        [Id] int IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [UserId] nvarchar(450) NOT NULL,
        [Name] nvarchar(120) NOT NULL,
        [Price] decimal(18,2) NOT NULL,
        [Active] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL
    );
END;

IF OBJECT_ID(N'[FinancialGoals]', N'U') IS NULL
BEGIN
    CREATE TABLE [FinancialGoals](
        [Id] int IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [UserId] nvarchar(450) NOT NULL,
        [Module] nvarchar(20) NOT NULL,
        [Name] nvarchar(120) NOT NULL,
        [TargetAmount] decimal(18,2) NOT NULL,
        [CurrentAmount] decimal(18,2) NOT NULL,
        [Deadline] datetime2 NULL
    );
END;

IF OBJECT_ID(N'[AuditLogs]', N'U') IS NULL
BEGIN
    CREATE TABLE [AuditLogs](
        [Id] bigint IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [UserId] nvarchar(max) NULL,
        [Action] nvarchar(80) NOT NULL,
        [Entity] nvarchar(80) NOT NULL,
        [EntityId] nvarchar(100) NULL,
        [Details] nvarchar(1000) NULL,
        [CreatedAt] datetime2 NOT NULL
    );
END;
""";

        await db.Database.ExecuteSqlRawAsync(sql);
    }
}
