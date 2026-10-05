using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchiMoney.Web.Data;
using SchiMoney.Web.Models;

namespace SchiMoney.Web.Areas.Barbershop.Controllers;

[Area("Barbershop"), Authorize]
public class ServicesController(AppDbContext db) : Controller
{
    public async Task<IActionResult> Index()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        return View(await db.BarbershopServices.AsNoTracking()
            .Where(x => x.UserId == userId).OrderBy(x => x.Name).ToListAsync());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(string name, decimal price)
    {
        if (!string.IsNullOrWhiteSpace(name) && price > 0)
        {
            db.BarbershopServices.Add(new BarbershopService
            {
                UserId = User.FindFirstValue(ClaimTypes.NameIdentifier)!,
                Name = name.Trim(),
                Price = price
            });
            await db.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var item = await db.BarbershopServices.FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId);
        if (item is not null) { db.Remove(item); await db.SaveChangesAsync(); }
        return RedirectToAction(nameof(Index));
    }
}
