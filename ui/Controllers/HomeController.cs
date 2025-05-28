using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using ui.Models;

namespace ui.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index() => View();

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error(int statusCode, string message = "")
        {
            if (statusCode == 404)
            {
                return View("NotFound");
            }

            if (string.IsNullOrEmpty(message))
            {
                message = "Во время обработки запроса произошла ошибка. Обратитесь к администратору";
            }

            ViewBag.ErrorMessage = message;
            return View("Error");
        }
    }
}
