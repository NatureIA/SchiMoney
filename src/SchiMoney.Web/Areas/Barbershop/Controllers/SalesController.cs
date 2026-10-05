using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchiMoney.Web.Data;
using SchiMoney.Web.Models;

namespace SchiMoney.Web.Areas.Barbershop.Controllers;

[Area("Barbershop"), Authorize]
public class SalesController(AppDbContext db) : Controller
{
    public async Task<IActionResult> Index()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        return View(await db.BarbershopSales.AsNoTracking()
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.Date).ThenByDescending(x => x.Id)
            .Take(250).ToListAsync());
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        ViewBag.Services = await db.BarbershopServices.AsNoTracking()
            .Where(x => x.UserId == userId && x.Active)
            .OrderBy(x => x.Name).ToListAsync();
        return View(new BarbershopSale { Date = DateTime.Today });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(BarbershopSale model)
    {
        ModelState.Remove(nameof(model.UserId));
        if (!ModelState.IsValid)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            ViewBag.Services = await db.BarbershopServices.AsNoTracking()
                .Where(x => x.UserId == userId && x.Active).OrderBy(x => x.Name).ToListAsync();
            return View(model);
        }
        model.UserId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        db.BarbershopSales.Add(model);
        await db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var item = await db.BarbershopSales.FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId);
        if (item is not null) { db.Remove(item); await db.SaveChangesAsync(); }
        return RedirectToAction(nameof(Index));
    }
}
