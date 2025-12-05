using Microsoft.AspNetCore.Mvc;

namespace DWB.Controllers
{
    public class ErrorController : Controller
    {
        public IActionResult ApiDown()
        {
            return View();
        }
    }
}
