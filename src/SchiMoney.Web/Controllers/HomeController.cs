using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SchiMoney.Web.Models;

namespace SchiMoney.Web.Controllers;

[Authorize]
public class HomeController : Controller
{
    [AllowAnonymous]
    public IActionResult Index()
    {
        if (User.Identity?.IsAuthenticated == true)
            return View();

        ViewBag.ReturnUrl = "/";
        return View("~/Views/Account/Login.cshtml", new LoginViewModel());
    }

    [AllowAnonymous, Route("erro")]
    public IActionResult Error() => View();
}
