using Microsoft.AspNetCore.Mvc;

namespace KicksMvc.Controllers;

public class TeamsController : Controller
{
    [HttpGet("/Pages/Teams/teams.html")]
    public IActionResult Index() => View();

}
