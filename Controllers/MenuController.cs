using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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
                    p.ThumbnailUrl,
                    IsConfigurable = true
                })
                .ToList();
            return Json(combos);
        }

        // NEW: Get regular products (non-configurable and not used as combo options)
        public JsonResult GetRegularProducts()
        {
            // Get IDs of products used as options in combos
            var comboOptionProductIds = _context.ProductConfigurationOptions
                .Select(pco => pco.OptionProductID)
                .Distinct()
                .ToList();

            var regularProducts = _context.Products
                .Where(p => !p.IsConfigurable && p.IsAvailable && !comboOptionProductIds.Contains(p.ProductID))
                .Select(p => new {
                    p.ProductID,
                    p.ProductName,
                    p.ShortDescription,
                    p.Price,
                    p.OriginalPrice,
                    p.ImageUrl,
                    p.ThumbnailUrl,
                    IsConfigurable = false
                })
                .ToList();
            return Json(regularProducts);
        }

        // NEW: Get all menu items for "Món ngon phải thử" category only
        public JsonResult GetAllMenuItems()
        {
            // Get CategoryID for "Món ngon phải thử"
            var monNgonPhaiThuCategory = _context.Categories
                .FirstOrDefault(c => c.CategoryName.Contains("Món ngon phải thử") || 
                                    c.CategoryName.Contains("mon ngon phai thu"));
            
            if (monNgonPhaiThuCategory == null)
            {
                Console.WriteLine("⚠️ 'Món ngon phải thử' category not found!");
                return Json(new List<object>());
            }

            Console.WriteLine($"📂 Found category: {monNgonPhaiThuCategory.CategoryName} (ID: {monNgonPhaiThuCategory.CategoryID})");

            // Get IDs of products used as options in combos
            var comboOptionProductIds = _context.ProductConfigurationOptions
                .Select(pco => pco.OptionProductID)
                .Distinct()
                .ToList();

            Console.WriteLine($"🚫 Excluding {comboOptionProductIds.Count} combo option products: {string.Join(", ", comboOptionProductIds)}");

            var allProducts = _context.Products
                .Include(p => p.Category)
                .Where(p => p.IsAvailable && 
                           p.CategoryID == monNgonPhaiThuCategory.CategoryID &&
                           (p.IsConfigurable || !comboOptionProductIds.Contains(p.ProductID)))
                .ToList()
                .Select(p => new {
                    p.ProductID,
                    p.ProductName,
                    p.ShortDescription,
                    p.Price,
                    p.OriginalPrice,
                    p.ImageUrl,
                    p.ThumbnailUrl,
                    p.IsConfigurable,
                    CategoryName = p.Category.CategoryName
                })
                .OrderBy(p => p.IsConfigurable ? 0 : 1)
                .ThenBy(p => p.ProductName)
                .ToList();

            Console.WriteLine($"📦 Found {allProducts.Count} products in 'Món ngon phải thử' category:");
            foreach (var product in allProducts)
            {
                Console.WriteLine($"  - {product.ProductName} (ID: {product.ProductID}, Category: {product.CategoryName}, IsConfigurable: {product.IsConfigurable})");
            }

            return Json(allProducts);
        }

        // DEBUG: Check categories and products
        public JsonResult DebugCategoriesAndProducts()
        {
            var categories = _context.Categories
                .Select(c => new { c.CategoryID, c.CategoryName })
                .ToList();

            var allProducts = _context.Products
                .Include(p => p.Category)
                .Select(p => new {
                    p.ProductID,
                    p.ProductName,
                    p.CategoryID,
                    CategoryName = p.Category.CategoryName,
                    p.IsConfigurable,
                    p.IsAvailable
                })
                .ToList();

            var comboOptionProductIds = _context.ProductConfigurationOptions
                .Select(pco => pco.OptionProductID)
                .Distinct()
                .ToList();

            return Json(new {
                categories,
                allProducts,
                comboOptionProductIds,
                totalCategories = categories.Count,
                totalProducts = allProducts.Count,
                availableProducts = allProducts.Count(p => p.IsAvailable),
                configurableProducts = allProducts.Count(p => p.IsConfigurable),
                comboOptionProducts = comboOptionProductIds.Count
            });
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