using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using JollibeeClone.Data;
using JollibeeClone.Models;

namespace JollibeeClone.Controllers
{
    public class ServiceController : Controller
    {
        private readonly AppDbContext _context;
        private readonly ILogger<ServiceController> _logger;

        public ServiceController(AppDbContext context, ILogger<ServiceController> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                // Lấy danh sách dịch vụ đang hoạt động, sắp xếp theo thứ tự hiển thị
                var services = await _context.Services
                    .Where(s => s.IsActive)
                    .OrderBy(s => s.DisplayOrder)
                    .ThenBy(s => s.ServiceID)
                    .ToListAsync();

                return View(services);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading services for user view");
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải danh sách dịch vụ.";
                return View(new List<Service>());
            }
        }

        [Route("Service/Promotion")]
        public IActionResult Promotions()
        {
            return View();
        }

       

        // API để lấy chi tiết dịch vụ
        [HttpGet]
        public async Task<IActionResult> GetServiceDetails(int id)
        {
            try
            {
                var service = await _context.Services
                    .Where(s => s.ServiceID == id && s.IsActive)
                    .FirstOrDefaultAsync();

                if (service == null)
                {
                    return Json(new { success = false, message = "Dịch vụ không tồn tại." });
                }

                return Json(new
                {
                    success = true,
                    data = new
                    {
                        service.ServiceID,
                        service.ServiceName,
                        service.ShortDescription,
                        service.Content,
                        service.ImageUrl
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting service details for ID: {ServiceId}", id);
                return Json(new { success = false, message = "Có lỗi xảy ra." });
            }
        }
    }
} 