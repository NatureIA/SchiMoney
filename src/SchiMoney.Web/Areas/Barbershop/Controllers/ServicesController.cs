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
    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    public async Task<IActionResult> Index() =>
        View(await db.BarbershopServices.AsNoTracking().Where(x => x.UserId == UserId).OrderByDescending(x => x.Active).ThenBy(x => x.Name).ToListAsync());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(string name, decimal price)
    {
        if (!string.IsNullOrWhiteSpace(name) && price > 0)
        {
            db.BarbershopServices.Add(new BarbershopService { UserId = UserId, Name = name.Trim(), Price = price });
            await db.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(int id, string name, decimal price)
    {
        var item = await db.BarbershopServices.FirstOrDefaultAsync(x => x.Id == id && x.UserId == UserId);
        if (item is not null && !string.IsNullOrWhiteSpace(name) && price > 0)
        {
            item.Name = name.Trim(); item.Price = price;
            await db.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Toggle(int id)
    {
        var item = await db.BarbershopServices.FirstOrDefaultAsync(x => x.Id == id && x.UserId == UserId);
        if (item is not null) { item.Active = !item.Active; await db.SaveChangesAsync(); }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var item = await db.BarbershopServices.FirstOrDefaultAsync(x => x.Id == id && x.UserId == UserId);
        if (item is not null) { db.Remove(item); await db.SaveChangesAsync(); }
        return RedirectToAction(nameof(Index));
    }
}
