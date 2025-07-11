using JollibeeClone.Models;
using JollibeeClone.Data;
using JollibeeClone.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using JollibeeClone.Areas.Admin.Attributes;

namespace JollibeeClone.Areas.Admin.Controllers
{
    [Area("Admin")]
    [AdminAuthorize]
    public class OrderController : Controller
    {
        private readonly ILogger<OrderController> _logger;
        private readonly AppDbContext _context;
        private readonly OrderStatusHistoryService _statusHistoryService;

        public OrderController(ILogger<OrderController> logger, AppDbContext context, OrderStatusHistoryService statusHistoryService)
        {
            _logger = logger;
            _context = context;
            _statusHistoryService = statusHistoryService;
        }

        // GET: Admin/Order
        [HttpGet]
        [Route("Admin/Order")]
        [Route("Admin/Order/Index")]
        public async Task<IActionResult> Index(string searchString, int? statusId, DateTime? fromDate, DateTime? toDate, string sortOrder, int? page)
        {
            try
            {
                // Pagination settings
                int pageSize = 10; // 10 orders per page
                int pageNumber = page ?? 1;

                // Get orders from database with navigation properties
                var ordersQuery = _context.Orders
                    .Include(o => o.OrderStatus)
                    .Include(o => o.PaymentMethod)
                    .Include(o => o.DeliveryMethod)
                    .Include(o => o.User)
                    .AsQueryable();

                // Apply filters
                if (!string.IsNullOrEmpty(searchString))
                {
                    ordersQuery = ordersQuery.Where(o => 
                        o.CustomerFullName.Contains(searchString) ||
                        o.CustomerPhoneNumber.Contains(searchString) ||
                        o.OrderCode.Contains(searchString)
                    );
                }

                if (statusId.HasValue)
                {
                    ordersQuery = ordersQuery.Where(o => o.OrderStatusID == statusId.Value);
                }

                if (fromDate.HasValue)
                {
                    ordersQuery = ordersQuery.Where(o => o.OrderDate.Date >= fromDate.Value.Date);
                }

                if (toDate.HasValue)
                {
                    ordersQuery = ordersQuery.Where(o => o.OrderDate.Date <= toDate.Value.Date);
                }

                // Apply sorting
                switch (sortOrder)
                {
                    case "date_desc":
                        ordersQuery = ordersQuery.OrderByDescending(o => o.OrderDate);
                        break;
                    case "date_asc":
                        ordersQuery = ordersQuery.OrderBy(o => o.OrderDate);
                        break;
                    case "total_desc":
                        ordersQuery = ordersQuery.OrderByDescending(o => o.TotalAmount);
                        break;
                    case "total_asc":
                        ordersQuery = ordersQuery.OrderBy(o => o.TotalAmount);
                        break;
                    case "customer":
                        ordersQuery = ordersQuery.OrderBy(o => o.CustomerFullName);
                        break;
                    default:
                        ordersQuery = ordersQuery.OrderByDescending(o => o.OrderDate);
                        break;
                }

                // Get total count before pagination
                var totalCount = await ordersQuery.CountAsync();

                // Apply pagination
                var orders = await ordersQuery
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                var paginatedOrders = new PaginatedList<Orders>(orders, totalCount, pageNumber, pageSize);

                // Get order statuses for filter dropdown
                var orderStatuses = await _context.OrderStatuses
                    .OrderBy(s => s.OrderStatusID)
                    .ToListAsync();

                // ViewBag for filters and pagination
                ViewBag.OrderStatuses = orderStatuses;
                ViewBag.CurrentSearch = searchString;
                ViewBag.CurrentStatus = statusId;
                ViewBag.CurrentFromDate = fromDate?.ToString("yyyy-MM-dd");
                ViewBag.CurrentToDate = toDate?.ToString("yyyy-MM-dd");
                ViewBag.CurrentSort = sortOrder;

                return View(paginatedOrders);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading orders");
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải danh sách đơn hàng.";
                return View(new PaginatedList<Orders>(new List<Orders>(), 0, 1, 10));
            }
        }

        // GET: Admin/Order/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                TempData["ErrorMessage"] = "ID đơn hàng không hợp lệ.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                var order = await _context.Orders
                    .Include(o => o.OrderStatus)
                    .Include(o => o.PaymentMethod)
                    .Include(o => o.DeliveryMethod)
                    .Include(o => o.User)
                    .Include(o => o.UserAddress)
                    .Include(o => o.Store)
                    .Include(o => o.OrderItems)
                        .ThenInclude(oi => oi.Product)
                    .FirstOrDefaultAsync(o => o.OrderID == id);

                if (order == null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy đơn hàng.";
                    return RedirectToAction(nameof(Index));
                }

                return View(order);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading order details for ID: {OrderId}", id);
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải thông tin đơn hàng.";
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: Admin/Order/UpdateStatus/5
        public async Task<IActionResult> UpdateStatus(int? id)
        {
            if (id == null)
            {
                TempData["ErrorMessage"] = "ID đơn hàng không hợp lệ.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                var order = await _context.Orders
                    .Include(o => o.OrderStatus)
                    .Include(o => o.PaymentMethod)
                    .Include(o => o.DeliveryMethod)
                    .FirstOrDefaultAsync(o => o.OrderID == id);

                if (order == null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy đơn hàng.";
                    return RedirectToAction(nameof(Index));
                }

                // Get available statuses based on current status
                var availableStatuses = await GetAvailableStatusesAsync(order.OrderStatusID, order.DeliveryMethodID);
                ViewBag.OrderStatuses = new SelectList(availableStatuses, "OrderStatusID", "StatusName");
                
                return View(order);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading update status page for ID: {OrderId}", id);
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải trang cập nhật trạng thái.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Admin/Order/UpdateStatus
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int OrderID, int NewOrderStatusID)
        {
            try
            {
                var order = await _context.Orders
                    .Include(o => o.OrderStatus)
                    .FirstOrDefaultAsync(o => o.OrderID == OrderID);

                if (order == null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy đơn hàng.";
                    return RedirectToAction(nameof(Index));
                }

                // Validate status transition
                if (!await IsValidStatusTransitionAsync(order.OrderStatusID, NewOrderStatusID, order.DeliveryMethodID))
                {
                    TempData["ErrorMessage"] = "Không thể chuyển từ trạng thái hiện tại sang trạng thái mới.";
                    return RedirectToAction("UpdateStatus", new { id = OrderID });
                }

                // Update order status and log history
                order.OrderStatusID = NewOrderStatusID;
                await _context.SaveChangesAsync();

                // Ghi lại lịch sử thay đổi trạng thái
                await _statusHistoryService.LogStatusChangeAsync(
                    orderId: order.OrderID,
                    statusId: NewOrderStatusID,
                    updatedBy: "Admin",
                    note: "Trạng thái được cập nhật bởi admin"
                );

                var newStatus = await _context.OrderStatuses
                    .FirstOrDefaultAsync(s => s.OrderStatusID == NewOrderStatusID);
                    
                TempData["SuccessMessage"] = $"Đã cập nhật trạng thái đơn hàng #{order.OrderCode} thành: {newStatus?.StatusName}";

                return RedirectToAction(nameof(Details), new { id = OrderID });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating order status for ID: {OrderId}", OrderID);
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi cập nhật trạng thái đơn hàng.";
                return RedirectToAction(nameof(Index));
            }
        }

        #region Helper Methods

        /// <summary>
        /// Get available status transitions based on current status and delivery method
        /// </summary>
        private async Task<List<OrderStatuses>> GetAvailableStatusesAsync(int currentStatusId, int? deliveryMethodId = null)
        {
            try
            {
                var allStatuses = await _context.OrderStatuses.ToListAsync();
                List<OrderStatuses> availableStatuses = new List<OrderStatuses>();
                
                // Define valid status transitions based on business logic
                switch (currentStatusId)
                {
                    case 1: // Chờ xác nhận -> có thể chuyển thành Đã xác nhận hoặc Đã hủy
                        availableStatuses = allStatuses.Where(s => s.OrderStatusID == 2 || s.OrderStatusID == 7).ToList();
                        break;
                        
                    case 2: // Đã xác nhận -> có thể chuyển thành Đang chuẩn bị hoặc Đã hủy
                        availableStatuses = allStatuses.Where(s => s.OrderStatusID == 3 || s.OrderStatusID == 7).ToList();
                        break;
                        
                    case 3: // Đang chuẩn bị -> logic phụ thuộc vào loại giao hàng
                        if (deliveryMethodId == 1) // Giao hàng tận nơi
                        {
                            // Có thể chuyển thành Đang giao hàng hoặc Hoàn thành hoặc Đã hủy
                            availableStatuses = allStatuses.Where(s => s.OrderStatusID == 4 || s.OrderStatusID == 6 || s.OrderStatusID == 7).ToList();
                        }
                        else if (deliveryMethodId == 2) // Hẹn lấy tại cửa hàng
                        {
                            // Có thể chuyển thành Sẵn sàng lấy hàng hoặc Hoàn thành hoặc Đã hủy
                            availableStatuses = allStatuses.Where(s => s.OrderStatusID == 5 || s.OrderStatusID == 6 || s.OrderStatusID == 7).ToList();
                        }
                        else
                        {
                            // Fallback: cho phép cả hai
                            availableStatuses = allStatuses.Where(s => s.OrderStatusID == 4 || s.OrderStatusID == 5 || s.OrderStatusID == 6 || s.OrderStatusID == 7).ToList();
                        }
                        break;
                        
                    case 4: // Đang giao hàng -> có thể chuyển thành Hoàn thành
                        availableStatuses = allStatuses.Where(s => s.OrderStatusID == 6).ToList();
                        break;
                        
                    case 5: // Sẵn sàng lấy hàng -> có thể chuyển thành Hoàn thành
                        availableStatuses = allStatuses.Where(s => s.OrderStatusID == 6).ToList();
                        break;
                        
                    case 6: // Hoàn thành -> không thể chuyển (trạng thái cuối)
                    case 7: // Đã hủy -> không thể chuyển (trạng thái cuối)
                    default:
                        availableStatuses = new List<OrderStatuses>();
                        break;
                }
                
                return availableStatuses;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting available statuses for status ID: {StatusId}", currentStatusId);
                return new List<OrderStatuses>();
            }
        }

        /// <summary>
        /// Validate if status transition is allowed
        /// </summary>
        private async Task<bool> IsValidStatusTransitionAsync(int currentStatusId, int newStatusId, int? deliveryMethodId = null)
        {
            try
            {
                var availableStatuses = await GetAvailableStatusesAsync(currentStatusId, deliveryMethodId);
                return availableStatuses.Any(s => s.OrderStatusID == newStatusId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating status transition from {CurrentId} to {NewId}", currentStatusId, newStatusId);
                return false;
            }
        }

        /// <summary>
        /// Get Vietnam local time to ensure consistent timezone handling
        /// </summary>
        private DateTime GetVietnamLocalTime()
        {
            var vietnamTimeZone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
            return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, vietnamTimeZone);
        }

        #endregion
    }
}



