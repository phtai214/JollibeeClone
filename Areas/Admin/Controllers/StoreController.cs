using JollibeeClone.Areas.Admin.Data;
using JollibeeClone.Areas.Admin.Models;
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
        public async Task<IActionResult> Index()
        {
            try
            {
                var stores = await _context.Stores
                    .OrderBy(s => s.StoreName)
                    .ToListAsync();
                return View(stores);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading stores");
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải danh sách cửa hàng.";
                return View(new List<Store>());
            }
        }

        // GET: Admin/Store/Details/5
        public async Task<IActionResult> Details(int? id)
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
                    TempData["ErrorMessage"] = "Không tìm thấy cửa hàng.";
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
                        TempData["SuccessMessage"] = "Cập nhật cửa hàng thành công!";
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
                    TempData["ErrorMessage"] = "Không tìm thấy cửa hàng.";
                    return RedirectToAction(nameof(Index));
                }

                return View(store);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading store for delete, ID: {StoreId}", id);
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải thông tin cửa hàng.";
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
                return "Kích thước ảnh không được vượt quá 5MB.";
            }

            // Check file type
            var allowedTypes = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
            var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!allowedTypes.Contains(fileExtension))
            {
                return "Chỉ chấp nhận các định dạng ảnh: JPG, JPEG, PNG, GIF, WEBP.";
            }

            return string.Empty;
        }

        private async Task<string> SaveImageFileAsync(IFormFile file, string folder)
        {
            try
            {
                var uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "assets", "images", folder);
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                var uniqueFileName = Guid.NewGuid().ToString() + "_" + file.FileName;
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(fileStream);
                }

                return $"/assets/images/{folder}/{uniqueFileName}";
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