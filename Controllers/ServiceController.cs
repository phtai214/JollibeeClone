using Microsoft.AspNetCore.Mvc;

namespace JollibeeClone.Controllers
{
    public class ServiceController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        [Route("Promotion")]
        public IActionResult Promotions()
        {
            return View();
        }

        [Route("Store")]
        public IActionResult Stores()
        {
            return View();
        }
    }
} 