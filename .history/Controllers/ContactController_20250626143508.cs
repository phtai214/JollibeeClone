using Microsoft.AspNetCore.Mvc;

namespace JollibeeClone.Controllers
{
    public class ContactController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
} 