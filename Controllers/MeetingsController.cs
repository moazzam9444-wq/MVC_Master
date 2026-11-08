using Microsoft.AspNetCore.Mvc;

namespace KicksMvc.Controllers;

public class MeetingsController : Controller
{
    [HttpGet("/Pages/Meetings/meeting.html")]
    public IActionResult Legacy() => View();

}
