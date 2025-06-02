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
        public async Task<IActionResult> Create([Bind("ProductName,ShortDescription,Price,OriginalPrice,ImageUrl,CategoryID,IsConfigurable,IsAvailable")] Product product, IFormFile? imageFile, string? selectedImageUrl)
        {
            _logger.LogInformation("=== CREATE POST STARTED ===");
            _logger.LogInformation("Product data received:");
            _logger.LogInformation("- ProductName: {ProductName}", product.ProductName);
            _logger.LogInformation("- Price: {Price}", product.Price);
            _logger.LogInformation("- CategoryID: {CategoryID}", product.CategoryID);
            _logger.LogInformation("- IsConfigurable: {IsConfigurable}", product.IsConfigurable);
            _logger.LogInformation("- IsAvailable: {IsAvailable}", product.IsAvailable);
            _logger.LogInformation("- ImageFile: {ImageFile}", imageFile?.FileName ?? "NULL");
            _logger.LogInformation("- SelectedImageUrl: {SelectedImageUrl}", selectedImageUrl ?? "NULL");

            // Log raw form data from Request
            _logger.LogInformation("Raw form data:");
            foreach (var key in Request.Form.Keys)
            {
                _logger.LogInformation("Form[{Key}] = {Value}", key, Request.Form[key]);
            }

            // Remove all ModelState entries for CategoryID to start fresh
            ModelState.Remove("CategoryID");
            
            try
            {
                // Basic validation without using ModelState first
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

                _logger.LogInformation("Manual validation result: {IsValid}, Errors: {Errors}", isValid, string.Join(", ", errors));

                if (isValid)
                {
                    _logger.LogInformation("Validation passed, processing image...");

                    // Xử lý ảnh: Ưu tiên file upload, sau đó mới đến ảnh có sẵn
                    if (imageFile != null && imageFile.Length > 0)
                    {
                        _logger.LogInformation("Processing uploaded image file: {FileName}, Size: {Size}", imageFile.FileName, imageFile.Length);
                        
                        var imageValidation = ValidateImageFile(imageFile);
                        if (!string.IsNullOrEmpty(imageValidation))
                        {
                            errors.Add(imageValidation);
                            isValid = false;
                        }
                        else
                        {
                            product.ImageUrl = await SaveImageFileAsync(imageFile, "products");
                            _logger.LogInformation("Image uploaded successfully: {ImageUrl}", product.ImageUrl);
                        }
                    }
                    else if (!string.IsNullOrEmpty(selectedImageUrl))
                    {
                        _logger.LogInformation("Using selected existing image: {SelectedImageUrl}", selectedImageUrl);
                        
                        // Kiểm tra xem file có tồn tại thực sự không
                        var imagePath = Path.Combine(_webHostEnvironment.WebRootPath, selectedImageUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
                        if (System.IO.File.Exists(imagePath))
                        {
                            product.ImageUrl = selectedImageUrl;
                            _logger.LogInformation("Selected image exists, using: {ImageUrl}", product.ImageUrl);
                        }
                        else
                        {
                            errors.Add($"Ảnh được chọn không tồn tại: {selectedImageUrl}");
                            isValid = false;
                            _logger.LogWarning("Selected image does not exist: {ImagePath}", imagePath);
                        }
                    }

                    if (isValid)
                    {
                        _logger.LogInformation("Final validation passed, saving product to database...");
                        _logger.LogInformation("Product ImageUrl before save: {ImageUrl}", product.ImageUrl ?? "NULL");
                        
                        _context.Add(product);
                        var result = await _context.SaveChangesAsync();
                        
                        _logger.LogInformation("Product created successfully: {ProductName}. SaveChanges result: {Result}. ProductID: {ProductID}", 
                            product.ProductName, result, product.ProductID);
                        _logger.LogInformation("Product ImageUrl after save: {ImageUrl}", product.ImageUrl ?? "NULL");
                        
                        TempData["SuccessMessage"] = "Sản phẩm đã được tạo thành công!";
                        return RedirectToAction(nameof(Index));
                    }
                }

                // If we get here, there were validation errors
                foreach (var error in errors)
                {
                    ModelState.AddModelError("", error);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating product: {ProductName}", product.ProductName);
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tạo sản phẩm: " + ex.Message;
            }

            // Reload categories for dropdown
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

            _logger.LogInformation("=== CREATE POST RETURNING TO VIEW ===");
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
        public async Task<IActionResult> Edit(int id, [Bind("ProductID,ProductName,ShortDescription,Price,OriginalPrice,ImageUrl,CategoryID,IsConfigurable,IsAvailable")] Product product, IFormFile? imageFile)
        {
            _logger.LogInformation("=== EDIT POST STARTED ===");
            _logger.LogInformation("Product data received:");
            _logger.LogInformation("- ProductID: {ProductID}", product.ProductID);
            _logger.LogInformation("- ProductName: {ProductName}", product.ProductName);
            _logger.LogInformation("- Price: {Price}", product.Price);
            _logger.LogInformation("- CategoryID: {CategoryID}", product.CategoryID);
            _logger.LogInformation("- IsConfigurable: {IsConfigurable}", product.IsConfigurable);
            _logger.LogInformation("- IsAvailable: {IsAvailable}", product.IsAvailable);

            // Log raw form data
            _logger.LogInformation("Raw form data:");
            foreach (var key in Request.Form.Keys)
            {
                _logger.LogInformation("Form[{Key}] = {Value}", key, Request.Form[key]);
            }

            if (id != product.ProductID) 
            {
                _logger.LogWarning("Product ID mismatch: URL ID = {UrlId}, Product.ProductID = {ProductId}", id, product.ProductID);
                TempData["ErrorMessage"] = "ID sản phẩm không khớp.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                // Check if categories exist in database
                var categoriesCount = await _context.Categories.Where(c => c.IsActive).CountAsync();
                _logger.LogInformation("Active categories count in database: {Count}", categoriesCount);
                
                if (categoriesCount == 0)
                {
                    _logger.LogWarning("No active categories found in database!");
                    TempData["ErrorMessage"] = "Không có danh mục nào trong hệ thống. Vui lòng tạo danh mục trước khi cập nhật sản phẩm.";
                    return RedirectToAction("Index", "Category");
                }

                // Custom validation
                if (await _context.Products.AnyAsync(p => p.ProductName.ToLower() == product.ProductName.ToLower() && p.ProductID != id))
                {
                    ModelState.AddModelError("ProductName", "Tên sản phẩm đã tồn tại.");
                    _logger.LogWarning("Product name already exists: {ProductName}", product.ProductName);
                }

                // Validate price
                if (product.Price <= 0)
                {
                    ModelState.AddModelError("Price", "Giá sản phẩm phải lớn hơn 0.");
                    _logger.LogWarning("Invalid price: {Price}", product.Price);
                }

                if (product.OriginalPrice.HasValue && product.OriginalPrice <= product.Price)
                {
                    ModelState.AddModelError("OriginalPrice", "Giá gốc phải lớn hơn giá bán hiện tại.");
                    _logger.LogWarning("Invalid original price: {OriginalPrice} vs {Price}", product.OriginalPrice, product.Price);
                }

                // Validate category - CategoryID is required (consistent with Create action)
                _logger.LogInformation("Validating CategoryID: {CategoryID}", product.CategoryID);
                if (product.CategoryID <= 0)
                {
                    ModelState.AddModelError("CategoryID", "Vui lòng chọn danh mục cho sản phẩm.");
                    _logger.LogWarning("CategoryID is invalid: {CategoryID}", product.CategoryID);
                }
                else
                {
                    var categoryExists = await _context.Categories.AnyAsync(c => c.CategoryID == product.CategoryID && c.IsActive);
                    _logger.LogInformation("Category exists check for ID {CategoryID}: {Exists}", product.CategoryID, categoryExists);
                    if (!categoryExists)
                    {
                        ModelState.AddModelError("CategoryID", "Danh mục được chọn không hợp lệ.");
                        _logger.LogWarning("Category does not exist or is inactive: {CategoryID}", product.CategoryID);
                    }
                }

                // Log ModelState
                _logger.LogInformation("ModelState.IsValid: {IsValid}", ModelState.IsValid);
                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("ModelState errors:");
                    foreach (var error in ModelState)
                    {
                        foreach (var errorMsg in error.Value.Errors)
                        {
                            _logger.LogWarning("- {Key}: {Error}", error.Key, errorMsg.ErrorMessage);
                        }
                    }
                }

                if (ModelState.IsValid)
                {
                    _logger.LogInformation("ModelState is valid, proceeding with update...");
                    var existingProduct = await _context.Products.AsNoTracking().FirstOrDefaultAsync(p => p.ProductID == id);
                    
                    // Handle image upload
                    if (imageFile != null && imageFile.Length > 0)
                    {
                        _logger.LogInformation("Processing image file: {FileName}", imageFile.FileName);
                        var imageValidation = ValidateImageFile(imageFile);
                        if (!string.IsNullOrEmpty(imageValidation))
                        {
                            ModelState.AddModelError("ImageFile", imageValidation);
                            _logger.LogWarning("Image validation failed: {Error}", imageValidation);
                            ViewBag.CategoryID = new SelectList(await _context.Categories.Where(c => c.IsActive).ToListAsync(), "CategoryID", "CategoryName", product.CategoryID);
                            return View(product);
                        }

                        // Delete old image
                        if (!string.IsNullOrEmpty(existingProduct?.ImageUrl))
                        {
                            DeleteImageFile(existingProduct.ImageUrl);
                        }

                        product.ImageUrl = await SaveImageFileAsync(imageFile, "products");
                        _logger.LogInformation("Image uploaded successfully: {ImageUrl}", product.ImageUrl);
                    }
                    else
                    {
                        // Keep existing image
                        product.ImageUrl = existingProduct?.ImageUrl;
                        _logger.LogInformation("Keeping existing image: {ImageUrl}", product.ImageUrl ?? "NULL");
                    }

                    _logger.LogInformation("Updating product in database...");
                    _context.Update(product);
                    var result = await _context.SaveChangesAsync();
                    
                    _logger.LogInformation("Product updated successfully: {ProductName} by Admin. SaveChanges result: {Result}", product.ProductName, result);
                    TempData["SuccessMessage"] = "Sản phẩm đã được cập nhật thành công!";
                    return RedirectToAction(nameof(Index));
                }
                else
                {
                    _logger.LogWarning("ModelState validation failed, returning to view");
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
                _logger.LogInformation("Reloaded {CategoryCount} categories for dropdown", categories.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading categories for dropdown");
                ViewBag.CategoryID = new SelectList(new List<object>(), "CategoryID", "CategoryName");
            }

            _logger.LogInformation("=== EDIT POST RETURNING TO VIEW ===");
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

        private async Task<string> SaveImageFileAsync(IFormFile file, string folder)
        {
            _logger.LogInformation("=== SAVE IMAGE ASYNC STARTED ===");
            _logger.LogInformation("File name: {FileName}, Size: {Size}, ContentType: {ContentType}", 
                file.FileName, file.Length, file.ContentType);
            
            var uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "assets", "images");
            _logger.LogInformation("Upload folder path: {UploadsFolder}", uploadsFolder);
            
            Directory.CreateDirectory(uploadsFolder);
            _logger.LogInformation("Directory created/ensured: {UploadsFolder}", uploadsFolder);

            var uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(file.FileName);
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);
            _logger.LogInformation("Unique filename: {UniqueFileName}", uniqueFileName);
            _logger.LogInformation("Full file path: {FilePath}", filePath);

            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(fileStream);
            }
            
            var imageUrl = $"/assets/images/{uniqueFileName}";
            _logger.LogInformation("Image saved successfully. Returning URL: {ImageUrl}", imageUrl);
            _logger.LogInformation("File exists after save: {FileExists}", System.IO.File.Exists(filePath));
            
            return imageUrl;
        }

        private void DeleteImageFile(string imageUrl)
        {
            if (string.IsNullOrEmpty(imageUrl)) return;

            try
            {
                // imageUrl có dạng "/assets/images/filename.jpg"
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
