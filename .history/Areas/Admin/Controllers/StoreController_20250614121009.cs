using JollibeeClone.Data;
using JollibeeClone.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using JollibeeClone.Areas.Admin.Attributes;

namespace JollibeeClone.Areas.Admin.Controllers
{
    [Area("Admin")]
    [AdminAuthorize]
    public class StoreController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly ILogger<StoreController> _logger;

        public StoreController(AppDbContext context, IWebHostEnvironment webHostEnvironment, ILogger<StoreController> logger)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
            _logger = logger;
        }

        // GET: Admin/Store
        [HttpGet]
        [Route("Admin/Store")]
        [Route("Admin/Store/Index")]
        public async Task<IActionResult> Index(string searchString, string city, bool? isActive, string sortOrder, int? page)
        {
            try
            {
                // Pagination settings
                int pageSize = 9; // 9 stores per page
                int pageNumber = page ?? 1;

                var query = _context.Stores.AsQueryable();

                // Apply filters
                if (!string.IsNullOrEmpty(searchString))
                {
                    query = query.Where(s => s.StoreName.Contains(searchString) || 
                                           s.StreetAddress.Contains(searchString) ||
                                           s.District.Contains(searchString));
                }

                if (!string.IsNullOrEmpty(city))
                {
                    query = query.Where(s => s.City == city);
                }

                if (isActive.HasValue)
                {
                    query = query.Where(s => s.IsActive == isActive.Value);
                }

                // Apply sorting
                switch (sortOrder)
                {
                    case "name_desc":
                        query = query.OrderByDescending(s => s.StoreName);
                        break;
                    case "city":
                        query = query.OrderBy(s => s.City).ThenBy(s => s.StoreName);
                        break;
                    case "city_desc":
                        query = query.OrderByDescending(s => s.City).ThenBy(s => s.StoreName);
                        break;
                    default:
                        query = query.OrderBy(s => s.City).ThenBy(s => s.StoreName);
                        break;
                }

                // Create paginated list
                var paginatedStores = await PaginatedList<Store>.CreateAsync(query, pageNumber, pageSize);

                // ViewBag for filters and pagination
                var allCities = await _context.Stores.Select(s => s.City).Distinct().OrderBy(c => c).ToListAsync();
                ViewBag.Cities = allCities;

                ViewBag.CurrentSearch = searchString;
                ViewBag.CurrentCity = city;
                ViewBag.CurrentActive = isActive;
                ViewBag.CurrentSort = sortOrder;

                return View(paginatedStores);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading stores");
                TempData["ErrorMessage"] = "CÃ³ lá»—i xáº£y ra khi táº£i danh sÃ¡ch cá»­a hÃ ng.";
                return View(new PaginatedList<Store>(new List<Store>(), 0, 1, 9));
            }
        }

        // GET: Admin/Store/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                TempData["ErrorMessage"] = "ID cá»­a hÃ ng khÃ´ng há»£p lá»‡.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                var store = await _context.Stores
                    .FirstOrDefaultAsync(m => m.StoreID == id);

                if (store == null)
                {
                    TempData["ErrorMessage"] = "KhÃ´ng tÃ¬m tháº¥y cá»­a hÃ ng.";
                    return RedirectToAction(nameof(Index));
                }

                return View(store);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading store details for ID: {StoreId}", id);
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải thông tin cửa hàng.";
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: Admin/Store/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Admin/Store/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("StoreName,StreetAddress,Ward,District,City,PhoneNumber,OpeningHours,ImageUrl,IsActive")] Store store, IFormFile? imageFile)
        {
            try
            {
                // Basic validation
                bool isValid = true;
                List<string> errors = new List<string>();

                if (string.IsNullOrWhiteSpace(store.StoreName))
                {
                    errors.Add("Tên cửa hàng là bắt buộc");
                    isValid = false;
                }

                if (string.IsNullOrWhiteSpace(store.StreetAddress))
                {
                    errors.Add("Địa chỉ là bắt buộc");
                    isValid = false;
                }

                if (string.IsNullOrWhiteSpace(store.District))
                {
                    errors.Add("Quận/Huyện là bắt buộc");
                    isValid = false;
                }

                if (string.IsNullOrWhiteSpace(store.City))
                {
                    errors.Add("Thành phố là bắt buộc");
                    isValid = false;
                }

                // Check for duplicate store name
                var existingStore = await _context.Stores
                    .FirstOrDefaultAsync(s => s.StoreName.ToLower() == store.StoreName.ToLower());
                if (existingStore != null)
                {
                    errors.Add("Tên cửa hàng đã tồn tại");
                    isValid = false;
                }

                if (isValid)
                {
                    // Process image upload
                    if (imageFile != null && imageFile.Length > 0)
                    {
                        var imageValidation = ValidateImageFile(imageFile);
                        if (!string.IsNullOrEmpty(imageValidation))
                        {
                            errors.Add(imageValidation);
                            isValid = false;
                        }
                        else
                        {
                            store.ImageUrl = await SaveImageFileAsync(imageFile, "stores");
                        }
                    }

                    if (isValid)
                    {
                        _context.Add(store);
                        await _context.SaveChangesAsync();
                        TempData["SuccessMessage"] = "Thêm cửa hàng thành công!";
                        return RedirectToAction(nameof(Index));
                    }
                }

                // If we get here, there are errors
                foreach (var error in errors)
                {
                    ModelState.AddModelError("", error);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating store");
                ModelState.AddModelError("", "Có lỗi xảy ra khi tạo cửa hàng.");
            }

            return View(store);
        }

        // GET: Admin/Store/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                TempData["ErrorMessage"] = "ID cửa hàng không hợp lệ.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                var store = await _context.Stores.FindAsync(id);
                if (store == null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy cửa hàng.";
                    return RedirectToAction(nameof(Index));
                }
                return View(store);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading store for edit, ID: {StoreId}", id);
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải thông tin cửa hàng.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Admin/Store/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("StoreID,StoreName,StreetAddress,Ward,District,City,PhoneNumber,OpeningHours,ImageUrl,IsActive")] Store store, IFormFile? imageFile)
        {
            if (id != store.StoreID)
            {
                TempData["ErrorMessage"] = "ID cửa hàng không hợp lệ.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                // Basic validation
                bool isValid = true;
                List<string> errors = new List<string>();

                if (string.IsNullOrWhiteSpace(store.StoreName))
                {
                    errors.Add("Tên cửa hàng là bắt buộc");
                    isValid = false;
                }

                if (string.IsNullOrWhiteSpace(store.StreetAddress))
                {
                    errors.Add("Địa chỉ là bắt buộc");
                    isValid = false;
                }

                if (string.IsNullOrWhiteSpace(store.District))
                {
                    errors.Add("Quận/Huyện là bắt buộc");
                    isValid = false;
                }

                if (string.IsNullOrWhiteSpace(store.City))
                {
                    errors.Add("Thành phố là bắt buộc");
                    isValid = false;
                }

                // Check for duplicate store name (excluding current store)
                var existingStore = await _context.Stores
                    .FirstOrDefaultAsync(s => s.StoreName.ToLower() == store.StoreName.ToLower() && s.StoreID != store.StoreID);
                if (existingStore != null)
                {
                    errors.Add("Tên cửa hàng đã tồn tại");
                    isValid = false;
                }

                if (isValid)
                {
                    // Get original store for image comparison
                    var originalStore = await _context.Stores.AsNoTracking().FirstOrDefaultAsync(s => s.StoreID == id);

                    // Process image upload
                    if (imageFile != null && imageFile.Length > 0)
                    {
                        var imageValidation = ValidateImageFile(imageFile);
                        if (!string.IsNullOrEmpty(imageValidation))
                        {
                            errors.Add(imageValidation);
                            isValid = false;
                        }
                        else
                        {
                            // Delete old image if exists
                            if (!string.IsNullOrEmpty(originalStore?.ImageUrl))
                            {
                                DeleteImageFile(originalStore.ImageUrl);
                            }
                            store.ImageUrl = await SaveImageFileAsync(imageFile, "stores");
                        }
                    }

                    if (isValid)
                    {
                        _context.Update(store);
                        await _context.SaveChangesAsync();
                        TempData["SuccessMessage"] = "Cáº­p nháº­t cá»­a hÃ ng thÃ nh cÃ´ng!";
                        return RedirectToAction(nameof(Index));
                    }
                }

                // If we get here, there are errors
                foreach (var error in errors)
                {
                    ModelState.AddModelError("", error);
                }
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await StoreExistsAsync(store.StoreID))
                {
                    TempData["ErrorMessage"] = "Cửa hàng không tồn tại.";
                    return RedirectToAction(nameof(Index));
                }
                else
                {
                    ModelState.AddModelError("", "Có xung đột dữ liệu. Vui lòng thử lại.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating store");
                ModelState.AddModelError("", "Có lỗi xảy ra khi cập nhật cửa hàng.");
            }

            return View(store);
        }

        // GET: Admin/Store/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                TempData["ErrorMessage"] = "ID cửa hàng không hợp lệ.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                var store = await _context.Stores
                    .FirstOrDefaultAsync(m => m.StoreID == id);
                if (store == null)
                {
                    TempData["ErrorMessage"] = "KhÃ´ng tÃ¬m tháº¥y cá»­a hÃ ng.";
                    return RedirectToAction(nameof(Index));
                }

                return View(store);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading store for delete, ID: {StoreId}", id);
                TempData["ErrorMessage"] = "CÃ³ lá»—i xáº£y ra khi táº£i thÃ´ng tin cá»­a hÃ ng.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Admin/Store/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                var store = await _context.Stores.FindAsync(id);
                if (store != null)
                {
                    // Check if store has orders
                    var hasOrders = await _context.Orders.AnyAsync(o => o.StoreID == id);
                    if (hasOrders)
                    {
                        TempData["ErrorMessage"] = "Không thể xóa cửa hàng này vì có đơn hàng liên quan.";
                        return RedirectToAction(nameof(Index));
                    }

                    // Delete image file if exists
                    if (!string.IsNullOrEmpty(store.ImageUrl))
                    {
                        DeleteImageFile(store.ImageUrl);
                    }

                    _context.Stores.Remove(store);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Xóa cửa hàng thành công!";
                }
                else
                {
                    TempData["ErrorMessage"] = "Không tìm thấy cửa hàng.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting store with ID: {StoreId}", id);
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi xóa cửa hàng.";
            }

            return RedirectToAction(nameof(Index));
        }

        private async Task<bool> StoreExistsAsync(int id)
        {
            return await _context.Stores.AnyAsync(e => e.StoreID == id);
        }

        private string ValidateImageFile(IFormFile file)
        {
            // Check file size (5MB limit)
            if (file.Length > 5 * 1024 * 1024)
            {
                return "KÃ­ch thÆ°á»›c áº£nh khÃ´ng Ä‘Æ°á»£c vÆ°á»£t quÃ¡ 5MB.";
            }

            // Check file type
            var allowedTypes = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
            var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!allowedTypes.Contains(fileExtension))
            {
                return "Chá»‰ cháº¥p nháº­n cÃ¡c Ä‘á»‹nh dáº¡ng áº£nh: JPG, JPEG, PNG, GIF, WEBP.";
            }

            return string.Empty;
        }

        private async Task<string> SaveImageFileAsync(IFormFile file, string folder)
        {
            try
            {
                var uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", folder);
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                var fileExtension = Path.GetExtension(file.FileName);
                var uniqueFileName = $"{Guid.NewGuid()}{fileExtension}";
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(fileStream);
                }

                return $"/uploads/{folder}/{uniqueFileName}";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving image file");
                throw;
            }
        }

        private void DeleteImageFile(string imageUrl)
        {
            try
            {
                if (!string.IsNullOrEmpty(imageUrl))
                {
                    var filePath = Path.Combine(_webHostEnvironment.WebRootPath, imageUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
                    if (System.IO.File.Exists(filePath))
                    {
                        System.IO.File.Delete(filePath);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting image file: {ImageUrl}", imageUrl);
            }
        }
    }
} 


