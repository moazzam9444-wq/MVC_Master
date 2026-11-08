using Microsoft.AspNetCore.Mvc;

namespace KicksMvc.Controllers;

public class DemoController : Controller
{
    [HttpGet("/Pages/Demo/catalogue.html")]
    public IActionResult Catalogue() => View();

    [HttpGet("/Pages/Demo/demo.html")]
    public IActionResult Simulation() => View();

}
