using Microsoft.AspNetCore.Mvc;

namespace JollibeeClone.Controllers
{
    public class NewsController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
} 