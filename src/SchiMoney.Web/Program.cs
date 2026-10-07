using System.Globalization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.EntityFrameworkCore;
using SchiMoney.Web.Data;
using SchiMoney.Web.Models;
using SchiMoney.Web.Services;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' não configurada.");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(connectionString, sql => sql.EnableRetryOnFailure()));

builder.Services
    .AddIdentity<ApplicationUser, IdentityRole>(options =>
    {
        options.Password.RequiredLength = 10;
        options.Password.RequireDigit = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireNonAlphanumeric = true;
        options.Lockout.MaxFailedAccessAttempts = 5;
    })
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/conta/entrar";
    options.AccessDeniedPath = "/conta/entrar";
    options.ExpireTimeSpan = TimeSpan.FromDays(14);
    options.SlidingExpiration = true;
});

builder.Services.AddControllersWithViews();
builder.Services.AddScoped<BarbershopRecurringSaleService>();
builder.Services.AddScoped<BarbershopRecurringExpenseService>();

var app = builder.Build();

var culture = new CultureInfo("pt-BR");
CultureInfo.DefaultThreadCurrentCulture = culture;
CultureInfo.DefaultThreadCurrentUICulture = culture;

if (!app.Environment.IsDevelopment())
{
    if (string.IsNullOrWhiteSpace(app.Configuration["SCHIMONEY_ADMIN_EMAIL"]) ||
        string.IsNullOrWhiteSpace(app.Configuration["SCHIMONEY_ADMIN_PASSWORD"]))
    {
        throw new InvalidOperationException(
            "Configure SCHIMONEY_ADMIN_EMAIL e SCHIMONEY_ADMIN_PASSWORD no ambiente de produção.");
    }

    app.UseExceptionHandler("/erro");
    app.UseHsts();
}

app.UseHttpsRedirection();

var staticContentTypes = new FileExtensionContentTypeProvider();
staticContentTypes.Mappings[".webmanifest"] = "application/manifest+json";

app.UseStaticFiles(new StaticFileOptions
{
    ContentTypeProvider = staticContentTypes,
    OnPrepareResponse = context =>
    {
        var path = context.Context.Request.Path.Value ?? "";

        if (path.EndsWith("manifest.webmanifest", StringComparison.OrdinalIgnoreCase) ||
            path.EndsWith("manifest.json", StringComparison.OrdinalIgnoreCase) ||
            path.EndsWith("service-worker.js", StringComparison.OrdinalIgnoreCase))
        {
            context.Context.Response.Headers.CacheControl = "no-cache, no-store, must-revalidate";
            context.Context.Response.Headers.Pragma = "no-cache";
            context.Context.Response.Headers.Expires = "0";
        }

        if (path.EndsWith("manifest.json", StringComparison.OrdinalIgnoreCase))
        {
            context.Context.Response.ContentType = "application/manifest+json; charset=utf-8";
        }

        if (path.EndsWith("service-worker.js", StringComparison.OrdinalIgnoreCase))
        {
            context.Context.Response.Headers["Service-Worker-Allowed"] = "/";
        }
    }
});

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/health", async (AppDbContext db) =>
{
    try
    {
        if (!await db.Database.CanConnectAsync())
            return Results.Json(new { status = "unhealthy", database = "unreachable" }, statusCode: 503);

        await db.PersonalTransactions.AsNoTracking().AnyAsync();
        await db.PersonalAccounts.AsNoTracking().AnyAsync();
        await db.PersonalCreditCards.AsNoTracking().AnyAsync();
        await db.BarbershopSales.AsNoTracking().AnyAsync();
        await db.BarbershopRecurringSales.AsNoTracking().AnyAsync();
        await db.BarbershopExpenses.AsNoTracking().AnyAsync();
        await db.BarbershopRecurringExpenses.AsNoTracking().AnyAsync();
        await db.BarbershopServices.AsNoTracking().AnyAsync();
        await db.FinancialGoals.AsNoTracking().AnyAsync();

        return Results.Ok(new
        {
            status = "healthy",
            application = "SchiMoney",
            database = "connected",
            schema = "ready",
            utc = DateTime.UtcNow
        });
    }
    catch (Exception ex)
    {
        return Results.Json(new
        {
            status = "unhealthy",
            database = "error",
            message = ex.GetType().Name
        }, statusCode: 503);
    }
});

app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

await DbInitializer.InitializeAsync(app.Services, app.Configuration);

app.Run();
