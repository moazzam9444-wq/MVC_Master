using Microsoft.AspNetCore.Mvc;

namespace KicksMvc.Controllers;

public class OpenGamesController : Controller
{
    [HttpGet("/Pages/OpenGames/open-games.html")]
    public IActionResult Index() => View();

    [HttpGet("/Pages/OpenGames/game-details.html")]
    public IActionResult Details() => View();

}
