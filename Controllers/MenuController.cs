using Microsoft.AspNetCore.Mvc;
using JollibeeClone.Data;
using JollibeeClone.Models;
using System.Linq;

namespace JollibeeClone.Controllers
{
    public class MenuController : Controller
    {
        private readonly AppDbContext _context;
        public MenuController(AppDbContext context)
        {
            _context = context;
        }

        public IActionResult MonNgonPhaiThu()
        {
            return View();
        }

        public IActionResult GaGionVuiVe()
        {
            return View();
        }

        public IActionResult MiYJolly()
        {
            return View();
        }

        public IActionResult GaSotCay()
        {
            return View();
        }

        public IActionResult BurgerCom()
        {
            return View();
        }

        public IActionResult PhanAnPhu()
        {
            return View();
        }

        public IActionResult MonTrangMieng()
        {
            return View();
        }

        public IActionResult ThucUong()
        {
            return View();
        }

        public JsonResult GetCombos()
        {
            var combos = _context.Products
                .Where(p => p.IsConfigurable && p.IsAvailable)
                .Select(p => new {
                    p.ProductID,
                    p.ProductName,
                    p.ShortDescription,
                    p.Price,
                    p.OriginalPrice,
                    p.ImageUrl,
                    p.ThumbnailUrl
                })
                .ToList();
            return Json(combos);
        }

        [HttpGet]
        public JsonResult GetComboOptions(int productId)
        {
            var combo = _context.Products
                .Where(p => p.ProductID == productId && p.IsConfigurable && p.IsAvailable)
                .Select(p => new {
                    p.ProductID,
                    p.ProductName,
                    Groups = p.ProductConfigurationGroups.Select(g => new {
                        g.ConfigGroupID,
                        g.GroupName,
                        g.MinSelections,
                        g.MaxSelections,
                        Options = g.ProductConfigurationOptions.Select(o => new {
                            o.ConfigOptionID,
                            o.OptionProductID,
                            o.PriceAdjustment,
                            o.IsDefault,
                            o.DisplayOrder,
                            o.CustomImageUrl,
                            o.Quantity,
                            o.VariantID,
                            ProductName = o.OptionProduct.ProductName,
                            ProductImage = o.OptionProduct.ImageUrl,
                            VariantName = o.Variant != null ? o.Variant.VariantName : null,
                            VariantType = o.Variant != null ? o.Variant.VariantType : null
                        }).ToList()
                    }).ToList()
                })
                .FirstOrDefault();
            if (combo == null)
                return Json(new { error = "Combo not found" });
            return Json(combo);
        }
    }
} 