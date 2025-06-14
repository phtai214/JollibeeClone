using JollibeeClone.Data;
using JollibeeClone.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using JollibeeClone.Areas.Admin.Attributes;

namespace JollibeeClone.Areas.Admin.Controllers
{
    [Area("Admin")]
    [AdminAuthorize]
    public class ServiceController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly ILogger<ServiceController> _logger;

        public ServiceController(AppDbContext context, IWebHostEnvironment webHostEnvironment, ILogger<ServiceController> logger)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
            _logger = logger;
        }

        // GET: Admin/Service
        [HttpGet]
        [Route("Admin/Service")]
        [Route("Admin/Service/Index")]
        public async Task<IActionResult> Index(string searchString, bool? isActive, string sortOrder, int? page)
        {
            try
            {
                // Pagination settings
                int pageSize = 10;
                int pageNumber = page ?? 1;

                var query = _context.Services.AsQueryable();

                // Apply filters
                if (!string.IsNullOrEmpty(searchString))
                {
                    query = query.Where(s => s.ServiceName.Contains(searchString) || s.ShortDescription.Contains(searchString));
                }

                if (isActive.HasValue)
                {
                    query = query.Where(s => s.IsActive == isActive.Value);
                }

                // Apply sorting
                switch (sortOrder)
                {
                    case "name_desc":
                        query = query.OrderByDescending(s => s.ServiceName);
                        break;
                    case "order":
                        query = query.OrderBy(s => s.DisplayOrder).ThenBy(s => s.ServiceName);
                        break;
                    case "order_desc":
                        query = query.OrderByDescending(s => s.DisplayOrder).ThenBy(s => s.ServiceName);
                        break;
                    default:
                        query = query.OrderBy(s => s.DisplayOrder).ThenBy(s => s.ServiceName);
                        break;
                }

                // Create paginated list
                var paginatedServices = await PaginatedList<Service>.CreateAsync(query, pageNumber, pageSize);

                // ViewBag for filters
                ViewBag.CurrentSearch = searchString;
                ViewBag.CurrentActive = isActive;
                ViewBag.CurrentSort = sortOrder;

                return View(paginatedServices);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading services");
                TempData["ErrorMessage"] = "CÃ³ lá»—i xáº£y ra khi táº£i danh sÃ¡ch dá»‹ch vá»¥.";
                return View(new PaginatedList<Service>(new List<Service>(), 0, 1, 10));
            }
        }

        // GET: Admin/Service/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                TempData["ErrorMessage"] = "ID dá»‹ch vá»¥ khÃ´ng há»£p lá»‡.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                var service = await _context.Services
                    .FirstOrDefaultAsync(m => m.ServiceID == id);

                if (service == null)
                {
                    TempData["ErrorMessage"] = "KhÃ´ng tÃ¬m tháº¥y dá»‹ch vá»¥.";
                    return RedirectToAction(nameof(Index));
                }

                return View(service);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading service details for ID: {ServiceId}", id);
                TempData["ErrorMessage"] = "CÃ³ lá»—i xáº£y ra khi táº£i thÃ´ng tin dá»‹ch vá»¥.";
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: Admin/Service/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Admin/Service/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ServiceName,ShortDescription,Content,DisplayOrder,IsActive")] Service service, IFormFile? ImageFile)
        {
            try
            {
                // Custom validation
                if (await _context.Services.AnyAsync(s => s.ServiceName.ToLower() == service.ServiceName.ToLower()))
                {
                    ModelState.AddModelError("ServiceName", "TÃªn dá»‹ch vá»¥ Ä‘Ã£ tá»“n táº¡i.");
                }

                if (ModelState.IsValid)
                {
                    // Handle image upload with validation
                    if (ImageFile != null && ImageFile.Length > 0)
                    {
                        var imageValidation = ValidateImageFile(ImageFile);
                        if (!string.IsNullOrEmpty(imageValidation))
                        {
                            ModelState.AddModelError("ImageFile", imageValidation);
                            return View(service);
                        }

                        service.ImageUrl = await SaveImageFileAsync(ImageFile, "services");
                    }

                    _context.Add(service);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation("Service created successfully: {ServiceName} by Admin", service.ServiceName);
                    TempData["SuccessMessage"] = "Dá»‹ch vá»¥ Ä‘Ã£ Ä‘Æ°á»£c táº¡o thÃ nh cÃ´ng!";
                    return RedirectToAction(nameof(Index));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating service: {ServiceName}", service.ServiceName);
                TempData["ErrorMessage"] = "CÃ³ lá»—i xáº£y ra khi táº¡o dá»‹ch vá»¥.";
            }

            return View(service);
        }

        // GET: Admin/Service/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                TempData["ErrorMessage"] = "ID dá»‹ch vá»¥ khÃ´ng há»£p lá»‡.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                var service = await _context.Services.FindAsync(id);
                if (service == null)
                {
                    TempData["ErrorMessage"] = "KhÃ´ng tÃ¬m tháº¥y dá»‹ch vá»¥.";
                    return RedirectToAction(nameof(Index));
                }

                return View(service);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading edit service page for ID: {ServiceId}", id);
                TempData["ErrorMessage"] = "CÃ³ lá»—i xáº£y ra khi táº£i trang chá»‰nh sá»­a.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Admin/Service/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("ServiceID,ServiceName,ShortDescription,Content,ImageUrl,DisplayOrder,IsActive")] Service service, IFormFile? ImageFile)
        {
            if (id != service.ServiceID)
            {
                TempData["ErrorMessage"] = "ID dá»‹ch vá»¥ khÃ´ng khá»›p.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                // Custom validation
                if (await _context.Services.AnyAsync(s => s.ServiceName.ToLower() == service.ServiceName.ToLower() && s.ServiceID != id))
                {
                    ModelState.AddModelError("ServiceName", "TÃªn dá»‹ch vá»¥ Ä‘Ã£ tá»“n táº¡i.");
                }

                if (ModelState.IsValid)
                {
                    var existingService = await _context.Services.AsNoTracking().FirstOrDefaultAsync(s => s.ServiceID == id);
                    if (existingService == null)
                    {
                        TempData["ErrorMessage"] = "KhÃ´ng tÃ¬m tháº¥y dá»‹ch vá»¥ Ä‘á»ƒ cáº­p nháº­t.";
                        return RedirectToAction(nameof(Index));
                    }

                    // Handle image upload
                    if (ImageFile != null && ImageFile.Length > 0)
                    {
                        var imageValidation = ValidateImageFile(ImageFile);
                        if (!string.IsNullOrEmpty(imageValidation))
                        {
                            ModelState.AddModelError("ImageFile", imageValidation);
                            return View(service);
                        }

                        // Delete old image if exists
                        if (!string.IsNullOrEmpty(existingService.ImageUrl))
                        {
                            DeleteImageFile(existingService.ImageUrl);
                        }

                        service.ImageUrl = await SaveImageFileAsync(ImageFile, "services");
                    }
                    else
                    {
                        // Keep existing image
                        service.ImageUrl = existingService.ImageUrl;
                    }

                    _context.Update(service);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation("Service updated successfully: {ServiceName} by Admin", service.ServiceName);
                    TempData["SuccessMessage"] = "Dá»‹ch vá»¥ Ä‘Ã£ Ä‘Æ°á»£c cáº­p nháº­t thÃ nh cÃ´ng!";
                    return RedirectToAction(nameof(Index));
                }
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogError(ex, "Concurrency error updating service: {ServiceId}", id);
                if (!await ServiceExistsAsync(service.ServiceID))
                {
                    TempData["ErrorMessage"] = "Dá»‹ch vá»¥ khÃ´ng tá»“n táº¡i.";
                    return RedirectToAction(nameof(Index));
                }
                else
                {
                    TempData["ErrorMessage"] = "Dá»‹ch vá»¥ Ä‘Ã£ bá»‹ thay Ä‘á»•i bá»Ÿi ngÆ°á»i dÃ¹ng khÃ¡c. Vui lÃ²ng thá»­ láº¡i.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating service: {ServiceId}", id);
                TempData["ErrorMessage"] = "Có
            }

            return View(service);
        }

        // GET: Admin/Service/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                TempData["ErrorMessage"] = "ID dịch vụ không hợp lệ.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                var service = await _context.Services
                    .FirstOrDefaultAsync(m => m.ServiceID == id);

                if (service == null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy dịch vụ.";
                    return RedirectToAction(nameof(Index));
                }

                return View(service);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading delete service page for ID: {ServiceId}", id);
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải trang xóa dịch vụ.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Admin/Service/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                var service = await _context.Services.FindAsync(id);
                if (service != null)
                {
                    // Delete image file if exists
                    if (!string.IsNullOrEmpty(service.ImageUrl))
                    {
                        DeleteImageFile(service.ImageUrl);
                    }

                    _context.Services.Remove(service);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation("Service deleted successfully: {ServiceName} by Admin", service.ServiceName);
                    TempData["SuccessMessage"] = "Dịch vụ đã được xóa thành công!";
                }
                else
                {
                    TempData["ErrorMessage"] = "Không tìm thấy dịch vụ để xóa.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting service: {ServiceId}", id);
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi xóa dịch vụ.";
            }

            return RedirectToAction(nameof(Index));
        }

        // Helper methods
        private async Task<bool> ServiceExistsAsync(int id)
        {
            return await _context.Services.AnyAsync(e => e.ServiceID == id);
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
            var uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", folder);
            Directory.CreateDirectory(uploadsFolder);
            var fileExtension = Path.GetExtension(file.FileName);
            var uniqueFileName = $"{Guid.NewGuid()}{fileExtension}";
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);
            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(fileStream);
            }
            return $"/uploads/{folder}/{uniqueFileName}";
        }

        private void DeleteImageFile(string imageUrl)
        {
            if (string.IsNullOrEmpty(imageUrl)) return;

            try
            {
                var imagePath = Path.Combine(_webHostEnvironment.WebRootPath, imageUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
                if (System.IO.File.Exists(imagePath))
                {
                    System.IO.File.Delete(imagePath);
                    _logger.LogInformation("Deleted image file: {ImagePath}", imagePath);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to delete image file: {ImageUrl}", imageUrl);
            }
        }
    }
} 


