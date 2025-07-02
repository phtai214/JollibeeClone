using Microsoft.AspNetCore.Mvc;

namespace JollibeeClone.Controllers
{
    public class CartController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
} 