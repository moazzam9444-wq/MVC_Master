using Microsoft.AspNetCore.Mvc;

namespace KicksMvc.Controllers;

public class ChallengesController : Controller
{
    [HttpGet("/Pages/Challenges/challenges.html")]
    public IActionResult Index() => View();

}
