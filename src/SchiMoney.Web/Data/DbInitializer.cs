using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SchiMoney.Web.Models;

namespace SchiMoney.Web.Data;

public static class DbInitializer
{
    public static async Task InitializeAsync(IServiceProvider services, IConfiguration configuration)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await db.Database.EnsureCreatedAsync();
        await SchemaBootstrapper.EnsureAsync(db);

        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var email = configuration["SCHIMONEY_ADMIN_EMAIL"] ?? "admin@schimoney.local";
        var password = configuration["SCHIMONEY_ADMIN_PASSWORD"] ?? "ChangeMe!12345";

        var user = await userManager.FindByEmailAsync(email);
        if (user is null)
        {
            user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                DisplayName = "Administrador"
            };

            var result = await userManager.CreateAsync(user, password);
            if (!result.Succeeded)
                throw new InvalidOperationException("Não foi possível criar o usuário administrador: " +
                    string.Join("; ", result.Errors.Select(e => e.Description)));
        }

        if (!await db.BarbershopServices.AnyAsync(x => x.UserId == user.Id))
        {
            db.BarbershopServices.AddRange(
                new BarbershopService { UserId = user.Id, Name = "Corte", Price = 45 },
                new BarbershopService { UserId = user.Id, Name = "Barba", Price = 30 },
                new BarbershopService { UserId = user.Id, Name = "Corte + Barba", Price = 65 }
            );
            await db.SaveChangesAsync();
        }
    }
}
