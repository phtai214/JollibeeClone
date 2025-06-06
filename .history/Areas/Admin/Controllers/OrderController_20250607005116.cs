using JollibeeClone.Areas.Admin.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using JollibeeClone.Areas.Admin.Attributes;

namespace JollibeeClone.Areas.Admin.Controllers
{
    [Area("Admin")]
    [AdminAuthorize]
    public class OrderController : Controller
    {
        private readonly ILogger<OrderController> _logger;

        public OrderController(ILogger<OrderController> logger)
        {
            _logger = logger;
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

                var orders = GetMockOrders();

                // Apply filters
                if (!string.IsNullOrEmpty(searchString))
                {
                    orders = orders.Where(o => 
                        o.CustomerFullName.Contains(searchString, StringComparison.OrdinalIgnoreCase) ||
                        o.CustomerPhoneNumber.Contains(searchString, StringComparison.OrdinalIgnoreCase) ||
                        o.OrderCode.Contains(searchString, StringComparison.OrdinalIgnoreCase)
                    ).ToList();
                }

                if (statusId.HasValue)
                {
                    orders = orders.Where(o => o.OrderStatusID == statusId.Value).ToList();
                }

                if (fromDate.HasValue)
                {
                    orders = orders.Where(o => o.OrderDate.Date >= fromDate.Value.Date).ToList();
                }

                if (toDate.HasValue)
                {
                    orders = orders.Where(o => o.OrderDate.Date <= toDate.Value.Date).ToList();
                }

                // Apply sorting
                switch (sortOrder)
                {
                    case "date_desc":
                        orders = orders.OrderByDescending(o => o.OrderDate).ToList();
                        break;
                    case "date_asc":
                        orders = orders.OrderBy(o => o.OrderDate).ToList();
                        break;
                    case "total_desc":
                        orders = orders.OrderByDescending(o => o.TotalAmount).ToList();
                        break;
                    case "total_asc":
                        orders = orders.OrderBy(o => o.TotalAmount).ToList();
                        break;
                    case "customer":
                        orders = orders.OrderBy(o => o.CustomerFullName).ToList();
                        break;
                    default:
                        orders = orders.OrderByDescending(o => o.OrderDate).ToList();
                        break;
                }

                // Create paginated list
                var skip = (pageNumber - 1) * pageSize;
                var pagedOrders = orders.Skip(skip).Take(pageSize).ToList();
                var totalCount = orders.Count;
                var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

                var paginatedOrders = new PaginatedList<Orders>(pagedOrders, totalCount, pageNumber, pageSize);

                // ViewBag for filters and pagination
                ViewBag.OrderStatuses = GetMockOrderStatuses();
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
                var order = GetMockOrders().FirstOrDefault(o => o.OrderID == id);

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
                var order = GetMockOrders().FirstOrDefault(o => o.OrderID == id);

                if (order == null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy đơn hàng.";
                    return RedirectToAction(nameof(Index));
                }

                // Get available statuses based on current status
                var availableStatuses = GetAvailableStatuses(order.OrderStatusID);
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
                var order = GetMockOrders().FirstOrDefault(o => o.OrderID == OrderID);

                if (order == null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy đơn hàng.";
                    return RedirectToAction(nameof(Index));
                }

                // Validate status transition
                if (!IsValidStatusTransition(order.OrderStatusID, NewOrderStatusID))
                {
                    TempData["ErrorMessage"] = "Không thể chuyển từ trạng thái hiện tại sang trạng thái mới.";
                    return RedirectToAction("UpdateStatus", new { id = OrderID });
                }

                // In real implementation, update the database here
                // For now, just simulate success
                var newStatus = GetMockOrderStatuses().FirstOrDefault(s => s.OrderStatusID == NewOrderStatusID);
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

        #region Mock Data Methods

        private List<Orders> GetMockOrders()
        {
            var orderStatuses = GetMockOrderStatuses();
            var paymentMethods = GetMockPaymentMethods();
            var deliveryMethods = GetMockDeliveryMethods();
            var products = GetMockProducts();

            var orders = new List<Orders>
            {
                new Orders
                {
                    OrderID = 1,
                    OrderCode = "JBC001",
                    CustomerFullName = "Nguyễn Văn An",
                    CustomerEmail = "nguyenvanan@gmail.com",
                    CustomerPhoneNumber = "0901234567",
                    OrderDate = DateTime.Now.AddDays(-2),
                    SubtotalAmount = 180000,
                    ShippingFee = 20000,
                    DiscountAmount = 10000,
                    TotalAmount = 190000,
                    OrderStatusID = 1,
                    PaymentMethodID = 1,
                    DeliveryMethodID = 1,
                    NotesByCustomer = "Giao hàng nhanh giúp em",
                    OrderStatus = orderStatuses.First(s => s.OrderStatusID == 1),
                    PaymentMethod = paymentMethods.First(p => p.PaymentMethodID == 1),
                    DeliveryMethod = deliveryMethods.First(d => d.DeliveryMethodID == 1),
                    OrderItems = new List<OrderItems>
                    {
                        new OrderItems
                        {
                            OrderItemID = 1,
                            OrderID = 1,
                            ProductID = 1,
                            ProductNameSnapshot = "Burger Zinger",
                            Quantity = 2,
                            UnitPrice = 65000,
                            Subtotal = 130000,
                            Product = products.First(p => p.ProductID == 1)
                        },
                        new OrderItems
                        {
                            OrderItemID = 2,
                            OrderID = 1,
                            ProductID = 2,
                            ProductNameSnapshot = "Gà Rán Giòn Cay",
                            Quantity = 1,
                            UnitPrice = 50000,
                            Subtotal = 50000,
                            Product = products.First(p => p.ProductID == 2)
                        }
                    }
                },
                new Orders
                {
                    OrderID = 2,
                    OrderCode = "JBC002",
                    CustomerFullName = "Trần Thị Bình",
                    CustomerEmail = "tranthibinh@gmail.com",
                    CustomerPhoneNumber = "0909876543",
                    OrderDate = DateTime.Now.AddDays(-1),
                    SubtotalAmount = 120000,
                    ShippingFee = 15000,
                    DiscountAmount = 0,
                    TotalAmount = 135000,
                    OrderStatusID = 2,
                    PaymentMethodID = 2,
                    DeliveryMethodID = 1,
                    NotesByCustomer = "",
                    OrderStatus = orderStatuses.First(s => s.OrderStatusID == 2),
                    PaymentMethod = paymentMethods.First(p => p.PaymentMethodID == 2),
                    DeliveryMethod = deliveryMethods.First(d => d.DeliveryMethodID == 1),
                    OrderItems = new List<OrderItems>
                    {
                        new OrderItems
                        {
                            OrderItemID = 3,
                            OrderID = 2,
                            ProductID = 3,
                            ProductNameSnapshot = "Combo Gia Đình",
                            Quantity = 1,
                            UnitPrice = 120000,
                            Subtotal = 120000,
                            Product = products.First(p => p.ProductID == 3)
                        }
                    }
                },
                new Orders
                {
                    OrderID = 3,
                    OrderCode = "JBC003",
                    CustomerFullName = "Lê Minh Cường",
                    CustomerEmail = "leminhcuong@gmail.com",
                    CustomerPhoneNumber = "0912345678",
                    OrderDate = DateTime.Now.AddHours(-5),
                    SubtotalAmount = 85000,
                    ShippingFee = 20000,
                    DiscountAmount = 5000,
                    TotalAmount = 100000,
                    OrderStatusID = 3,
                    PaymentMethodID = 1,
                    DeliveryMethodID = 1,
                    NotesByCustomer = "Không cay",
                    OrderStatus = orderStatuses.First(s => s.OrderStatusID == 3),
                    PaymentMethod = paymentMethods.First(p => p.PaymentMethodID == 1),
                    DeliveryMethod = deliveryMethods.First(d => d.DeliveryMethodID == 1),
                    OrderItems = new List<OrderItems>
                    {
                        new OrderItems
                        {
                            OrderItemID = 4,
                            OrderID = 3,
                            ProductID = 4,
                            ProductNameSnapshot = "Cơm Gà Teriyaki",
                            Quantity = 1,
                            UnitPrice = 85000,
                            Subtotal = 85000,
                            Product = products.First(p => p.ProductID == 4)
                        }
                    }
                },
                new Orders
                {
                    OrderID = 4,
                    OrderCode = "JBC004",
                    CustomerFullName = "Phạm Thị Dung",
                    CustomerEmail = "phamthidung@gmail.com",
                    CustomerPhoneNumber = "0923456789",
                    OrderDate = DateTime.Now.AddHours(-2),
                    SubtotalAmount = 95000,
                    ShippingFee = 25000,
                    DiscountAmount = 0,
                    TotalAmount = 120000,
                    OrderStatusID = 4,
                    PaymentMethodID = 3,
                    DeliveryMethodID = 1,
                    NotesByCustomer = "",
                    OrderStatus = orderStatuses.First(s => s.OrderStatusID == 4),
                    PaymentMethod = paymentMethods.First(p => p.PaymentMethodID == 3),
                    DeliveryMethod = deliveryMethods.First(d => d.DeliveryMethodID == 1),
                    OrderItems = new List<OrderItems>
                    {
                        new OrderItems
                        {
                            OrderItemID = 5,
                            OrderID = 4,
                            ProductID = 5,
                            ProductNameSnapshot = "Pepsi Cola",
                            Quantity = 2,
                            UnitPrice = 15000,
                            Subtotal = 30000,
                            Product = products.First(p => p.ProductID == 5)
                        },
                        new OrderItems
                        {
                            OrderItemID = 6,
                            OrderID = 4,
                            ProductID = 1,
                            ProductNameSnapshot = "Burger Zinger",
                            Quantity = 1,
                            UnitPrice = 65000,
                            Subtotal = 65000,
                            Product = products.First(p => p.ProductID == 1)
                        }
                    }
                },
                new Orders
                {
                    OrderID = 5,
                    OrderCode = "JBC005",
                    CustomerFullName = "Hoàng Văn Em",
                    CustomerEmail = "hoangvanem@gmail.com",
                    CustomerPhoneNumber = "0934567890",
                    OrderDate = DateTime.Now.AddDays(-3),
                    SubtotalAmount = 150000,
                    ShippingFee = 0,
                    DiscountAmount = 15000,
                    TotalAmount = 135000,
                    OrderStatusID = 5,
                    PaymentMethodID = 1,
                    DeliveryMethodID = 2,
                    NotesByCustomer = "Đã hoàn thành giao hàng",
                    OrderStatus = orderStatuses.First(s => s.OrderStatusID == 5),
                    PaymentMethod = paymentMethods.First(p => p.PaymentMethodID == 1),
                    DeliveryMethod = deliveryMethods.First(d => d.DeliveryMethodID == 2),
                    OrderItems = new List<OrderItems>
                    {
                        new OrderItems
                        {
                            OrderItemID = 7,
                            OrderID = 5,
                            ProductID = 3,
                            ProductNameSnapshot = "Combo Gia Đình",
                            Quantity = 1,
                            UnitPrice = 120000,
                            Subtotal = 120000,
                            Product = products.First(p => p.ProductID == 3)
                        },
                        new OrderItems
                        {
                            OrderItemID = 8,
                            OrderID = 5,
                            ProductID = 6,
                            ProductNameSnapshot = "Khoai Tây Chiên",
                            Quantity = 1,
                            UnitPrice = 30000,
                            Subtotal = 30000,
                            Product = products.First(p => p.ProductID == 6)
                        }
                    }
                },
                new Orders
                {
                    OrderID = 6,
                    OrderCode = "JBC006",
                    CustomerFullName = "Ngô Thị Phương",
                    CustomerEmail = "ngothiphuong@gmail.com",
                    CustomerPhoneNumber = "0945678901",
                    OrderDate = DateTime.Now.AddDays(-1),
                    SubtotalAmount = 75000,
                    ShippingFee = 20000,
                    DiscountAmount = 0,
                    TotalAmount = 95000,
                    OrderStatusID = 6,
                    PaymentMethodID = 2,
                    DeliveryMethodID = 1,
                    NotesByCustomer = "Khách hàng hủy do thay đổi ý kiến",
                    OrderStatus = orderStatuses.First(s => s.OrderStatusID == 6),
                    PaymentMethod = paymentMethods.First(p => p.PaymentMethodID == 2),
                    DeliveryMethod = deliveryMethods.First(d => d.DeliveryMethodID == 1),
                    OrderItems = new List<OrderItems>
                    {
                        new OrderItems
                        {
                            OrderItemID = 9,
                            OrderID = 6,
                            ProductID = 2,
                            ProductNameSnapshot = "Gà Rán Giòn Cay",
                            Quantity = 1,
                            UnitPrice = 50000,
                            Subtotal = 50000,
                            Product = products.First(p => p.ProductID == 2)
                        },
                        new OrderItems
                        {
                            OrderItemID = 10,
                            OrderID = 6,
                            ProductID = 7,
                            ProductNameSnapshot = "Tôm Viên Chiên",
                            Quantity = 1,
                            UnitPrice = 25000,
                            Subtotal = 25000,
                            Product = products.First(p => p.ProductID == 7)
                        }
                    }
                }
            };

            return orders;
        }

        private List<OrderStatuses> GetMockOrderStatuses()
        {
            return new List<OrderStatuses>
            {
                new OrderStatuses { OrderStatusID = 1, StatusName = "Chờ xác nhận", Description = "Đơn hàng mới chờ xác nhận" },
                new OrderStatuses { OrderStatusID = 2, StatusName = "Đã xác nhận", Description = "Đơn hàng đã được xác nhận" },
                new OrderStatuses { OrderStatusID = 3, StatusName = "Đang chuẩn bị", Description = "Đang chuẩn bị đơn hàng" },
                new OrderStatuses { OrderStatusID = 4, StatusName = "Đang giao", Description = "Đơn hàng đang được giao" },
                new OrderStatuses { OrderStatusID = 5, StatusName = "Đã giao", Description = "Đơn hàng đã được giao thành công" },
                new OrderStatuses { OrderStatusID = 6, StatusName = "Đã hủy", Description = "Đơn hàng đã bị hủy" }
            };
        }

        private List<PaymentMethods> GetMockPaymentMethods()
        {
            return new List<PaymentMethods>
            {
                new PaymentMethods { PaymentMethodID = 1, MethodName = "Tiền mặt", IsActive = true },
                new PaymentMethods { PaymentMethodID = 2, MethodName = "Chuyển khoản", IsActive = true },
                new PaymentMethods { PaymentMethodID = 3, MethodName = "Ví điện tử", IsActive = true }
            };
        }

        private List<DeliveryMethods> GetMockDeliveryMethods()
        {
            return new List<DeliveryMethods>
            {
                new DeliveryMethods { DeliveryMethodID = 1, MethodName = "Giao hàng tận nơi", Description = "Giao hàng tận nơi trong 30-45 phút", IsActive = true },
                new DeliveryMethods { DeliveryMethodID = 2, MethodName = "Nhận tại cửa hàng", Description = "Khách hàng đến nhận tại cửa hàng", IsActive = true }
            };
        }

        private List<Product> GetMockProducts()
        {
            return new List<Product>
            {
                new Product { ProductID = 1, ProductName = "Burger Zinger", Price = 65000, IsAvailable = true },
                new Product { ProductID = 2, ProductName = "Gà Rán Giòn Cay", Price = 50000, IsAvailable = true },
                new Product { ProductID = 3, ProductName = "Combo Gia Đình", Price = 120000, IsAvailable = true },
                new Product { ProductID = 4, ProductName = "Cơm Gà Teriyaki", Price = 85000, IsAvailable = true },
                new Product { ProductID = 5, ProductName = "Pepsi Cola", Price = 15000, IsAvailable = true },
                new Product { ProductID = 6, ProductName = "Khoai Tây Chiên", Price = 30000, IsAvailable = true },
                new Product { ProductID = 7, ProductName = "Tôm Viên Chiên", Price = 25000, IsAvailable = true }
            };
        }

        private List<OrderStatuses> GetAvailableStatuses(int currentStatusId)
        {
            var allStatuses = GetMockOrderStatuses();
            
            // Business logic for status transitions
            switch (currentStatusId)
            {
                case 1: // Chờ xác nhận -> có thể chuyển thành Đã xác nhận hoặc Đã hủy
                    return allStatuses.Where(s => s.OrderStatusID == 2 || s.OrderStatusID == 6).ToList();
                
                case 2: // Đã xác nhận -> có thể chuyển thành Đang chuẩn bị hoặc Đã hủy
                    return allStatuses.Where(s => s.OrderStatusID == 3 || s.OrderStatusID == 6).ToList();
                
                case 3: // Đang chuẩn bị -> có thể chuyển thành Đang giao hoặc Đã hủy
                    return allStatuses.Where(s => s.OrderStatusID == 4 || s.OrderStatusID == 6).ToList();
                
                case 4: // Đang giao -> chỉ có thể chuyển thành Đã giao (không thể hủy khi đang giao)
                    return allStatuses.Where(s => s.OrderStatusID == 5).ToList();
                
                case 5: // Đã giao -> không thể chuyển đổi
                    return new List<OrderStatuses>();
                
                case 6: // Đã hủy -> không thể chuyển đổi
                    return new List<OrderStatuses>();
                
                default:
                    return new List<OrderStatuses>();
            }
        }

        private bool IsValidStatusTransition(int currentStatusId, int newStatusId)
        {
            var availableStatuses = GetAvailableStatuses(currentStatusId);
            return availableStatuses.Any(s => s.OrderStatusID == newStatusId);
        }

        #endregion
    }
}
