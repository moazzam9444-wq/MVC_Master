using Microsoft.AspNetCore.Mvc;

namespace KicksMvc.Controllers;

public class HomeController : Controller
{
    [HttpGet("/error"),HttpPost("/error")]
    public IActionResult Error(){Response.StatusCode=500;return View("Status",500);}

    [HttpGet("/status/{code:int}")]
    public IActionResult Status(int code){code=code is 403 or 404 or 500?code:404;Response.StatusCode=code;return View("Status",code);}

    [HttpGet("/")]
    [HttpGet("/index.html")]
    public IActionResult Index() => View();

    [HttpGet("/Pages/About/about.html")]
    public IActionResult About() => View();

    [HttpGet("/Pages/Contact/contact.html")]
    public IActionResult Contact() => View();

}
