using Microsoft.AspNetCore.Mvc;

namespace KicksMvc.Controllers;

public class DocumentationController : Controller
{
    [HttpGet("/docs/database/database.html")]
    public IActionResult Database() => View();

}
