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

        // NEW: Get all menu items for "Gà Giòn Vui Vẻ" category
        public JsonResult GetGaGionVuiVeItems()
        {
            // Get CategoryID for "Gà Giòn Vui Vẻ"
            var gaGionVuiVeCategory = _context.Categories
                .FirstOrDefault(c => c.CategoryName.Contains("Gà Giòn Vui Vẻ") || 
                                    c.CategoryName.Contains("ga gion vui ve"));
            
            if (gaGionVuiVeCategory == null)
            {
                Console.WriteLine("⚠️ 'Gà Giòn Vui Vẻ' category not found!");
                return Json(new List<object>());
            }

            Console.WriteLine($"📂 Found category: {gaGionVuiVeCategory.CategoryName} (ID: {gaGionVuiVeCategory.CategoryID})");

            // Get IDs of products used as options in combos
            var comboOptionProductIds = _context.ProductConfigurationOptions
                .Select(pco => pco.OptionProductID)
                .Distinct()
                .ToList();

            Console.WriteLine($"🚫 Excluding {comboOptionProductIds.Count} combo option products: {string.Join(", ", comboOptionProductIds)}");

            var allProducts = _context.Products
                .Include(p => p.Category)
                .Where(p => p.IsAvailable && 
                           p.CategoryID == gaGionVuiVeCategory.CategoryID &&
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

            Console.WriteLine($"📦 Found {allProducts.Count} products in 'Gà Giòn Vui Vẻ' category:");
            foreach (var product in allProducts)
            {
                Console.WriteLine($"  - {product.ProductName} (ID: {product.ProductID}, Category: {product.CategoryName}, IsConfigurable: {product.IsConfigurable})");
            }

            return Json(allProducts);
        }

        // NEW: Get all menu items for "Mì Ý Jolly" category
        public JsonResult GetMiYJollyItems()
        {
            try
            {
                Console.WriteLine("🔍 GetMiYJollyItems called");
                
                // Get CategoryID for "Mì Ý Jolly"
                var miYJollyCategory = _context.Categories
                    .FirstOrDefault(c => c.CategoryName.Contains("Mì Ý Jolly") || 
                                        c.CategoryName.Contains("mi y jolly") ||
                                        c.CategoryName.Contains("Mì Ý"));
                
                if (miYJollyCategory == null)
                {
                    Console.WriteLine("⚠️ 'Mì Ý Jolly' category not found!");
                    return Json(new List<object>());
                }

                Console.WriteLine($"📂 Found category: {miYJollyCategory.CategoryName} (ID: {miYJollyCategory.CategoryID})");

                // Simplified logic - just get all available products in this category
                var allProducts = _context.Products
                    .Include(p => p.Category)
                    .Where(p => p.IsAvailable && p.CategoryID == miYJollyCategory.CategoryID)
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
                    .OrderBy(p => p.ProductName)
                    .ToList();

                Console.WriteLine($"📦 Found {allProducts.Count} products in 'Mì Ý Jolly' category:");
                foreach (var product in allProducts)
                {
                    Console.WriteLine($"  - {product.ProductName} (ID: {product.ProductID}, Price: {product.Price}, IsConfigurable: {product.IsConfigurable})");
                }

                return Json(allProducts);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error in GetMiYJollyItems: {ex.Message}");
                Console.WriteLine($"❌ Stack trace: {ex.StackTrace}");
                return Json(new List<object>());
            }
        }

        // NEW: Get all menu items for "Gà Sốt Cay" category
        public JsonResult GetGaSotCayItems()
        {
            try
            {
                Console.WriteLine("🔍 GetGaSotCayItems called");
                
                // Get CategoryID for "Gà Sốt Cay"
                var gaSotCayCategory = _context.Categories
                    .FirstOrDefault(c => c.CategoryName.Contains("Gà Sốt Cay") || 
                                        c.CategoryName.Contains("ga sot cay") ||
                                        c.CategoryName.Contains("Gà sốt cay"));
                
                if (gaSotCayCategory == null)
                {
                    Console.WriteLine("⚠️ 'Gà Sốt Cay' category not found!");
                    return Json(new List<object>());
                }

                Console.WriteLine($"📂 Found category: {gaSotCayCategory.CategoryName} (ID: {gaSotCayCategory.CategoryID})");

                // Simplified logic - just get all available products in this category
                var allProducts = _context.Products
                    .Include(p => p.Category)
                    .Where(p => p.IsAvailable && p.CategoryID == gaSotCayCategory.CategoryID)
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
                    .OrderBy(p => p.ProductName)
                    .ToList();

                Console.WriteLine($"📦 Found {allProducts.Count} products in 'Gà Sốt Cay' category:");
                foreach (var product in allProducts)
                {
                    Console.WriteLine($"  - {product.ProductName} (ID: {product.ProductID}, Price: {product.Price}, IsConfigurable: {product.IsConfigurable})");
                }

                return Json(allProducts);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error in GetGaSotCayItems: {ex.Message}");
                Console.WriteLine($"❌ Stack trace: {ex.StackTrace}");
                return Json(new List<object>());
            }
        }

        // NEW: Get all menu items for "Burger/Cơm" category
        public JsonResult GetBurgerComItems()
        {
            try
            {
                Console.WriteLine("🔍 GetBurgerComItems called");
                
                // Get CategoryID for "Burger/Cơm" - try multiple variations
                var burgerComCategory = _context.Categories
                    .FirstOrDefault(c => c.CategoryName.Contains("Burger") || 
                                        c.CategoryName.Contains("burger") ||
                                        c.CategoryName.Contains("Cơm") ||
                                        c.CategoryName.Contains("com") ||
                                        c.CategoryName.Contains("Burger/Cơm"));
                
                if (burgerComCategory == null)
                {
                    Console.WriteLine("⚠️ 'Burger/Cơm' category not found!");
                    return Json(new List<object>());
                }

                Console.WriteLine($"📂 Found category: {burgerComCategory.CategoryName} (ID: {burgerComCategory.CategoryID})");

                // Simplified logic - just get all available products in this category
                var allProducts = _context.Products
                    .Include(p => p.Category)
                    .Where(p => p.IsAvailable && p.CategoryID == burgerComCategory.CategoryID)
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
                    .OrderBy(p => p.ProductName)
                    .ToList();

                Console.WriteLine($"📦 Found {allProducts.Count} products in 'Burger/Cơm' category:");
                foreach (var product in allProducts)
                {
                    Console.WriteLine($"  - {product.ProductName} (ID: {product.ProductID}, Price: {product.Price}, IsConfigurable: {product.IsConfigurable})");
                }

                return Json(allProducts);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error in GetBurgerComItems: {ex.Message}");
                Console.WriteLine($"❌ Stack trace: {ex.StackTrace}");
                return Json(new List<object>());
            }
        }

        // NEW: Get all menu items for "Phần ăn phụ" category
        public JsonResult GetPhanAnPhuItems()
        {
            try
            {
                Console.WriteLine("🔍 GetPhanAnPhuItems called");
                
                // Get CategoryID for "Phần ăn phụ" - try multiple variations
                var phanAnPhuCategory = _context.Categories
                    .FirstOrDefault(c => c.CategoryName.Contains("Phần ăn phụ") || 
                                        c.CategoryName.Contains("Phần Ăn Phụ") ||
                                        c.CategoryName.Contains("phan an phu") ||
                                        c.CategoryName.Contains("Phần ăn"));
                
                if (phanAnPhuCategory == null)
                {
                    Console.WriteLine("⚠️ 'Phần ăn phụ' category not found!");
                    return Json(new List<object>());
                }

                Console.WriteLine($"📂 Found category: {phanAnPhuCategory.CategoryName} (ID: {phanAnPhuCategory.CategoryID})");

                // Get IDs of products used as options in combos
                var comboOptionProductIds = _context.ProductConfigurationOptions
                    .Select(pco => pco.OptionProductID)
                    .Distinct()
                    .ToList();

                Console.WriteLine($"🚫 Excluding {comboOptionProductIds.Count} combo option products: {string.Join(", ", comboOptionProductIds)}");

                // Get all available products in this category, excluding combo option products
                var allProducts = _context.Products
                    .Include(p => p.Category)
                    .Where(p => p.IsAvailable && 
                               p.CategoryID == phanAnPhuCategory.CategoryID &&
                               (p.IsConfigurable || !comboOptionProductIds.Contains(p.ProductID)))
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

                Console.WriteLine($"📦 Found {allProducts.Count} products in 'Phần ăn phụ' category:");
                foreach (var product in allProducts)
                {
                    Console.WriteLine($"  - {product.ProductName} (ID: {product.ProductID}, Price: {product.Price}, IsConfigurable: {product.IsConfigurable})");
                }

                return Json(allProducts);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error in GetPhanAnPhuItems: {ex.Message}");
                Console.WriteLine($"❌ Stack trace: {ex.StackTrace}");
                return Json(new List<object>());
            }
        }

        // NEW: Get all menu items for "Món tráng miệng" category
        public JsonResult GetMonTrangMiengItems()
        {
            try
            {
                Console.WriteLine("🔍 GetMonTrangMiengItems called");
                
                // Get CategoryID for "Món tráng miệng" - try multiple variations
                var monTrangMiengCategory = _context.Categories
                    .FirstOrDefault(c => c.CategoryName.Contains("Món tráng miệng") || 
                                        c.CategoryName.Contains("Món Tráng Miệng") ||
                                        c.CategoryName.Contains("mon trang mieng") ||
                                        c.CategoryName.Contains("Tráng miệng") ||
                                        c.CategoryName.Contains("trang mieng"));
                
                if (monTrangMiengCategory == null)
                {
                    Console.WriteLine("⚠️ 'Món tráng miệng' category not found!");
                    return Json(new List<object>());
                }

                Console.WriteLine($"📂 Found category: {monTrangMiengCategory.CategoryName} (ID: {monTrangMiengCategory.CategoryID})");

                // Get IDs of products used as options in combos
                var comboOptionProductIds = _context.ProductConfigurationOptions
                    .Select(pco => pco.OptionProductID)
                    .Distinct()
                    .ToList();

                Console.WriteLine($"🚫 Excluding {comboOptionProductIds.Count} combo option products: {string.Join(", ", comboOptionProductIds)}");

                // Get all available products in this category, excluding combo option products
                var allProducts = _context.Products
                    .Include(p => p.Category)
                    .Where(p => p.IsAvailable && 
                               p.CategoryID == monTrangMiengCategory.CategoryID &&
                               (p.IsConfigurable || !comboOptionProductIds.Contains(p.ProductID)))
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

                Console.WriteLine($"📦 Found {allProducts.Count} products in 'Món tráng miệng' category:");
                foreach (var product in allProducts)
                {
                    Console.WriteLine($"  - {product.ProductName} (ID: {product.ProductID}, Price: {product.Price}, IsConfigurable: {product.IsConfigurable})");
                }

                return Json(allProducts);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error in GetMonTrangMiengItems: {ex.Message}");
                Console.WriteLine($"❌ Stack trace: {ex.StackTrace}");
                return Json(new List<object>());
            }
        }

        // NEW: Get all menu items for "Thức uống" category
        public JsonResult GetThucUongItems()
        {
            try
            {
                Console.WriteLine("🔍 GetThucUongItems called");
                
                // Get CategoryID for "Thức uống" - try multiple variations
                var thucUongCategory = _context.Categories
                    .FirstOrDefault(c => c.CategoryName.Contains("Thức uống") || 
                                        c.CategoryName.Contains("Thức Uống") ||
                                        c.CategoryName.Contains("thuc uong") ||
                                        c.CategoryName.Contains("Đồ uống") ||
                                        c.CategoryName.Contains("do uong"));
                
                if (thucUongCategory == null)
                {
                    Console.WriteLine("⚠️ 'Thức uống' category not found!");
                    return Json(new List<object>());
                }

                Console.WriteLine($"📂 Found category: {thucUongCategory.CategoryName} (ID: {thucUongCategory.CategoryID})");

                // Get IDs of products used as options in combos
                var comboOptionProductIds = _context.ProductConfigurationOptions
                    .Select(pco => pco.OptionProductID)
                    .Distinct()
                    .ToList();

                Console.WriteLine($"🚫 Excluding {comboOptionProductIds.Count} combo option products: {string.Join(", ", comboOptionProductIds)}");

                // Get all available products in this category, excluding combo option products
                var allProducts = _context.Products
                    .Include(p => p.Category)
                    .Where(p => p.IsAvailable && 
                               p.CategoryID == thucUongCategory.CategoryID &&
                               (p.IsConfigurable || !comboOptionProductIds.Contains(p.ProductID)))
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

                Console.WriteLine($"📦 Found {allProducts.Count} products in 'Thức uống' category:");
                foreach (var product in allProducts)
                {
                    Console.WriteLine($"  - {product.ProductName} (ID: {product.ProductID}, Price: {product.Price}, IsConfigurable: {product.IsConfigurable})");
                }

                return Json(allProducts);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error in GetThucUongItems: {ex.Message}");
                Console.WriteLine($"❌ Stack trace: {ex.StackTrace}");
                return Json(new List<object>());
            }
        }

        // DEBUG: Simple test for Mì Ý Jolly response format
        public JsonResult DebugMiYJolly()
        {
            var miYJollyCategory = _context.Categories
                .FirstOrDefault(c => c.CategoryName.Contains("Mì Ý Jolly") || 
                                    c.CategoryName.Contains("mi y jolly") ||
                                    c.CategoryName.Contains("Mì Ý"));
            
            if (miYJollyCategory == null)
            {
                return Json(new { error = "Category not found", searchTerms = new[] { "Mì Ý Jolly", "mi y jolly", "Mì Ý" } });
            }

            var products = _context.Products
                .Include(p => p.Category)
                .Where(p => p.CategoryID == miYJollyCategory.CategoryID)
                .Select(p => new {
                    p.ProductID,
                    p.ProductName,
                    p.IsAvailable,
                    p.IsConfigurable,
                    p.Price,
                    p.ImageUrl,
                    CategoryName = p.Category.CategoryName
                })
                .ToList();

            return Json(new {
                categoryFound = miYJollyCategory.CategoryName,
                categoryId = miYJollyCategory.CategoryID,
                totalProducts = products.Count,
                availableProducts = products.Where(p => p.IsAvailable).Count(),
                products = products
            });
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