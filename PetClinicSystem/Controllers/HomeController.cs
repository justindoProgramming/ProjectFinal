using Microsoft.AspNetCore.Mvc;

namespace PetClinicSystem.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            ViewBag.Layout = "_Layout_Public";
            return View();
        }
    }
}
