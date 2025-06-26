using Microsoft.AspNetCore.Mvc;

namespace JollibeeClone.Controllers
{
    public class StoreController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
} 