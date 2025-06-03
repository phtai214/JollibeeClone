using JollibeeClone.Areas.Admin.Data;
using JollibeeClone.Areas.Admin.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using JollibeeClone.Areas.Admin.Attributes;
using System.Text.Json;

namespace JollibeeClone.Areas.Admin.Controllers
{
    [Area("Admin")]
    [AdminAuthorize]
    public class ProductController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly ILogger<ProductController> _logger;

        public ProductController(AppDbContext context, IWebHostEnvironment webHostEnvironment, ILogger<ProductController> logger)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
            _logger = logger;
        }

        // GET: Admin/Product
        public async Task<IActionResult> Index()
        {
            try
            {
                var products = await _context.Products
                    .Include(p => p.Category)
                    .OrderBy(p => p.ProductName)
                    .ToListAsync();
                return View(products);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading products");
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải danh sách sản phẩm.";
                return View(new List<Product>());
            }
        }

        // GET: Admin/Product/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) 
            {
                TempData["ErrorMessage"] = "ID sản phẩm không hợp lệ.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                var product = await _context.Products
                    .Include(p => p.Category)
                    .FirstOrDefaultAsync(m => m.ProductID == id);

                if (product == null) 
                {
                    TempData["ErrorMessage"] = "Không tìm thấy sản phẩm.";
                    return RedirectToAction(nameof(Index));
                }

                return View(product);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading product details for ID: {ProductId}", id);
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải thông tin sản phẩm.";
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: Admin/Product/Create
        public async Task<IActionResult> Create()
        {
            try
            {
                // Check if categories exist, if not create sample ones
                var categoriesExist = await _context.Categories.AnyAsync(c => c.IsActive);
                if (!categoriesExist)
                {
                    _logger.LogWarning("No active categories found, creating sample categories...");
                    await CreateSampleCategoriesAsync();
                }

                var categories = await _context.Categories.Where(c => c.IsActive).ToListAsync();
                ViewBag.CategoryID = new SelectList(categories, "CategoryID", "CategoryName");
                
                _logger.LogInformation("Create GET loaded with {CategoryCount} categories", categories.Count);
                
                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading create product page");
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải trang tạo sản phẩm.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Admin/Product/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ProductName,ShortDescription,Price,OriginalPrice,CategoryID,IsConfigurable,IsAvailable")] Product product, IFormFile? imageFile)
        {
            _logger.LogInformation("=== CREATE POST STARTED ===");
            _logger.LogInformation("Product data received:");
            _logger.LogInformation("- ProductName: {ProductName}", product.ProductName);
            _logger.LogInformation("- Price: {Price}", product.Price);
            _logger.LogInformation("- CategoryID: {CategoryID}", product.CategoryID);
            _logger.LogInformation("- ImageFile: {ImageFile}", imageFile?.FileName ?? "NULL");

            // Remove ModelState validation cho CategoryID và ImageUrl để tự handle
            ModelState.Remove("CategoryID");
            ModelState.Remove("ImageUrl");

            try
            {
                // Basic validation
                bool isValid = true;
                List<string> errors = new List<string>();

                if (string.IsNullOrWhiteSpace(product.ProductName))
                {
                    errors.Add("Tên sản phẩm là bắt buộc");
                    isValid = false;
                }

                if (product.Price <= 0)
                {
                    errors.Add("Giá sản phẩm phải lớn hơn 0");
                    isValid = false;
                }

                // Validate category manually
                if (product.CategoryID <= 0)
                {
                    errors.Add("Vui lòng chọn danh mục cho sản phẩm");
                    isValid = false;
                }
                else
                {
                    var categoryExists = await _context.Categories.AnyAsync(c => c.CategoryID == product.CategoryID && c.IsActive);
                    if (!categoryExists)
                    {
                        errors.Add("Danh mục được chọn không hợp lệ");
                        isValid = false;
                    }
                }

                // Handle image upload TRƯỚC khi validate ModelState
                if (imageFile != null && imageFile.Length > 0)
                {
                    _logger.LogInformation("Processing image file: {Name}, Size: {Length}", imageFile.FileName, imageFile.Length);
                    
                    var imageValidation = ValidateImageFile(imageFile);
                    if (!string.IsNullOrEmpty(imageValidation))
                    {
                        errors.Add(imageValidation);
                        isValid = false;
                    }
                    else
                    {
                        try
                        {
                            product.ImageUrl = await SaveImageFileAsync(imageFile);
                            _logger.LogInformation("Image uploaded successfully: {ImageUrl}", product.ImageUrl);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error uploading image");
                            errors.Add("Lỗi khi upload hình ảnh: " + ex.Message);
                            isValid = false;
                        }
                    }
                }
                else
                {
                    // Không có ảnh - để null
                    product.ImageUrl = null;
                    _logger.LogInformation("No image uploaded, product will be created without image");
                }

                // Validate remaining fields from ModelState
                foreach (var modelError in ModelState.Values.SelectMany(v => v.Errors))
                {
                    if (!string.IsNullOrEmpty(modelError.ErrorMessage))
                    {
                        errors.Add(modelError.ErrorMessage);
                        isValid = false;
                    }
                }

                if (isValid && errors.Count == 0)
                {
                    _logger.LogInformation("All validation passed, saving product to database...");
                    _logger.LogInformation("Product details before save: Name={ProductName}, Price={Price}, CategoryID={CategoryID}, ImageUrl={ImageUrl}", 
                        product.ProductName, product.Price, product.CategoryID, product.ImageUrl ?? "NULL");
                    
                    _context.Add(product);
                    var result = await _context.SaveChangesAsync();
                    
                    _logger.LogInformation("Product created successfully: {ProductName}. ProductID: {ProductID}, ImageUrl: {ImageUrl}", 
                        product.ProductName, product.ProductID, product.ImageUrl ?? "NULL");
                    
                    TempData["SuccessMessage"] = "Sản phẩm đã được tạo thành công!";
                    return RedirectToAction(nameof(Index));
                }

                // If validation failed, add errors to ModelState
                foreach (var error in errors)
                {
                    ModelState.AddModelError("", error);
                }
                
                _logger.LogWarning("Validation failed with errors: {Errors}", string.Join(", ", errors));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating product: {ProductName}", product.ProductName);
                ModelState.AddModelError("", "Có lỗi xảy ra khi tạo sản phẩm: " + ex.Message);
            }

            // Reload categories for dropdown khi có lỗi
            try
            {
                var categories = await _context.Categories.Where(c => c.IsActive).ToListAsync();
                ViewBag.CategoryID = new SelectList(categories, "CategoryID", "CategoryName", product.CategoryID);
                _logger.LogInformation("Reloaded {CategoryCount} categories for dropdown", categories.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading categories for dropdown");
                ViewBag.CategoryID = new SelectList(new List<object>(), "CategoryID", "CategoryName");
            }

            return View(product);
        }

        // GET: Admin/Product/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) 
            {
                TempData["ErrorMessage"] = "ID sản phẩm không hợp lệ.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                var product = await _context.Products.FindAsync(id);
                if (product == null) 
                {
                    TempData["ErrorMessage"] = "Không tìm thấy sản phẩm.";
                    return RedirectToAction(nameof(Index));
                }

                // Check if categories exist, if not create sample ones
                var categoriesExist = await _context.Categories.AnyAsync(c => c.IsActive);
                if (!categoriesExist)
                {
                    _logger.LogWarning("No active categories found, creating sample categories...");
                    await CreateSampleCategoriesAsync();
                }

                var categories = await _context.Categories.Where(c => c.IsActive).ToListAsync();
                ViewBag.CategoryID = new SelectList(categories, "CategoryID", "CategoryName", product.CategoryID);
                
                _logger.LogInformation("Edit GET loaded product {ProductId} with {CategoryCount} categories", id, categories.Count);
                return View(product);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading edit product page for ID: {ProductId}", id);
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải trang chỉnh sửa.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Admin/Product/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("ProductID,ProductName,ShortDescription,Price,OriginalPrice,CategoryID,IsConfigurable,IsAvailable")] Product product, IFormFile? imageFile)
        {
            _logger.LogInformation("=== EDIT POST STARTED ===");
            _logger.LogInformation("Product data received:");
            _logger.LogInformation("- ProductID: {ProductID}", product.ProductID);
            _logger.LogInformation("- ProductName: {ProductName}", product.ProductName);
            _logger.LogInformation("- Price: {Price}", product.Price);
            _logger.LogInformation("- CategoryID: {CategoryID}", product.CategoryID);
            _logger.LogInformation("- ImageFile: {ImageFile}", imageFile?.FileName ?? "NULL");

            if (id != product.ProductID) 
            {
                _logger.LogWarning("Product ID mismatch: URL ID = {UrlId}, Product.ProductID = {ProductId}", id, product.ProductID);
                TempData["ErrorMessage"] = "ID sản phẩm không khớp.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                // Basic validation
                bool isValid = true;
                List<string> errors = new List<string>();

                if (string.IsNullOrWhiteSpace(product.ProductName))
                {
                    errors.Add("Tên sản phẩm là bắt buộc.");
                    isValid = false;
                }
                else if (await _context.Products.AnyAsync(p => p.ProductName.ToLower() == product.ProductName.ToLower() && p.ProductID != id))
                {
                    errors.Add("Tên sản phẩm đã tồn tại.");
                    isValid = false;
                }

                if (product.Price <= 0)
                {
                    errors.Add("Giá sản phẩm phải lớn hơn 0.");
                    isValid = false;
                }

                if (product.OriginalPrice.HasValue && product.OriginalPrice <= product.Price)
                {
                    errors.Add("Giá gốc phải lớn hơn giá bán hiện tại.");
                    isValid = false;
                }

                // Validate category
                if (product.CategoryID <= 0)
                {
                    errors.Add("Vui lòng chọn danh mục cho sản phẩm.");
                    isValid = false;
                }
                else
                {
                    var categoryExists = await _context.Categories.AnyAsync(c => c.CategoryID == product.CategoryID && c.IsActive);
                    if (!categoryExists)
                    {
                        errors.Add("Danh mục được chọn không hợp lệ.");
                        isValid = false;
                    }
                }

                if (isValid)
                {
                    var existingProduct = await _context.Products.AsNoTracking().FirstOrDefaultAsync(p => p.ProductID == id);
                    
                    // Handle image upload
                    if (imageFile != null && imageFile.Length > 0)
                    {
                        _logger.LogInformation("Processing image file: {FileName}", imageFile.FileName);
                        var imageValidation = ValidateImageFile(imageFile);
                        if (!string.IsNullOrEmpty(imageValidation))
                        {
                            errors.Add(imageValidation);
                            isValid = false;
                        }
                        else
                        {
                            // Delete old image
                            if (!string.IsNullOrEmpty(existingProduct?.ImageUrl))
                            {
                                DeleteImageFile(existingProduct.ImageUrl);
                            }

                            product.ImageUrl = await SaveImageFileAsync(imageFile);
                            _logger.LogInformation("Image uploaded successfully: {ImageUrl}", product.ImageUrl);
                            _logger.LogInformation("ImageUrl set to: {ImageUrl}", product.ImageUrl);
                        }
                    }
                    else
                    {
                        // Keep existing image
                        product.ImageUrl = existingProduct?.ImageUrl;
                        _logger.LogInformation("Keeping existing image: {ImageUrl}", product.ImageUrl ?? "NULL");
                    }

                    if (isValid)
                    {
                        _logger.LogInformation("Updating product in database...");
                        _context.Update(product);
                        var result = await _context.SaveChangesAsync();
                        
                        _logger.LogInformation("Product updated successfully: {ProductName}. ImageUrl: {ImageUrl}", product.ProductName, product.ImageUrl ?? "NULL");
                        TempData["SuccessMessage"] = "Sản phẩm đã được cập nhật thành công!";
                        return RedirectToAction(nameof(Index));
                    }
                }

                // If validation failed
                foreach (var error in errors)
                {
                    ModelState.AddModelError("", error);
                }
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogError(ex, "Concurrency error updating product: {ProductId}", id);
                if (!await ProductExistsAsync(product.ProductID))
                {
                    TempData["ErrorMessage"] = "Sản phẩm không tồn tại.";
                    return RedirectToAction(nameof(Index));
                }
                else
                {
                    TempData["ErrorMessage"] = "Sản phẩm đã bị thay đổi bởi người dùng khác. Vui lòng thử lại.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating product: {ProductId}", id);
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi cập nhật sản phẩm: " + ex.Message;
            }

            // Reload categories for dropdown
            try
            {
                var categories = await _context.Categories.Where(c => c.IsActive).ToListAsync();
                ViewBag.CategoryID = new SelectList(categories, "CategoryID", "CategoryName", product.CategoryID);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading categories for dropdown");
                ViewBag.CategoryID = new SelectList(new List<object>(), "CategoryID", "CategoryName");
            }

            return View(product);
        }

        // GET: Admin/Product/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) 
            {
                TempData["ErrorMessage"] = "ID sản phẩm không hợp lệ.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                var product = await _context.Products
                    .Include(p => p.Category)
                    .FirstOrDefaultAsync(m => m.ProductID == id);

                if (product == null) 
                {
                    TempData["ErrorMessage"] = "Không tìm thấy sản phẩm.";
                    return RedirectToAction(nameof(Index));
                }

                return View(product);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading delete product page for ID: {ProductId}", id);
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải trang xóa sản phẩm.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Admin/Product/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                var product = await _context.Products.FindAsync(id);
                if (product != null)
                {
                    // Check if product is referenced in any orders (if you have OrderItem table)
                    // This is a placeholder - you may need to adjust based on your order system
                    // if (await _context.OrderItems.AnyAsync(oi => oi.ProductID == id))
                    // {
                    //     TempData["ErrorMessage"] = "Không thể xóa sản phẩm vì đã có trong đơn hàng!";
                    //     return RedirectToAction(nameof(Index));
                    // }

                    // Delete image file if exists
                    if (!string.IsNullOrEmpty(product.ImageUrl))
                    {
                        DeleteImageFile(product.ImageUrl);
                    }

                    _context.Products.Remove(product);
                    await _context.SaveChangesAsync();
                    
                    _logger.LogInformation("Product deleted successfully: {ProductName} by Admin", product.ProductName);
                    TempData["SuccessMessage"] = "Sản phẩm đã được xóa thành công!";
                }
                else
                {
                    TempData["ErrorMessage"] = "Không tìm thấy sản phẩm để xóa.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting product: {ProductId}", id);
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi xóa sản phẩm.";
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: Admin/Product/TestCategories - Debug action để kiểm tra categories
        [HttpGet]
        public async Task<IActionResult> TestCategories()
        {
            try
            {
                var allCategories = await _context.Categories.ToListAsync();
                var activeCategories = await _context.Categories.Where(c => c.IsActive).ToListAsync();
                
                var result = new
                {
                    TotalCategories = allCategories.Count,
                    ActiveCategories = activeCategories.Count,
                    Categories = allCategories.Select(c => new
                    {
                        c.CategoryID,
                        c.CategoryName,
                        c.IsActive,
                        c.DisplayOrder
                    }).ToList()
                };

                _logger.LogInformation("Categories test result: {Result}", JsonSerializer.Serialize(result));
                return Json(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error testing categories");
                return Json(new { error = ex.Message, stackTrace = ex.StackTrace });
            }
        }

        // GET: Admin/Product/TestUpdate/5 - Test direct database update
        [HttpGet]
        public async Task<IActionResult> TestUpdate(int id)
        {
            try
            {
                var product = await _context.Products.FindAsync(id);
                if (product == null)
                {
                    return Json(new { success = false, message = "Product not found" });
                }

                // Get the first available category
                var firstCategory = await _context.Categories.Where(c => c.IsActive).FirstOrDefaultAsync();
                if (firstCategory == null)
                {
                    return Json(new { success = false, message = "No categories available" });
                }

                // Simple update without validation
                product.CategoryID = firstCategory.CategoryID;
                product.ProductName = "Test Product Updated - " + DateTime.Now.ToString("HH:mm:ss");
                
                _context.Update(product);
                var result = await _context.SaveChangesAsync();

                return Json(new { 
                    success = true, 
                    message = $"Product updated successfully. SaveChanges result: {result}",
                    productId = product.ProductID,
                    categoryId = product.CategoryID,
                    categoryName = firstCategory.CategoryName
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in test update for product {ProductId}", id);
                return Json(new { success = false, message = ex.Message, stackTrace = ex.StackTrace });
            }
        }

        // GET: Admin/Product/TestData - Debug action để kiểm tra dữ liệu
        [HttpGet]
        public async Task<IActionResult> TestData()
        {
            try
            {
                var products = await _context.Products
                    .Include(p => p.Category)
                    .Take(10)
                    .ToListAsync();
                
                var result = new
                {
                    TotalProducts = await _context.Products.CountAsync(),
                    ProductsWithImages = await _context.Products.Where(p => !string.IsNullOrEmpty(p.ImageUrl)).CountAsync(),
                    ProductsWithoutImages = await _context.Products.Where(p => string.IsNullOrEmpty(p.ImageUrl)).CountAsync(),
                    SampleProducts = products.Select(p => new
                    {
                        p.ProductID,
                        p.ProductName,
                        p.ImageUrl,
                        CategoryName = p.Category?.CategoryName,
                        p.Price,
                        p.IsAvailable
                    }).ToList(),
                    ImagePaths = products.Where(p => !string.IsNullOrEmpty(p.ImageUrl))
                        .Select(p => new
                        {
                            ProductName = p.ProductName,
                            ImageUrl = p.ImageUrl,
                            ImageExists = !string.IsNullOrEmpty(p.ImageUrl) ? 
                                System.IO.File.Exists(Path.Combine(_webHostEnvironment.WebRootPath, p.ImageUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar))) : false
                        }).ToList()
                };

                _logger.LogInformation("Test data result: {Result}", System.Text.Json.JsonSerializer.Serialize(result));
                return Json(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in test data");
                return Json(new { error = ex.Message, stackTrace = ex.StackTrace });
            }
        }

        // GET: Admin/Product/TestImages - Test endpoint để verify images
        [HttpGet]
        public IActionResult TestImages()
        {
            try
            {
                var imagesPath = Path.Combine(_webHostEnvironment.WebRootPath, "assets", "images");
                var imageFiles = Directory.Exists(imagesPath) ? Directory.GetFiles(imagesPath) : new string[0];
                
                var testImages = new List<string>
                {
                    "/assets/images/1gacay_menu.jpg",
                    "/assets/images/2gacay_menu.jpg", 
                    "/assets/images/1gagion_menu.png",
                    "/assets/images/bee.png",
                    "/assets/images/hotdog_menu.png"
                };

                var result = new
                {
                    ImagesDirectoryExists = Directory.Exists(imagesPath),
                    ImagesPath = imagesPath,
                    TotalFiles = imageFiles.Length,
                    Files = imageFiles.Select(f => new
                    {
                        FileName = Path.GetFileName(f),
                        FullPath = f,
                        Size = new FileInfo(f).Length,
                        RelativeUrl = "/assets/images/" + Path.GetFileName(f)
                    }).ToList(),
                    TestImageResults = testImages.Select(img => new
                    {
                        ImageUrl = img,
                        PhysicalPath = Path.Combine(_webHostEnvironment.WebRootPath, img.TrimStart('/').Replace('/', Path.DirectorySeparatorChar)),
                        Exists = System.IO.File.Exists(Path.Combine(_webHostEnvironment.WebRootPath, img.TrimStart('/').Replace('/', Path.DirectorySeparatorChar)))
                    }).ToList()
                };

                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message, stackTrace = ex.StackTrace });
            }
        }

        // GET: Admin/Product/TestCreate - Test simple create without validation
        [HttpPost]
        public async Task<IActionResult> TestCreate(string productName, string selectedImageUrl)
        {
            try
            {
                _logger.LogInformation("=== TEST CREATE ===");
                _logger.LogInformation("ProductName: {ProductName}", productName);
                _logger.LogInformation("SelectedImageUrl: {SelectedImageUrl}", selectedImageUrl);

                var product = new Product
                {
                    ProductName = productName ?? "Test Product " + DateTime.Now.ToString("HH:mm:ss"),
                    Price = 50000,
                    CategoryID = 1, // Assume first category
                    ImageUrl = selectedImageUrl,
                    IsAvailable = true,
                    IsConfigurable = false
                };

                _context.Add(product);
                var result = await _context.SaveChangesAsync();
                
                return Json(new { 
                    success = true, 
                    message = "Test product created successfully",
                    productId = product.ProductID,
                    imageUrl = product.ImageUrl,
                    saveResult = result
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in test create");
                return Json(new { success = false, error = ex.Message });
            }
        }

        // Helper method to create sample categories if none exist
        private async Task CreateSampleCategoriesAsync()
        {
            try
            {
                var categories = new List<Categories>
                {
                    new Categories { CategoryName = "Gà rán", Description = "Gà rán giòn tan, thơm ngon", DisplayOrder = 1, IsActive = true },
                    new Categories { CategoryName = "Burger", Description = "Burger đa dạng hương vị", DisplayOrder = 2, IsActive = true },
                    new Categories { CategoryName = "Mì Ý", Description = "Mì Ý phong cách Ý chính thống", DisplayOrder = 3, IsActive = true },
                    new Categories { CategoryName = "Cơm", Description = "Cơm món Á đậm đà hương vị", DisplayOrder = 4, IsActive = true },
                    new Categories { CategoryName = "Thức uống", Description = "Đồ uống giải khát đa dạng", DisplayOrder = 5, IsActive = true },
                    new Categories { CategoryName = "Tráng miệng", Description = "Bánh kẹo, kem tráng miệng", DisplayOrder = 6, IsActive = true }
                };

                _context.Categories.AddRange(categories);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Created {Count} sample categories", categories.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating sample categories");
            }
        }

        // Helper methods
        private async Task<bool> ProductExistsAsync(int id)
        {
            return await _context.Products.AnyAsync(e => e.ProductID == id);
        }

        private string ValidateImageFile(IFormFile file)
        {
            // Check file size (5MB limit)
            if (file.Length > 5 * 1024 * 1024)
                return "Kích thước file không được vượt quá 5MB.";

            // Check file extension
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!allowedExtensions.Contains(extension))
                return "Chỉ chấp nhận file ảnh với định dạng: JPG, JPEG, PNG, GIF, WEBP.";

            // Check content type
            var allowedContentTypes = new[] { "image/jpeg", "image/png", "image/gif", "image/webp" };
            if (!allowedContentTypes.Contains(file.ContentType.ToLowerInvariant()))
                return "Định dạng file không hợp lệ.";

            return string.Empty;
        }

        private async Task<string> SaveImageFileAsync(IFormFile file)
        {
            var uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "products");
            Directory.CreateDirectory(uploadsFolder);
            var fileExtension = Path.GetExtension(file.FileName);
            var uniqueFileName = $"{Guid.NewGuid()}{fileExtension}";
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);
            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(fileStream);
            }
            return $"/uploads/products/{uniqueFileName}";
        }

        private void DeleteImageFile(string imageUrl)
        {
            if (string.IsNullOrEmpty(imageUrl)) return;

            try
            {
                // imageUrl có dạng "/uploads/products/filename.jpg"
                var imagePath = Path.Combine(_webHostEnvironment.WebRootPath, imageUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
                if (System.IO.File.Exists(imagePath))
                {
                    System.IO.File.Delete(imagePath);
                    _logger.LogInformation("Deleted image file: {ImagePath}", imagePath);
                }
                else
                {
                    _logger.LogWarning("Image file not found for deletion: {ImagePath}", imagePath);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to delete image file: {ImageUrl}", imageUrl);
            }
        }
    }
}
