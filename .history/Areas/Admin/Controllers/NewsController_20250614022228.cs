using JollibeeClone.Data;
using JollibeeClone.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using JollibeeClone.Areas.Admin.Attributes;

namespace JollibeeClone.Areas.Admin.Controllers
{
    [Area("Admin")]
    [AdminAuthorize]
    public class NewsController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly ILogger<NewsController> _logger;

        public NewsController(AppDbContext context, IWebHostEnvironment webHostEnvironment, ILogger<NewsController> logger)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
            _logger = logger;
        }

        // GET: Admin/News
        [HttpGet]
        [Route("Admin/News")]
        [Route("Admin/News/Index")]
        public async Task<IActionResult> Index(string searchString, string newsType, bool? isPublished, string sortOrder, int? page)
        {
            try
            {
                // Pagination settings
                int pageSize = 10;
                int pageNumber = page ?? 1;

                var query = _context.News.Include(n => n.Author).AsQueryable();

                // Apply filters
                if (!string.IsNullOrEmpty(searchString))
                {
                    query = query.Where(n => n.Title.Contains(searchString) || n.ShortDescription.Contains(searchString));
                }

                if (!string.IsNullOrEmpty(newsType))
                {
                    query = query.Where(n => n.NewsType == newsType);
                }

                if (isPublished.HasValue)
                {
                    query = query.Where(n => n.IsPublished == isPublished.Value);
                }

                // Apply sorting
                switch (sortOrder)
                {
                    case "title_desc":
                        query = query.OrderByDescending(n => n.Title);
                        break;
                    case "date":
                        query = query.OrderBy(n => n.PublishedDate);
                        break;
                    case "date_desc":
                        query = query.OrderByDescending(n => n.PublishedDate);
                        break;
                    case "type":
                        query = query.OrderBy(n => n.NewsType).ThenByDescending(n => n.PublishedDate);
                        break;
                    default:
                        query = query.OrderByDescending(n => n.PublishedDate);
                        break;
                }

                // Create paginated list
                var paginatedNews = await PaginatedList<News>.CreateAsync(query, pageNumber, pageSize);

                // ViewBag for filters
                ViewBag.CurrentSearch = searchString;
                ViewBag.CurrentNewsType = newsType;
                ViewBag.CurrentPublished = isPublished;
                ViewBag.CurrentSort = sortOrder;

                ViewBag.NewsTypes = new SelectList(new[]
                {
                    new { Value = "", Text = "Táº¥t cáº£ loáº¡i" },
                    new { Value = "News", Text = "Tin tá»©c" },
                    new { Value = "Promotion", Text = "Khuyáº¿n mÃ£i" }
                }, "Value", "Text", newsType);

                return View(paginatedNews);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading news");
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải danh sách tin tức.";
                return View(new PaginatedList<News>(new List<News>(), 0, 1, 10));
            }
        }

        // GET: Admin/News/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                TempData["ErrorMessage"] = "ID tin tức không hợp lệ.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                var news = await _context.News
                    .Include(n => n.Author)
                    .FirstOrDefaultAsync(m => m.NewsID == id);

                if (news == null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy tin tức.";
                    return RedirectToAction(nameof(Index));
                }

                return View(news);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading news details for ID: {NewsId}", id);
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải thông tin tin tức.";
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: Admin/News/Create
        public async Task<IActionResult> Create()
        {
            try
            {
                await LoadViewBagDataAsync();
                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading create news page");
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải trang tạo tin tức.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Admin/News/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Title,ShortDescription,Content,NewsType,AuthorID,IsPublished")] News news, IFormFile? ImageFile)
        {
            try
            {
                _logger.LogInformation("Starting to create news with title: {Title}", news?.Title);
                
                // Custom validation - Đơn giản hóa để debug
                if (!string.IsNullOrEmpty(news.Title) && await _context.News.AnyAsync(n => n.Title.ToLower() == news.Title.ToLower()))
                {
                    ModelState.AddModelError("Title", "Tiêu đề tin tức đã tồn tại.");
                }

                // Debug: Log ModelState errors
                if (!ModelState.IsValid)
                {
                    foreach (var modelError in ModelState)
                    {
                        if (modelError.Value.Errors.Count > 0)
                        {
                            _logger.LogWarning("ModelState Error for {Key}: {Errors}", 
                                modelError.Key, 
                                string.Join("; ", modelError.Value.Errors.Select(e => e.ErrorMessage)));
                        }
                    }
                    await LoadViewBagDataAsync();
                    return View(news);
                }

                if (ModelState.IsValid)
                {
                    _logger.LogInformation("ModelState is valid, proceeding with image upload");
                    
                    // Handle image upload with validation
                    if (ImageFile != null && ImageFile.Length > 0)
                    {
                        _logger.LogInformation("Processing image file: {FileName}, Size: {Size}", ImageFile.FileName, ImageFile.Length);
                        
                        var imageValidation = ValidateImageFile(ImageFile);
                        if (!string.IsNullOrEmpty(imageValidation))
                        {
                            ModelState.AddModelError("ImageFile", imageValidation);
                            await LoadViewBagDataAsync();
                            return View(news);
                        }

                        news.ImageUrl = await SaveImageFileAsync(ImageFile, "news");
                        _logger.LogInformation("Image saved to: {ImageUrl}", news.ImageUrl);
                    }

                    // Ensure all required fields have safe values
                    if (string.IsNullOrWhiteSpace(news.Title))
                    {
                        news.Title = "Tin tức mới";
                    }
                    
                    if (string.IsNullOrWhiteSpace(news.Content))
                    {
                        news.Content = "Nội dung tin tức";
                    }
                    
                    if (string.IsNullOrWhiteSpace(news.NewsType))
                    {
                        news.NewsType = "News";
                    }

                    // Set default values
                    news.PublishedDate = DateTime.Now;
                    
                    // Ensure AuthorID is properly handled (can be null)
                    if (news.AuthorID == 0)
                    {
                        news.AuthorID = null;
                    }
                    
                    _logger.LogInformation("Adding news to context: Title={Title}, AuthorID={AuthorID}, NewsType={NewsType}, Content Length={ContentLength}", 
                        news.Title, news.AuthorID, news.NewsType, news.Content?.Length ?? 0);
                    
                    // Test database connection
                    _logger.LogInformation("Testing database connection...");
                    var canConnect = await _context.Database.CanConnectAsync();
                    _logger.LogInformation("Database connection test result: {CanConnect}", canConnect);
                    
                    if (!canConnect)
                    {
                        throw new Exception("Cannot connect to database");
                    }
                    
                    _context.Add(news);
                    
                    _logger.LogInformation("Attempting to save changes to database");
                    var saveResult = await _context.SaveChangesAsync();
                    _logger.LogInformation("SaveChanges result: {SaveResult} entities saved", saveResult);

                    _logger.LogInformation("News created successfully: {NewsTitle} by Admin", news.Title);
                    TempData["SuccessMessage"] = "Tin tức đã được tạo thành công!";
                    return RedirectToAction(nameof(Index));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating news: {NewsTitle}. Inner Exception: {InnerException}", 
                    news?.Title ?? "Unknown", 
                    ex.InnerException?.Message ?? "None");
                
                // Log the full exception details
                var fullError = ex.ToString();
                _logger.LogError("Full exception details: {FullError}", fullError);
                
                TempData["ErrorMessage"] = $"Có lỗi xảy ra khi tạo tin tức: {ex.Message}";
                
                if (ex.InnerException != null)
                {
                    TempData["ErrorMessage"] += $ " Chi tiết: {ex.InnerException.Message}";
                }
            }

            await LoadViewBagDataAsync();
            return View(news);
        }

        // GET: Admin/News/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                TempData["ErrorMessage"] = "ID tin tức không hợp lệ.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                var news = await _context.News.FindAsync(id);
                if (news == null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy tin tức.";
                    return RedirectToAction(nameof(Index));
                }

                await LoadViewBagDataAsync();
                return View(news);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading edit news page for ID: {NewsId}", id);
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải trang chỉnh sửa.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Admin/News/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("NewsID,Title,ShortDescription,Content,ImageUrl,NewsType,AuthorID,IsPublished,PublishedDate")] News news, IFormFile? ImageFile)
        {
            if (id != news.NewsID)
            {
                TempData["ErrorMessage"] = "ID tin tức không khớp.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                // Custom validation
                if (await _context.News.AnyAsync(n => n.Title.ToLower() == news.Title.ToLower() && n.NewsID != id))
                {
                    ModelState.AddModelError("Title", "Tiêu đề tin tức đã tồn tại.");
                }

                if (ModelState.IsValid)
                {
                    var existingNews = await _context.News.AsNoTracking().FirstOrDefaultAsync(n => n.NewsID == id);
                    if (existingNews == null)
                    {
                        TempData["ErrorMessage"] = "Không tìm thấy tin tức để cập nhật.";
                        return RedirectToAction(nameof(Index));
                    }

                    // Handle image upload
                    if (ImageFile != null && ImageFile.Length > 0)
                    {
                        var imageValidation = ValidateImageFile(ImageFile);
                        if (!string.IsNullOrEmpty(imageValidation))
                        {
                            ModelState.AddModelError("ImageFile", imageValidation);
                            await LoadViewBagDataAsync();
                            return View(news);
                        }

                        // Delete old image if exists
                        if (!string.IsNullOrEmpty(existingNews.ImageUrl))
                        {
                            DeleteImageFile(existingNews.ImageUrl);
                        }

                        news.ImageUrl = await SaveImageFileAsync(ImageFile, "news");
                    }
                    else
                    {
                        // Keep existing image
                        news.ImageUrl = existingNews.ImageUrl;
                    }

                    _context.Update(news);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation("News updated successfully: {NewsTitle} by Admin", news.Title);
                    TempData["SuccessMessage"] = "Tin tức đã được cập nhật thành công!";
                    return RedirectToAction(nameof(Index));
                }
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogError(ex, "Concurrency error updating news: {NewsId}", id);
                if (!await NewsExistsAsync(news.NewsID))
                {
                    TempData["ErrorMessage"] = "Tin tức không tồn tại .";
                    return RedirectToAction(nameof(Index));
                }
                else
                {
                    TempData["ErrorMessage"] = "Tin tức đã bị thay đổi bởi người dùng khác. Vui lòng thử lại.";
                }   
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating news: {NewsId}", id);
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi cập nhật tin tức.";
            }

            await LoadViewBagDataAsync();
            return View(news);
        }

        // GET: Admin/News/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                TempData["ErrorMessage"] = "ID tin tức không hợp lệ.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                var news = await _context.News
                    .Include(n => n.Author)
                    .FirstOrDefaultAsync(m => m.NewsID == id);

                if (news == null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy tin tức.";
                    return RedirectToAction(nameof(Index));
                }

                return View(news);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading delete news page for ID: {NewsId}", id);
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải trang xóa tin tức.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Admin/News/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                var news = await _context.News.FindAsync(id);
                if (news != null)
                {
                    // Delete image file if exists
                    if (!string.IsNullOrEmpty(news.ImageUrl))
                    {
                        DeleteImageFile(news.ImageUrl);
                    }

                    _context.News.Remove(news);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation("News deleted successfully: {NewsTitle} by Admin", news.Title);
                    TempData["SuccessMessage"] = "Tin tức đã được xóa thành công!";
                }
                else
                {
                    TempData["ErrorMessage"] = "Không tìm thấy tin tức để xóa.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting news: {NewsId}", id);
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi xóa tin tức.";
            }

            return RedirectToAction(nameof(Index));
        }

        // Helper methods
        private async Task<bool> NewsExistsAsync(int id)
        {
            return await _context.News.AnyAsync(e => e.NewsID == id);
        }

        private async Task LoadViewBagDataAsync()
        {
            try
            {
                var authors = await _context.Users
                    .Where(u => u.IsActive)
                    .OrderBy(u => u.FullName)
                    .ToListAsync();

                _logger.LogInformation("Found {AuthorCount} active authors", authors.Count);

                // Always include "-- Không chọn tác giả --" option
                var authorList = new List<object>();
                authorList.Add(new { UserID = (int?)null, FullName = "-- Không chọn tác giả --" });
                
                foreach (var author in authors)
                {
                    authorList.Add(new { UserID = (int?)author.UserID, FullName = author.FullName });
                }

                ViewBag.AuthorID = new SelectList(authorList, "UserID", "FullName");
                ViewBag.NewsTypes = new SelectList(new[]
                {
                    new { Value = "News", Text = "Tin tức" },
                    new { Value = "Promotion", Text = "Khuyến mãi" }
                }, "Value", "Text");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading ViewBag data for News");
                // Provide fallback with no author option
                ViewBag.AuthorID = new SelectList(new[]
                {
                    new { UserID = (int?)null, FullName = "-- Không chọn tác giả --" }
                }, "UserID", "FullName");
                ViewBag.NewsTypes = new SelectList(new[]
                {
                    new { Value = "News", Text = "Tin tức" },
                    new { Value = "Promotion", Text = "Khuyến mãi" }
                }, "Value", "Text");
            }
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


