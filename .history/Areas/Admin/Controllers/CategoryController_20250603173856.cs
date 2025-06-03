using JollibeeClone.Areas.Admin.Data;
using JollibeeClone.Areas.Admin.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using JollibeeClone.Areas.Admin.Attributes;

namespace JollibeeClone.Areas.Admin.Controllers
{
    [Area("Admin")]
    [AdminAuthorize]
    public class CategoryController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly ILogger<CategoryController> _logger;

        public CategoryController(AppDbContext context, IWebHostEnvironment webHostEnvironment, ILogger<CategoryController> logger)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
            _logger = logger;
        }

        // GET: Admin/Category
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            try
            {
                var categories = await _context.Categories
                    .Include(c => c.ParentCategory)
                    .Include(c => c.SubCategories)
                    .Include(c => c.Products)
                    .OrderBy(c => c.DisplayOrder)
                    .ThenBy(c => c.CategoryName)
                    .ToListAsync();
                return View(categories);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading categories");
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải danh sách danh mục.";
                return View(new List<Categories>());
            }
        }

        // GET: Admin/Category/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) 
            {
                TempData["ErrorMessage"] = "ID danh mục không hợp lệ.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                var category = await _context.Categories
                    .Include(c => c.ParentCategory)
                    .Include(c => c.SubCategories)
                    .Include(c => c.Products)
                    .FirstOrDefaultAsync(m => m.CategoryID == id);

                if (category == null) 
                {
                    TempData["ErrorMessage"] = "Không tìm thấy danh mục.";
                    return RedirectToAction(nameof(Index));
                }

                return View(category);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading category details for ID: {CategoryId}", id);
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải thông tin danh mục.";
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: Admin/Category/Create
        public async Task<IActionResult> Create()
        {
            try
            {
                ViewData["ParentCategoryID"] = new SelectList(await _context.Categories.Where(c => c.IsActive).ToListAsync(), "CategoryID", "CategoryName");
                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading create category page");
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải trang tạo danh mục.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Admin/Category/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("CategoryName,Description,DisplayOrder,ParentCategoryID,IsActive")] Categories category, IFormFile? imageFile)
        {
            try
            {
                // Custom validation
                if (await _context.Categories.AnyAsync(c => c.CategoryName.ToLower() == category.CategoryName.ToLower()))
                {
                    ModelState.AddModelError("CategoryName", "Tên danh mục đã tồn tại.");
                }

                // Validate parent category
                if (category.ParentCategoryID.HasValue)
                {
                    var parentExists = await _context.Categories.AnyAsync(c => c.CategoryID == category.ParentCategoryID && c.IsActive);
                    if (!parentExists)
                    {
                        ModelState.AddModelError("ParentCategoryID", "Danh mục cha không hợp lệ.");
                    }
                }

                if (ModelState.IsValid)
                {
                    // Handle image upload with validation
                    if (imageFile != null && imageFile.Length > 0)
                    {
                        var imageValidation = ValidateImageFile(imageFile);
                        if (!string.IsNullOrEmpty(imageValidation))
                        {
                            ModelState.AddModelError("ImageFile", imageValidation);
                            ViewData["ParentCategoryID"] = new SelectList(await _context.Categories.Where(c => c.IsActive).ToListAsync(), "CategoryID", "CategoryName", category.ParentCategoryID);
                            return View(category);
                        }

                        category.ImageUrl = await SaveImageFileAsync(imageFile, "categories");
                    }

                    _context.Add(category);
                    await _context.SaveChangesAsync();
                    
                    _logger.LogInformation("Category created successfully: {CategoryName} by Admin", category.CategoryName);
                    TempData["SuccessMessage"] = "Danh mục đã được tạo thành công!";
                    return RedirectToAction(nameof(Index));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating category: {CategoryName}", category.CategoryName);
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tạo danh mục.";
            }

            ViewData["ParentCategoryID"] = new SelectList(await _context.Categories.Where(c => c.IsActive).ToListAsync(), "CategoryID", "CategoryName", category.ParentCategoryID);
            return View(category);
        }

        // GET: Admin/Category/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) 
            {
                TempData["ErrorMessage"] = "ID danh mục không hợp lệ.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                var category = await _context.Categories
                    .Include(c => c.SubCategories)
                    .Include(c => c.Products)
                    .FirstOrDefaultAsync(c => c.CategoryID == id);
                
                if (category == null) 
                {
                    TempData["ErrorMessage"] = "Không tìm thấy danh mục.";
                    return RedirectToAction(nameof(Index));
                }

                ViewData["ParentCategoryID"] = new SelectList(await _context.Categories.Where(c => c.IsActive && c.CategoryID != id).ToListAsync(), "CategoryID", "CategoryName", category.ParentCategoryID);
                return View(category);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading edit category page for ID: {CategoryId}", id);
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải trang chỉnh sửa.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Admin/Category/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("CategoryID,CategoryName,Description,ImageUrl,DisplayOrder,ParentCategoryID,IsActive")] Categories category, IFormFile? imageFile)
        {
            if (id != category.CategoryID) 
            {
                TempData["ErrorMessage"] = "ID danh mục không khớp.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                // Custom validation
                if (await _context.Categories.AnyAsync(c => c.CategoryName.ToLower() == category.CategoryName.ToLower() && c.CategoryID != id))
                {
                    ModelState.AddModelError("CategoryName", "Tên danh mục đã tồn tại.");
                }

                // Validate parent category (prevent circular reference)
                if (category.ParentCategoryID.HasValue)
                {
                    if (category.ParentCategoryID == id)
                    {
                        ModelState.AddModelError("ParentCategoryID", "Danh mục không thể là cha của chính nó.");
                    }
                    else
                    {
                        var hasCircularRef = await HasCircularReference(id, category.ParentCategoryID.Value);
                        if (hasCircularRef)
                        {
                            ModelState.AddModelError("ParentCategoryID", "Không thể tạo vòng lặp trong cấu trúc danh mục.");
                        }
                    }
                }

                if (ModelState.IsValid)
                {
                    var existingCategory = await _context.Categories.AsNoTracking().FirstOrDefaultAsync(c => c.CategoryID == id);
                    
                    // Handle image upload
                    if (imageFile != null && imageFile.Length > 0)
                    {
                        var imageValidation = ValidateImageFile(imageFile);
                        if (!string.IsNullOrEmpty(imageValidation))
                        {
                            ModelState.AddModelError("ImageFile", imageValidation);
                            ViewData["ParentCategoryID"] = new SelectList(await _context.Categories.Where(c => c.IsActive && c.CategoryID != id).ToListAsync(), "CategoryID", "CategoryName", category.ParentCategoryID);
                            return View(category);
                        }

                        // Delete old image
                        if (!string.IsNullOrEmpty(existingCategory?.ImageUrl))
                        {
                            DeleteImageFile(existingCategory.ImageUrl);
                        }

                        category.ImageUrl = await SaveImageFileAsync(imageFile, "categories");
                    }
                    else
                    {
                        // Keep existing image
                        category.ImageUrl = existingCategory?.ImageUrl;
                    }

                    _context.Update(category);
                    await _context.SaveChangesAsync();
                    
                    _logger.LogInformation("Category updated successfully: {CategoryName} by Admin", category.CategoryName);
                    TempData["SuccessMessage"] = "Danh mục đã được cập nhật thành công!";
                    return RedirectToAction(nameof(Index));
                }
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await CategoryExistsAsync(category.CategoryID))
                {
                    TempData["ErrorMessage"] = "Danh mục không tồn tại.";
                    return RedirectToAction(nameof(Index));
                }
                else
                {
                    TempData["ErrorMessage"] = "Danh mục đã bị thay đổi bởi người dùng khác. Vui lòng thử lại.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating category: {CategoryId}", id);
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi cập nhật danh mục.";
            }

            ViewData["ParentCategoryID"] = new SelectList(await _context.Categories.Where(c => c.IsActive && c.CategoryID != id).ToListAsync(), "CategoryID", "CategoryName", category.ParentCategoryID);
            return View(category);
        }

        // GET: Admin/Category/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) 
            {
                TempData["ErrorMessage"] = "ID danh mục không hợp lệ.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                var category = await _context.Categories
                    .Include(c => c.ParentCategory)
                    .Include(c => c.SubCategories)
                    .Include(c => c.Products)
                    .FirstOrDefaultAsync(m => m.CategoryID == id);

                if (category == null) 
                {
                    TempData["ErrorMessage"] = "Không tìm thấy danh mục.";
                    return RedirectToAction(nameof(Index));
                }

                return View(category);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading delete category page for ID: {CategoryId}", id);
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải trang xóa danh mục.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Admin/Category/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                var category = await _context.Categories
                    .Include(c => c.SubCategories)
                    .Include(c => c.Products)
                    .FirstOrDefaultAsync(c => c.CategoryID == id);

                if (category != null)
                {
                    // Check if category has subcategories or products
                    if (category.SubCategories.Any())
                    {
                        TempData["ErrorMessage"] = $"Không thể xóa danh mục vì có {category.SubCategories.Count} danh mục con liên quan!";
                        return RedirectToAction(nameof(Index));
                    }

                    if (category.Products.Any())
                    {
                        TempData["ErrorMessage"] = $"Không thể xóa danh mục vì có {category.Products.Count} sản phẩm liên quan!";
                        return RedirectToAction(nameof(Index));
                    }

                    // Delete image file if exists
                    if (!string.IsNullOrEmpty(category.ImageUrl))
                    {
                        DeleteImageFile(category.ImageUrl);
                    }

                    _context.Categories.Remove(category);
                    await _context.SaveChangesAsync();
                    
                    _logger.LogInformation("Category deleted successfully: {CategoryName} by Admin", category.CategoryName);
                    TempData["SuccessMessage"] = "Danh mục đã được xóa thành công!";
                }
                else
                {
                    TempData["ErrorMessage"] = "Không tìm thấy danh mục để xóa.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting category: {CategoryId}", id);
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi xóa danh mục.";
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: Admin/Category/SeedCategories - Action để tạo categories mẫu
        [HttpGet]
        public async Task<IActionResult> SeedCategories()
        {
            try
            {
                // Kiểm tra xem đã có categories chưa
                var existingCategories = await _context.Categories.AnyAsync();
                if (existingCategories)
                {
                    return Json(new { success = false, message = "Categories đã tồn tại trong database!" });
                }

                // Tạo categories mẫu
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

                _logger.LogInformation("Sample categories created successfully by Admin");
                return Json(new { success = true, message = $"Đã tạo thành công {categories.Count} categories mẫu!" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating sample categories");
                return Json(new { success = false, message = "Có lỗi xảy ra: " + ex.Message });
            }
        }

        // Helper methods
        private async Task<bool> CategoryExistsAsync(int id)
        {
            return await _context.Categories.AnyAsync(e => e.CategoryID == id);
        }

        private async Task<bool> HasCircularReference(int categoryId, int parentId)
        {
            var parent = await _context.Categories.FindAsync(parentId);
            while (parent != null)
            {
                if (parent.CategoryID == categoryId)
                    return true;
                
                parent = parent.ParentCategoryID.HasValue 
                    ? await _context.Categories.FindAsync(parent.ParentCategoryID.Value) 
                    : null;
            }
            return false;
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

