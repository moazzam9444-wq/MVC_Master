using Microsoft.AspNetCore.Mvc;
using KicksMvc.ViewModels;
using Microsoft.AspNetCore.Authorization;
using KicksMvc.Models.Identity;

namespace KicksMvc.Controllers;

public class DashboardController : Controller
{
    [Authorize(Roles = KicksRoles.Player), HttpGet("/Pages/Dashboard/player.html")]
    public IActionResult Player() => View(new DashboardPageViewModel("player"));

    [Authorize(Roles = KicksRoles.Owner), HttpGet("/Pages/Dashboard/owner.html")]
    public IActionResult Owner() => View(new DashboardPageViewModel("owner"));

    [Authorize(Roles = KicksRoles.Admin), HttpGet("/Pages/Dashboard/admin.html")]
    public IActionResult Admin() => View(new DashboardPageViewModel("admin"));

}
