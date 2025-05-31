using JollibeeClone.Areas.Admin.Data;
using JollibeeClone.Areas.Admin.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace JollibeeClone.Areas.Admin.Controllers
{
    [Area("Admin")]
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
                ViewData["CategoryID"] = new SelectList(await _context.Categories.Where(c => c.IsActive).ToListAsync(), "CategoryID", "CategoryName");
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
            _logger.LogInformation("Create POST called with product: {ProductName}, CategoryID: {CategoryID}, Price: {Price}", 
                product.ProductName, product.CategoryID, product.Price);

            try
            {
                // Log ModelState for debugging
                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("ModelState is invalid:");
                    foreach (var key in ModelState.Keys)
                    {
                        var state = ModelState[key];
                        if (state.Errors.Count > 0)
                        {
                            foreach (var error in state.Errors)
                            {
                                _logger.LogWarning("ModelState Error - Key: {Key}, Error: {Error}", key, error.ErrorMessage);
                            }
                        }
                    }
                }

                // Custom validation
                if (await _context.Products.AnyAsync(p => p.ProductName.ToLower() == product.ProductName.ToLower()))
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
                    _logger.LogWarning("Original price {OriginalPrice} is not greater than price {Price}", product.OriginalPrice, product.Price);
                }

                // Validate category - CategoryID is required
                if (product.CategoryID <= 0)
                {
                    ModelState.AddModelError("CategoryID", "Vui lòng chọn danh mục cho sản phẩm.");
                    _logger.LogWarning("CategoryID is required but not selected: {CategoryID}", product.CategoryID);
                }
                else
                {
                    var categoryExists = await _context.Categories.AnyAsync(c => c.CategoryID == product.CategoryID && c.IsActive);
                    if (!categoryExists)
                    {
                        ModelState.AddModelError("CategoryID", "Danh mục được chọn không hợp lệ.");
                        _logger.LogWarning("Category does not exist or is inactive: {CategoryID}", product.CategoryID);
                    }
                }

                _logger.LogInformation("Validation completed. ModelState.IsValid: {IsValid}", ModelState.IsValid);

                if (ModelState.IsValid)
                {
                    _logger.LogInformation("Starting product creation process...");

                    // Handle image upload with validation
                    if (imageFile != null && imageFile.Length > 0)
                    {
                        _logger.LogInformation("Processing image upload: {FileName}, Size: {Size}", imageFile.FileName, imageFile.Length);
                        
                        var imageValidation = ValidateImageFile(imageFile);
                        if (!string.IsNullOrEmpty(imageValidation))
                        {
                            ModelState.AddModelError("ImageFile", imageValidation);
                            _logger.LogWarning("Image validation failed: {ValidationError}", imageValidation);
                            ViewData["CategoryID"] = new SelectList(await _context.Categories.Where(c => c.IsActive).ToListAsync(), "CategoryID", "CategoryName", product.CategoryID);
                            return View(product);
                        }

                        product.ImageUrl = await SaveImageFileAsync(imageFile, "products");
                        _logger.LogInformation("Image uploaded successfully: {ImageUrl}", product.ImageUrl);
                    }

                    _context.Add(product);
                    var result = await _context.SaveChangesAsync();
                    
                    _logger.LogInformation("Product created successfully: {ProductName} by Admin. SaveChanges result: {Result}", product.ProductName, result);
                    TempData["SuccessMessage"] = "Sản phẩm đã được tạo thành công!";
                    return RedirectToAction(nameof(Index));
                }
                else
                {
                    _logger.LogWarning("ModelState validation failed, returning to Create view");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating product: {ProductName}", product.ProductName);
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tạo sản phẩm.";
            }

            // Reload categories for dropdown
            try
            {
                ViewData["CategoryID"] = new SelectList(await _context.Categories.Where(c => c.IsActive).ToListAsync(), "CategoryID", "CategoryName", product.CategoryID);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading categories for dropdown");
                ViewData["CategoryID"] = new SelectList(new List<object>(), "CategoryID", "CategoryName");
            }

            _logger.LogInformation("Returning Create view with errors");
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

                ViewData["CategoryID"] = new SelectList(await _context.Categories.Where(c => c.IsActive).ToListAsync(), "CategoryID", "CategoryName", product.CategoryID);
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
            if (id != product.ProductID) 
            {
                TempData["ErrorMessage"] = "ID sản phẩm không khớp.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                // Custom validation
                if (await _context.Products.AnyAsync(p => p.ProductName.ToLower() == product.ProductName.ToLower() && p.ProductID != id))
                {
                    ModelState.AddModelError("ProductName", "Tên sản phẩm đã tồn tại.");
                }

                // Validate price
                if (product.Price <= 0)
                {
                    ModelState.AddModelError("Price", "Giá sản phẩm phải lớn hơn 0.");
                }

                if (product.OriginalPrice.HasValue && product.OriginalPrice <= product.Price)
                {
                    ModelState.AddModelError("OriginalPrice", "Giá gốc phải lớn hơn giá bán hiện tại.");
                }

                // Validate category
                if (product.CategoryID > 0)
                {
                    var categoryExists = await _context.Categories.AnyAsync(c => c.CategoryID == product.CategoryID && c.IsActive);
                    if (!categoryExists)
                    {
                        ModelState.AddModelError("CategoryID", "Danh mục được chọn không hợp lệ.");
                    }
                }

                if (ModelState.IsValid)
                {
                    var existingProduct = await _context.Products.AsNoTracking().FirstOrDefaultAsync(p => p.ProductID == id);
                    
                    // Handle image upload
                    if (imageFile != null && imageFile.Length > 0)
                    {
                        var imageValidation = ValidateImageFile(imageFile);
                        if (!string.IsNullOrEmpty(imageValidation))
                        {
                            ModelState.AddModelError("ImageFile", imageValidation);
                            ViewData["CategoryID"] = new SelectList(await _context.Categories.Where(c => c.IsActive).ToListAsync(), "CategoryID", "CategoryName", product.CategoryID);
                            return View(product);
                        }

                        // Delete old image
                        if (!string.IsNullOrEmpty(existingProduct?.ImageUrl))
                        {
                            DeleteImageFile(existingProduct.ImageUrl);
                        }

                        product.ImageUrl = await SaveImageFileAsync(imageFile, "products");
                    }
                    else
                    {
                        // Keep existing image
                        product.ImageUrl = existingProduct?.ImageUrl;
                    }

                    _context.Update(product);
                    await _context.SaveChangesAsync();
                    
                    _logger.LogInformation("Product updated successfully: {ProductName} by Admin", product.ProductName);
                    TempData["SuccessMessage"] = "Sản phẩm đã được cập nhật thành công!";
                    return RedirectToAction(nameof(Index));
                }
            }
            catch (DbUpdateConcurrencyException)
            {
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
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi cập nhật sản phẩm.";
            }

            ViewData["CategoryID"] = new SelectList(await _context.Categories.Where(c => c.IsActive).ToListAsync(), "CategoryID", "CategoryName", product.CategoryID);
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
            var uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images", folder);
            Directory.CreateDirectory(uploadsFolder);

            var uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(file.FileName);
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(fileStream);
            }

            return $"/images/{folder}/{uniqueFileName}";
        }

        private void DeleteImageFile(string imageUrl)
        {
            if (string.IsNullOrEmpty(imageUrl)) return;

            try
            {
                var imagePath = Path.Combine(_webHostEnvironment.WebRootPath, imageUrl.TrimStart('/'));
                if (System.IO.File.Exists(imagePath))
                {
                    System.IO.File.Delete(imagePath);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to delete image file: {ImageUrl}", imageUrl);
            }
        }
    }
}
