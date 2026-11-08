using Microsoft.AspNetCore.Mvc;

namespace KicksMvc.Controllers;

public class HomeController : Controller
{
    [HttpGet("/")]
    [HttpGet("/index.html")]
    public IActionResult Index() => View();

    [HttpGet("/Pages/About/about.html")]
    public IActionResult About() => View();

    [HttpGet("/Pages/Contact/contact.html")]
    public IActionResult Contact() => View();

}
