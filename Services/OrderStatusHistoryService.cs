using JollibeeClone.Data;
using JollibeeClone.Models;
using Microsoft.EntityFrameworkCore;

namespace JollibeeClone.Services
{
    public class OrderStatusHistoryService
    {
        private readonly AppDbContext _context;

        public OrderStatusHistoryService(AppDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Ghi lại lịch sử thay đổi trạng thái đơn hàng
        /// </summary>
        public async Task<OrderStatusHistory> LogStatusChangeAsync(int orderId, int statusId, string updatedBy = "System", string? note = null)
        {
            var history = new OrderStatusHistory
            {
                OrderID = orderId,
                OrderStatusID = statusId,
                UpdatedAt = DateTime.Now,
                UpdatedBy = updatedBy,
                Note = note
            };

            _context.OrderStatusHistories.Add(history);
            await _context.SaveChangesAsync();

            return history;
        }

        /// <summary>
        /// Lấy lịch sử trạng thái của một đơn hàng, sắp xếp theo thời gian
        /// </summary>
        public async Task<List<OrderStatusHistory>> GetOrderStatusHistoryAsync(int orderId)
        {
            return await _context.OrderStatusHistories
                .Include(h => h.OrderStatus)
                .Where(h => h.OrderID == orderId)
                .OrderBy(h => h.UpdatedAt)
                .ToListAsync();
        }

        /// <summary>
        /// Lấy thời gian của một trạng thái cụ thể cho đơn hàng
        /// </summary>
        public async Task<DateTime?> GetStatusTimeAsync(int orderId, int statusId)
        {
            var history = await _context.OrderStatusHistories
                .Where(h => h.OrderID == orderId && h.OrderStatusID == statusId)
                .OrderByDescending(h => h.UpdatedAt) // Lấy lần cuối cùng nếu có nhiều lần
                .FirstOrDefaultAsync();

            return history?.UpdatedAt;
        }

        /// <summary>
        /// Kiểm tra xem đơn hàng đã đi qua trạng thái nào chưa
        /// </summary>
        public async Task<bool> HasOrderPassedStatusAsync(int orderId, int statusId)
        {
            return await _context.OrderStatusHistories
                .AnyAsync(h => h.OrderID == orderId && h.OrderStatusID == statusId);
        }

        /// <summary>
        /// Ghi lại trạng thái đầu tiên của đơn hàng (khi tạo đơn hàng)
        /// </summary>
        public async Task LogInitialStatusAsync(int orderId, DateTime orderDate)
        {
            // Trạng thái "Chờ xác nhận" có ID = 1
            var initialHistory = new OrderStatusHistory
            {
                OrderID = orderId,
                OrderStatusID = 1, // "Chờ xác nhận"
                UpdatedAt = orderDate,
                UpdatedBy = "System",
                Note = "Đơn hàng được tạo"
            };

            _context.OrderStatusHistories.Add(initialHistory);
            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Migrate dữ liệu từ Orders.LastStatusUpdateTime sang OrderStatusHistory
        /// </summary>
        public async Task MigrateExistingOrdersAsync()
        {
            var ordersWithoutHistory = await _context.Orders
                .Where(o => !_context.OrderStatusHistories.Any(h => h.OrderID == o.OrderID))
                .ToListAsync();

            foreach (var order in ordersWithoutHistory)
            {
                // Tạo lịch sử cho trạng thái đầu tiên (đặt hàng)
                var initialHistory = new OrderStatusHistory
                {
                    OrderID = order.OrderID,
                    OrderStatusID = 1, // "Chờ xác nhận" 
                    UpdatedAt = order.OrderDate,
                    UpdatedBy = "System",
                    Note = "Đơn hàng được tạo (migrate data)"
                };
                _context.OrderStatusHistories.Add(initialHistory);

                // Nếu đơn hàng hiện tại không ở trạng thái "Chờ xác nhận" 
                if (order.OrderStatusID != 1)
                {
                    var currentStatusHistory = new OrderStatusHistory
                    {
                        OrderID = order.OrderID,
                        OrderStatusID = order.OrderStatusID,
                        UpdatedAt = order.OrderDate.AddMinutes(10), // Ước tính 10 phút sau khi đặt hàng
                        UpdatedBy = "System",
                        Note = "Trạng thái hiện tại (migrate data)"
                    };
                    _context.OrderStatusHistories.Add(currentStatusHistory);
                }
            }

            await _context.SaveChangesAsync();
        }
    }
} 