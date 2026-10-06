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
        [InstallmentGroupId] nvarchar(36) NULL,
        [InstallmentNumber] int NULL,
        [InstallmentTotal] int NULL,
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

        var recurringStructureSql = """
IF OBJECT_ID(N'[BarbershopRecurringSales]', N'U') IS NULL
BEGIN
    CREATE TABLE [BarbershopRecurringSales](
        [Id] int IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [UserId] nvarchar(450) NOT NULL,
        [ServiceName] nvarchar(120) NOT NULL,
        [Amount] decimal(18,2) NOT NULL,
        [PaymentMethod] nvarchar(30) NOT NULL,
        [CustomerName] nvarchar(120) NULL,
        [Notes] nvarchar(500) NULL,
        [StartDate] datetime2 NOT NULL,
        [EndDate] datetime2 NULL,
        [Active] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NOT NULL
    );
END;

IF COL_LENGTH('BarbershopSales', 'RecurringSeriesId') IS NULL
BEGIN
    ALTER TABLE [BarbershopSales] ADD [RecurringSeriesId] int NULL;
END;

IF COL_LENGTH('BarbershopSales', 'RecurringOccurrenceKey') IS NULL
BEGIN
    ALTER TABLE [BarbershopSales] ADD [RecurringOccurrenceKey] nvarchar(7) NULL;
END;
""";

        await db.Database.ExecuteSqlRawAsync(recurringStructureSql);

        var recurringIndexesSql = """
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_BarbershopRecurringSales_UserId_Active'
      AND object_id = OBJECT_ID('BarbershopRecurringSales')
)
BEGIN
    CREATE INDEX [IX_BarbershopRecurringSales_UserId_Active]
        ON [BarbershopRecurringSales]([UserId], [Active]);
END;

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_BarbershopSales_RecurringSeriesId_RecurringOccurrenceKey'
      AND object_id = OBJECT_ID('BarbershopSales')
)
BEGIN
    CREATE INDEX [IX_BarbershopSales_RecurringSeriesId_RecurringOccurrenceKey]
        ON [BarbershopSales]([RecurringSeriesId], [RecurringOccurrenceKey]);
END;
""";

        await db.Database.ExecuteSqlRawAsync(recurringIndexesSql);

        var expenseInstallmentStructureSql = """
IF COL_LENGTH('BarbershopExpenses', 'InstallmentGroupId') IS NULL
BEGIN
    ALTER TABLE [BarbershopExpenses] ADD [InstallmentGroupId] nvarchar(36) NULL;
END;

IF COL_LENGTH('BarbershopExpenses', 'InstallmentNumber') IS NULL
BEGIN
    ALTER TABLE [BarbershopExpenses] ADD [InstallmentNumber] int NULL;
END;

IF COL_LENGTH('BarbershopExpenses', 'InstallmentTotal') IS NULL
BEGIN
    ALTER TABLE [BarbershopExpenses] ADD [InstallmentTotal] int NULL;
END;
""";

        await db.Database.ExecuteSqlRawAsync(expenseInstallmentStructureSql);

        var expenseInstallmentIndexSql = """
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_BarbershopExpenses_InstallmentGroupId'
      AND object_id = OBJECT_ID('BarbershopExpenses')
)
BEGIN
    CREATE INDEX [IX_BarbershopExpenses_InstallmentGroupId]
        ON [BarbershopExpenses]([InstallmentGroupId]);
END;
""";

        await db.Database.ExecuteSqlRawAsync(expenseInstallmentIndexSql);
    }
}
