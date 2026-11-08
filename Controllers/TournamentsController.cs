using Microsoft.AspNetCore.Mvc;

namespace KicksMvc.Controllers;

public class TournamentsController : Controller
{
    [HttpGet("/Pages/Tournaments/tournaments.html")]
    public IActionResult Index() => View();

    [HttpGet("/Pages/Tournaments/tournament-details.html")]
    public IActionResult Details() => View();

}
