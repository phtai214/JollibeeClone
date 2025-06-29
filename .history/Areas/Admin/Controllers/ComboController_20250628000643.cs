using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using JollibeeClone.Data;
using JollibeeClone.Models;
using JollibeeClone.ViewModels;
using JollibeeClone.Areas.Admin.Attributes;

namespace JollibeeClone.Areas.Admin.Controllers
{
    [Area("Admin")]
    [AdminAuthorize]
    public class ComboController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public ComboController(AppDbContext context, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
        }

        // GET: Admin/Combo
        public async Task<IActionResult> Index()
        {
            var combos = await _context.Products
                .Where(p => p.IsConfigurable == true)
                .Include(p => p.Category)
                .Include(p => p.ProductConfigurationGroups)
                    .ThenInclude(g => g.ProductConfigurationOptions)
                .OrderBy(p => p.ProductName)
                .ToListAsync();

            return View(combos);
        }

        // GET: Admin/Combo/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var combo = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.ProductConfigurationGroups)
                    .ThenInclude(g => g.ProductConfigurationOptions)
                        .ThenInclude(o => o.OptionProduct)
                .Include(p => p.ProductConfigurationGroups)
                    .ThenInclude(g => g.ProductConfigurationOptions)
                        .ThenInclude(o => o.Variant)
                .FirstOrDefaultAsync(p => p.ProductID == id && p.IsConfigurable == true);

            if (combo == null)
            {
                return NotFound();
            }

            var viewModel = new ComboDetailViewModel
            {
                ProductID = combo.ProductID,
                ComboName = combo.ProductName,
                ShortDescription = combo.ShortDescription,
                ComboPrice = combo.Price,
                ImageUrl = combo.ImageUrl,
                ThumbnailUrl = combo.ThumbnailUrl,
                CategoryName = combo.Category?.CategoryName ?? "N/A",
                IsAvailable = combo.IsAvailable,
                ConfigGroups = combo.ProductConfigurationGroups.Select(g => new ComboGroupDetailViewModel
                {
                    GroupName = g.GroupName,
                    MinSelections = g.MinSelections,
                    MaxSelections = g.MaxSelections,
                    DisplayOrder = g.DisplayOrder,
                    Options = g.ProductConfigurationOptions.Select(o => new ComboOptionDetailViewModel
                    {
                        ProductName = o.OptionProduct.ProductName,
                        VariantName = o.Variant?.VariantName,
                        Quantity = o.Quantity,
                        PriceAdjustment = o.PriceAdjustment,
                        IsDefault = o.IsDefault,
                        DisplayOrder = o.DisplayOrder,
                        ProductImageUrl = o.OptionProduct.ImageUrl,
                        ProductThumbnailUrl = o.OptionProduct.ThumbnailUrl,
                        CustomImageUrl = o.CustomImageUrl
                    }).OrderBy(o => o.DisplayOrder).ToList()
                }).OrderBy(g => g.DisplayOrder).ToList()
            };

            return View(viewModel);
        }

        // GET: Admin/Combo/Create
        public async Task<IActionResult> Create()
        {
            var viewModel = new ComboCreateViewModel
            {
                Categories = await _context.Categories
                    .Where(c => c.IsActive == true)
                    .OrderBy(c => c.CategoryName)
                    .ToListAsync(),
                AvailableProducts = await _context.Products
                    .Where(p => p.IsAvailable == true && p.IsConfigurable == false)
                    .OrderBy(p => p.ProductName)
                    .ToListAsync()
            };

            return View(viewModel);
        }

        // POST: Admin/Combo/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ComboCreateViewModel viewModel)
        {
            // Debug: Kiểm tra dữ liệu form
            System.Diagnostics.Debug.WriteLine($"=== COMBO CREATE DEBUG ===");
            System.Diagnostics.Debug.WriteLine($"ComboName: {viewModel.ComboName}");
            System.Diagnostics.Debug.WriteLine($"ConfigGroups Count: {viewModel.ConfigGroups?.Count ?? 0}");
            
            // Count total form fields và analyze
            int totalFields = 0;
            int configGroupsFields = 0;
            if (Request.HasFormContentType && Request.Form != null)
            {
                totalFields = Request.Form.Count;
                configGroupsFields = Request.Form.Keys.Count(k => k.Contains("ConfigGroups"));
                
                System.Diagnostics.Debug.WriteLine($"=== MEGA COMBO FORM ANALYSIS ===");
                System.Diagnostics.Debug.WriteLine($"Total form fields: {totalFields}");
                System.Diagnostics.Debug.WriteLine($"ConfigGroups related fields: {configGroupsFields}");
                System.Diagnostics.Debug.WriteLine($"Current MaxModelBindingCollectionSize: 20480");
                System.Diagnostics.Debug.WriteLine($"Current ValueCountLimit: 25600");
                
                if (totalFields > 20000)
                {
                    System.Diagnostics.Debug.WriteLine($"🚨 CRITICAL: Extremely high field count! May exceed limits.");
                }
                else if (totalFields > 10000)
                {
                    System.Diagnostics.Debug.WriteLine($"⚠️  WARNING: Very high field count detected!");
                }
                else if (totalFields > 5000)
                {
                    System.Diagnostics.Debug.WriteLine($"ℹ️  INFO: High field count - this is expected for mega combos.");
                }
                
                // Show sample field names for debugging
                var configFields = Request.Form.Keys.Where(k => k.Contains("ConfigGroups")).Take(5);
                System.Diagnostics.Debug.WriteLine($"Sample ConfigGroups fields:");
                foreach (var field in configFields)
                {
                    var value = Request.Form[field].ToString();
                    if (value.Length > 50) value = value.Substring(0, 50) + "...";
                    System.Diagnostics.Debug.WriteLine($"  {field} = {value}");
                }
                
                if (configGroupsFields > 5)
                {
                    System.Diagnostics.Debug.WriteLine($"  ... and {configGroupsFields - 5} more ConfigGroups fields");
                }
            }
            
            if (viewModel.ConfigGroups != null)
            {
                for (int i = 0; i < viewModel.ConfigGroups.Count; i++)
                {
                    var group = viewModel.ConfigGroups[i];
                    System.Diagnostics.Debug.WriteLine($"Group {i}: '{group.GroupName}' - Options: {group.Options?.Count ?? 0}");
                    
                    if (group.Options != null)
                    {
                        for (int j = 0; j < group.Options.Count; j++)
                        {
                            var option = group.Options[j];
                            System.Diagnostics.Debug.WriteLine($"  Option {j}: ProductID={option.ProductID}, VariantID={option.VariantID}, Quantity={option.Quantity}");
                        }
                    }
                }
            }

            // Debug ModelState validation
            if (!ModelState.IsValid)
            {
                System.Diagnostics.Debug.WriteLine("=== MODELSTATE VALIDATION ERRORS ===");
                foreach (var kvp in ModelState)
                {
                    if (kvp.Value.Errors.Count > 0)
                    {
                        System.Diagnostics.Debug.WriteLine($"Field: {kvp.Key}");
                        foreach (var error in kvp.Value.Errors)
                        {
                            System.Diagnostics.Debug.WriteLine($"  Error: {error.ErrorMessage}");
                            if (error.Exception != null)
                            {
                                System.Diagnostics.Debug.WriteLine($"  Exception: {error.Exception.Message}");
                            }
                        }
                    }
                }
            }

            if (ModelState.IsValid)
            {
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    // Upload ảnh combo
                    string? imageUrl = null;
                    string? thumbnailUrl = null;

                    if (viewModel.ImageFile != null)
                    {
                        imageUrl = await UploadImage(viewModel.ImageFile, "products");
                    }

                    if (viewModel.ThumbnailFile != null)
                    {
                        thumbnailUrl = await UploadImage(viewModel.ThumbnailFile, "products");
                    }

                    // Tạo sản phẩm combo
                    var combo = new Product
                    {
                        ProductName = viewModel.ComboName,
                        ShortDescription = viewModel.ShortDescription,
                        Price = viewModel.ComboPrice,
                        CategoryID = viewModel.CategoryID,
                        ImageUrl = imageUrl,
                        ThumbnailUrl = thumbnailUrl,
                        IsConfigurable = true,
                        IsAvailable = true
                    };

                    _context.Products.Add(combo);
                    await _context.SaveChangesAsync();

                    // Tạo các nhóm cấu hình
                    foreach (var groupViewModel in viewModel.ConfigGroups)
                    {
                        var group = new ProductConfigurationGroup
                        {
                            MainProductID = combo.ProductID,
                            GroupName = groupViewModel.GroupName,
                            MinSelections = groupViewModel.MinSelections,
                            MaxSelections = groupViewModel.MaxSelections,
                            DisplayOrder = groupViewModel.DisplayOrder
                        };

                        _context.ProductConfigurationGroups.Add(group);
                        await _context.SaveChangesAsync();

                        // Tạo các option trong nhóm
                        int optionIndex = 0;
                        int validOptionsCount = 0;
                        foreach (var optionViewModel in groupViewModel.Options)
                        {
                            System.Diagnostics.Debug.WriteLine($"Processing option {optionIndex} in group '{groupViewModel.GroupName}':");
                            System.Diagnostics.Debug.WriteLine($"  ProductID: {optionViewModel.ProductID}");
                            System.Diagnostics.Debug.WriteLine($"  VariantID: {optionViewModel.VariantID}");
                            System.Diagnostics.Debug.WriteLine($"  Quantity: {optionViewModel.Quantity}");
                            
                            // Skip invalid options (empty ones from form)
                            if (optionViewModel.ProductID <= 0 || !optionViewModel.VariantID.HasValue || optionViewModel.VariantID <= 0)
                            {
                                System.Diagnostics.Debug.WriteLine($"Skipping invalid option {optionIndex} in group '{groupViewModel.GroupName}': ProductID={optionViewModel.ProductID}, VariantID={optionViewModel.VariantID}");
                                optionIndex++;
                                continue;
                            }
                            
                            optionIndex++;

                            string? customImageUrl = null;
                            if (optionViewModel.CustomImageFile != null)
                            {
                                customImageUrl = await UploadImage(optionViewModel.CustomImageFile, "products");
                            }

                            var option = new ProductConfigurationOption
                            {
                                ConfigGroupID = group.ConfigGroupID,
                                OptionProductID = optionViewModel.ProductID,
                                VariantID = optionViewModel.VariantID.Value,
                                Quantity = optionViewModel.Quantity,
                                PriceAdjustment = optionViewModel.PriceAdjustment,
                                CustomImageUrl = customImageUrl,
                                IsDefault = optionViewModel.IsDefault,
                                DisplayOrder = optionViewModel.DisplayOrder
                            };

                            System.Diagnostics.Debug.WriteLine($"Creating option: ProductID={option.OptionProductID}, VariantID={option.VariantID}, ConfigGroupID={option.ConfigGroupID}");
                            _context.ProductConfigurationOptions.Add(option);
                            validOptionsCount++;
                        }
                        
                        // Validate that group has at least one valid option
                        if (validOptionsCount == 0)
                        {
                            System.Diagnostics.Debug.WriteLine($"❌ Group '{groupViewModel.GroupName}' has no valid options!");
                            throw new InvalidOperationException($"Group '{groupViewModel.GroupName}' must have at least one valid option with both Product and Variant selected.");
                        }
                        
                        System.Diagnostics.Debug.WriteLine($"✅ Group '{groupViewModel.GroupName}' completed with {validOptionsCount} valid options");
                        
                        // Save options ngay sau khi tạo group
                        await _context.SaveChangesAsync();
                    }

                    // Commit transaction nếu thành công
                    await transaction.CommitAsync();
                    
                    TempData["SuccessMessage"] = "Combo đã được tạo thành công!";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    // Rollback transaction nếu có lỗi
                    await transaction.RollbackAsync();
                    ModelState.AddModelError("", "Đã xảy ra lỗi khi tạo combo: " + ex.Message);
                }
            }

            // Reload dropdown data nếu có lỗi
            viewModel.Categories = await _context.Categories
                .Where(c => c.IsActive == true)
                .OrderBy(c => c.CategoryName)
                .ToListAsync();
            viewModel.AvailableProducts = await _context.Products
                .Where(p => p.IsAvailable == true && p.IsConfigurable == false)
                .OrderBy(p => p.ProductName)
                .ToListAsync();

            return View(viewModel);
        }

        // Kiểm tra dữ liệu test
        public async Task<IActionResult> Test()
        {
            ViewBag.Products = await _context.Products.ToListAsync();
            ViewBag.Variants = await _context.ProductVariants.Include(v => v.Product).ToListAsync();
            return View();
        }

        // Test variant API
        public IActionResult VariantTest()
        {
            return View();
        }

        // API để lấy danh sách products
        [HttpGet]
        public async Task<IActionResult> GetProducts()
        {
            var products = await _context.Products
                .Where(p => p.IsAvailable == true && p.IsConfigurable == false)
                .OrderBy(p => p.ProductName)
                .Select(p => new
                {
                    p.ProductID,
                    p.ProductName,
                    p.Price
                })
                .ToListAsync();

            return Json(products);
        }

        // API để lấy variants của sản phẩm
        [HttpGet]
        public async Task<IActionResult> GetProductVariants(int productId)
        {
            var variants = await _context.ProductVariants
                .Where(v => v.ProductID == productId && v.IsAvailable == true)
                .OrderBy(v => v.DisplayOrder)
                .Select(v => new
                {
                    v.VariantID,
                    v.VariantName,
                    v.PriceAdjustment
                })
                .ToListAsync();

            return Json(variants);
        }

        // API để lấy thông tin sản phẩm
        [HttpGet]
        public async Task<IActionResult> GetProductInfo(int productId)
        {
            var product = await _context.Products
                .Where(p => p.ProductID == productId)
                .Select(p => new
                {
                    p.ProductID,
                    p.ProductName,
                    p.Price,
                    p.ImageUrl,
                    p.ThumbnailUrl
                })
                .FirstOrDefaultAsync();

            return Json(product);
        }

        // Helper method để upload ảnh
        private async Task<string> UploadImage(IFormFile imageFile, string folder)
        {
            var uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", folder);
            
            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            var uniqueFileName = Guid.NewGuid().ToString() + Path.GetExtension(imageFile.FileName);
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await imageFile.CopyToAsync(fileStream);
            }

            return $"/uploads/{folder}/{uniqueFileName}";
        }

        // GET: Admin/Combo/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var combo = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.ProductConfigurationGroups)
                    .ThenInclude(g => g.ProductConfigurationOptions)
                        .ThenInclude(o => o.OptionProduct)
                .Include(p => p.ProductConfigurationGroups)
                    .ThenInclude(g => g.ProductConfigurationOptions)
                        .ThenInclude(o => o.Variant)
                .FirstOrDefaultAsync(p => p.ProductID == id && p.IsConfigurable == true);

            if (combo == null)
            {
                return NotFound();
            }

            var viewModel = new ComboCreateViewModel
            {
                ComboName = combo.ProductName,
                ShortDescription = combo.ShortDescription,
                ComboPrice = combo.Price,
                CategoryID = combo.CategoryID,
                Categories = await _context.Categories
                    .Where(c => c.IsActive == true)
                    .OrderBy(c => c.CategoryName)
                    .ToListAsync(),
                AvailableProducts = await _context.Products
                    .Where(p => p.IsAvailable == true && p.IsConfigurable == false)
                    .OrderBy(p => p.ProductName)
                    .ToListAsync(),
                ConfigGroups = combo.ProductConfigurationGroups.OrderBy(g => g.DisplayOrder).Select(g => new ComboGroupViewModel
                {
                    GroupName = g.GroupName,
                    MinSelections = g.MinSelections,
                    MaxSelections = g.MaxSelections,
                    DisplayOrder = g.DisplayOrder,
                    Options = g.ProductConfigurationOptions.OrderBy(o => o.DisplayOrder).Select(o => new ComboOptionViewModel
                    {
                        ProductID = o.OptionProductID,
                        VariantID = o.VariantID,
                        Quantity = o.Quantity,
                        PriceAdjustment = o.PriceAdjustment,
                        IsDefault = o.IsDefault,
                        DisplayOrder = o.DisplayOrder,
                        ProductName = o.OptionProduct.ProductName,
                        VariantName = o.Variant?.VariantName,
                        ProductPrice = o.OptionProduct.Price,
                        ProductImageUrl = o.OptionProduct.ImageUrl,
                        ProductThumbnailUrl = o.OptionProduct.ThumbnailUrl
                    }).ToList()
                }).ToList()
            };

            ViewBag.ComboId = id;
            return View(viewModel);
        }

        // POST: Admin/Combo/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, ComboCreateViewModel viewModel)
        {
            var combo = await _context.Products
                .Include(p => p.ProductConfigurationGroups)
                    .ThenInclude(g => g.ProductConfigurationOptions)
                .FirstOrDefaultAsync(p => p.ProductID == id && p.IsConfigurable == true);

            if (combo == null)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Update combo basic info
                    combo.ProductName = viewModel.ComboName;
                    combo.ShortDescription = viewModel.ShortDescription;
                    combo.Price = viewModel.ComboPrice;
                    combo.CategoryID = viewModel.CategoryID;

                    // Upload new images if provided
                    if (viewModel.ImageFile != null)
                    {
                        combo.ImageUrl = await UploadImage(viewModel.ImageFile, "products");
                    }

                    if (viewModel.ThumbnailFile != null)
                    {
                        combo.ThumbnailUrl = await UploadImage(viewModel.ThumbnailFile, "products");
                    }

                    // Remove existing configuration
                    foreach (var group in combo.ProductConfigurationGroups)
                    {
                        _context.ProductConfigurationOptions.RemoveRange(group.ProductConfigurationOptions);
                    }
                    _context.ProductConfigurationGroups.RemoveRange(combo.ProductConfigurationGroups);

                    await _context.SaveChangesAsync();

                    // Add new configuration
                    foreach (var groupViewModel in viewModel.ConfigGroups)
                    {
                        var group = new ProductConfigurationGroup
                        {
                            MainProductID = combo.ProductID,
                            GroupName = groupViewModel.GroupName,
                            MinSelections = groupViewModel.MinSelections,
                            MaxSelections = groupViewModel.MaxSelections,
                            DisplayOrder = groupViewModel.DisplayOrder
                        };

                        _context.ProductConfigurationGroups.Add(group);
                        await _context.SaveChangesAsync();

                        foreach (var optionViewModel in groupViewModel.Options)
                        {
                            string? customImageUrl = null;
                            if (optionViewModel.CustomImageFile != null)
                            {
                                customImageUrl = await UploadImage(optionViewModel.CustomImageFile, "products");
                            }

                            var option = new ProductConfigurationOption
                            {
                                ConfigGroupID = group.ConfigGroupID,
                                OptionProductID = optionViewModel.ProductID,
                                VariantID = optionViewModel.VariantID,
                                Quantity = optionViewModel.Quantity,
                                PriceAdjustment = optionViewModel.PriceAdjustment,
                                CustomImageUrl = customImageUrl,
                                IsDefault = optionViewModel.IsDefault,
                                DisplayOrder = optionViewModel.DisplayOrder
                            };

                            _context.ProductConfigurationOptions.Add(option);
                        }
                    }

                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Combo đã được cập nhật thành công!";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Đã xảy ra lỗi khi cập nhật combo: " + ex.Message);
                }
            }

            // Reload dropdown data if error
            viewModel.Categories = await _context.Categories
                .Where(c => c.IsActive == true)
                .OrderBy(c => c.CategoryName)
                .ToListAsync();
            viewModel.AvailableProducts = await _context.Products
                .Where(p => p.IsAvailable == true && p.IsConfigurable == false)
                .OrderBy(p => p.ProductName)
                .ToListAsync();

            ViewBag.ComboId = id;
            return View(viewModel);
        }

        // GET: Admin/Combo/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var combo = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.ProductConfigurationGroups)
                    .ThenInclude(g => g.ProductConfigurationOptions)
                        .ThenInclude(o => o.OptionProduct)
                .Include(p => p.ProductConfigurationGroups)
                    .ThenInclude(g => g.ProductConfigurationOptions)
                        .ThenInclude(o => o.Variant)
                .FirstOrDefaultAsync(p => p.ProductID == id && p.IsConfigurable == true);

            if (combo == null)
            {
                return NotFound();
            }

            // Map to ComboDetailViewModel for consistent display
            var viewModel = new ComboDetailViewModel
            {
                ProductID = combo.ProductID,
                ComboName = combo.ProductName,
                ShortDescription = combo.ShortDescription,
                ComboPrice = combo.Price,
                ImageUrl = combo.ImageUrl,
                ThumbnailUrl = combo.ThumbnailUrl,
                CategoryName = combo.Category?.CategoryName ?? "N/A",
                IsAvailable = combo.IsAvailable,
                ConfigGroups = combo.ProductConfigurationGroups.Select(g => new ComboGroupDetailViewModel
                {
                    GroupName = g.GroupName,
                    MinSelections = g.MinSelections,
                    MaxSelections = g.MaxSelections,
                    DisplayOrder = g.DisplayOrder,
                    Options = g.ProductConfigurationOptions.Select(o => new ComboOptionDetailViewModel
                    {
                        ProductName = o.OptionProduct.ProductName,
                        VariantName = o.Variant?.VariantName,
                        Quantity = o.Quantity,
                        PriceAdjustment = o.PriceAdjustment,
                        IsDefault = o.IsDefault,
                        DisplayOrder = o.DisplayOrder,
                        ProductImageUrl = o.OptionProduct.ImageUrl,
                        ProductThumbnailUrl = o.OptionProduct.ThumbnailUrl,
                        CustomImageUrl = o.CustomImageUrl
                    }).OrderBy(o => o.DisplayOrder).ToList()
                }).OrderBy(g => g.DisplayOrder).ToList()
            };

            return View(viewModel);
        }

        // POST: Admin/Combo/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var combo = await _context.Products
                .Include(p => p.ProductConfigurationGroups)
                    .ThenInclude(g => g.ProductConfigurationOptions)
                .FirstOrDefaultAsync(p => p.ProductID == id && p.IsConfigurable == true);

            if (combo != null)
            {
                // Xóa tất cả configuration options
                foreach (var group in combo.ProductConfigurationGroups)
                {
                    _context.ProductConfigurationOptions.RemoveRange(group.ProductConfigurationOptions);
                }

                // Xóa tất cả configuration groups
                _context.ProductConfigurationGroups.RemoveRange(combo.ProductConfigurationGroups);

                // Xóa combo
                _context.Products.Remove(combo);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Combo đã được xóa thành công!";
            }

            return RedirectToAction(nameof(Index));
        }

        // Debug action để test tạo combo
        public async Task<IActionResult> TestCreateCombo()
        {
            try
            {
                // Tạo combo test đơn giản
                var combo = new Product
                {
                    ProductName = "Test Combo " + DateTime.Now.ToString("HHmmss"),
                    ShortDescription = "Test combo description",
                    Price = 99000,
                    CategoryID = 1, // Assuming category 1 exists
                    IsConfigurable = true,
                    IsAvailable = true
                };

                _context.Products.Add(combo);
                await _context.SaveChangesAsync();

                // Tạo group test
                var group = new ProductConfigurationGroup
                {
                    MainProductID = combo.ProductID,
                    GroupName = "Test Group",
                    MinSelections = 1,
                    MaxSelections = 1,
                    DisplayOrder = 0
                };

                _context.ProductConfigurationGroups.Add(group);
                await _context.SaveChangesAsync();

                // Lấy sản phẩm đầu tiên và variant đầu tiên
                var firstProduct = await _context.Products
                    .Where(p => p.IsAvailable && !p.IsConfigurable)
                    .FirstOrDefaultAsync();
                    
                var firstVariant = await _context.ProductVariants
                    .Where(v => v.ProductID == firstProduct.ProductID && v.IsAvailable)
                    .FirstOrDefaultAsync();

                if (firstProduct != null && firstVariant != null)
                {
                    // Tạo option test
                    var option = new ProductConfigurationOption
                    {
                        ConfigGroupID = group.ConfigGroupID,
                        OptionProductID = firstProduct.ProductID,
                        VariantID = firstVariant.VariantID,
                        Quantity = 1,
                        PriceAdjustment = 0,
                        IsDefault = true,
                        DisplayOrder = 0
                    };

                    _context.ProductConfigurationOptions.Add(option);
                    await _context.SaveChangesAsync();

                    return Json(new { 
                        success = true, 
                        message = "Test combo created successfully!", 
                        comboId = combo.ProductID,
                        productUsed = firstProduct.ProductName,
                        variantUsed = firstVariant.VariantName
                    });
                }
                else
                {
                    return Json(new { 
                        success = false, 
                        message = "No products or variants found for testing" 
                    });
                }
            }
            catch (Exception ex)
            {
                return Json(new { 
                    success = false, 
                    message = ex.Message,
                    stackTrace = ex.StackTrace
                });
            }
        }
    }
} 