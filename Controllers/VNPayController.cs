using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using JollibeeClone.Data;
using JollibeeClone.Services;
using JollibeeClone.ViewModels;
using Newtonsoft.Json;
using Microsoft.AspNetCore.Http.Extensions;

namespace JollibeeClone.Controllers
{
    public class VNPayController : Controller
    {
        private readonly AppDbContext _context;
        private readonly VNPayService _vnPayService;
        private readonly EmailService _emailService;
        private readonly OrderStatusHistoryService _statusHistoryService;

        public VNPayController(AppDbContext context, VNPayService vnPayService, EmailService emailService, OrderStatusHistoryService statusHistoryService)
        {
            _context = context;
            _vnPayService = vnPayService;
            _emailService = emailService;
            _statusHistoryService = statusHistoryService;
        }

        // GET: /VNPay/PaymentCallback - Xử lý callback từ VNPay
        [HttpGet]
        public async Task<IActionResult> PaymentCallback()
        {
            try
            {
                Console.WriteLine("🔄 =================================");
                Console.WriteLine("🔄 VNPay Return callback received");
                Console.WriteLine($"🔍 Request URL: {Request.GetDisplayUrl()}");
                Console.WriteLine($"🔍 Query parameters count: {Request.Query.Count}");
                Console.WriteLine("🔄 =================================");
                
                foreach (var param in Request.Query)
                {
                    Console.WriteLine($"🔍 {param.Key} = {param.Value}");
                }
                Console.WriteLine("🔄 =================================");

                // Validate VNPay response
                var vnPayResponse = _vnPayService.ValidateCallback(Request.Query);
                
                Console.WriteLine($"🔍 VNPay Response validation:");
                Console.WriteLine($"  - Success: {vnPayResponse.Success}");
                Console.WriteLine($"  - OrderId: {vnPayResponse.OrderId}");
                Console.WriteLine($"  - Amount: {vnPayResponse.Amount:N0}₫");
                Console.WriteLine($"  - TransactionId: {vnPayResponse.TransactionId}");
                Console.WriteLine($"  - ResponseCode: {vnPayResponse.ResponseCode}");
                Console.WriteLine($"  - Message: {vnPayResponse.Message}");

                if (string.IsNullOrEmpty(vnPayResponse.OrderId))
                {
                    Console.WriteLine("❌ OrderId is null or empty in VNPay response");
                    TempData["ErrorMessage"] = "Thông tin đơn hàng không hợp lệ từ VNPay.";
                    return RedirectToAction("Index", "Home");
                }

                // Find order by OrderCode
                var order = await _context.Orders
                    .Include(o => o.PaymentMethod)
                    .Include(o => o.DeliveryMethod)
                    .Include(o => o.Store)
                    .Include(o => o.UserAddress)
                    .Include(o => o.OrderItems)
                        .ThenInclude(oi => oi.Product)
                    .FirstOrDefaultAsync(o => o.OrderCode == vnPayResponse.OrderId);

                if (order == null)
                {
                    Console.WriteLine($"❌ Order not found with OrderCode: {vnPayResponse.OrderId}");
                    TempData["ErrorMessage"] = "Không tìm thấy đơn hàng.";
                    return RedirectToAction("Index", "Home");
                }

                Console.WriteLine($"🔍 Order found: ID={order.OrderID}, Code={order.OrderCode}, Amount={order.TotalAmount:N0}₫");

                // Verify amount matches
                if (Math.Abs(vnPayResponse.Amount - order.TotalAmount) > 0.01m)
                {
                    Console.WriteLine($"❌ Amount mismatch: VNPay={vnPayResponse.Amount:N0}₫, Order={order.TotalAmount:N0}₫");
                    TempData["ErrorMessage"] = "Số tiền thanh toán không khớp với đơn hàng.";
                    return RedirectToAction("Index", "Home");
                }

                // Get existing payment record
                var payment = await _context.Payments
                    .FirstOrDefaultAsync(p => p.OrderID == order.OrderID);

                if (payment == null)
                {
                    Console.WriteLine($"❌ Payment record not found for OrderID: {order.OrderID}");
                    TempData["ErrorMessage"] = "Không tìm thấy thông tin thanh toán.";
                    return RedirectToAction("Index", "Home");
                }

                using var transaction = await _context.Database.BeginTransactionAsync();
                
                try
                {
                    if (vnPayResponse.Success)
                    {
                        Console.WriteLine("✅ VNPay payment successful - updating records");

                        // Update payment status
                        payment.PaymentStatus = "Completed";
                        payment.TransactionCode = vnPayResponse.TransactionId;
                        payment.PaymentDate = GetVietnamLocalTime();
                        payment.Notes = $"VNPay transaction successful. Bank: {vnPayResponse.BankCode}, TransactionId: {vnPayResponse.TransactionId}";
                        
                        Console.WriteLine($"🔄 Updating payment:");
                        Console.WriteLine($"  - PaymentID: {payment.PaymentID}");
                        Console.WriteLine($"  - New Status: Completed");
                        Console.WriteLine($"  - TransactionId: {vnPayResponse.TransactionId}");
                        
                        _context.Payments.Update(payment);

                        // Update order status to "Đã xác nhận" (ID=2) after successful VNPay payment
                        var orderStatusId = await GetPaidOrderStatusIdAsync();
                        Console.WriteLine($"🔄 Current order status: {order.OrderStatusID}, Target status: {orderStatusId}");
                        
                        if (orderStatusId.HasValue && order.OrderStatusID != orderStatusId.Value)
                        {
                            var oldStatusId = order.OrderStatusID;
                            order.OrderStatusID = orderStatusId.Value;
                            _context.Orders.Update(order);
                            Console.WriteLine($"✅ Order status updated from {oldStatusId} to {orderStatusId.Value}");
                            
                            // Log status change in history
                            try
                            {
                                await _statusHistoryService.LogStatusChangeAsync(
                                    orderId: order.OrderID,
                                    statusId: orderStatusId.Value,
                                    updatedBy: "VNPay System",
                                    note: $"Thanh toán VNPay thành công. TransactionId: {vnPayResponse.TransactionId}"
                                );
                                Console.WriteLine($"✅ Order status history logged successfully");
                            }
                            catch (Exception historyEx)
                            {
                                Console.WriteLine($"❌ Failed to log status history: {historyEx.Message}");
                                // Don't fail the whole transaction for history logging
                            }
                        }
                        else
                        {
                            Console.WriteLine($"⚠️ Order status not updated. Current: {order.OrderStatusID}, Target: {orderStatusId}");
                        }

                        Console.WriteLine($"🔄 Saving changes to database...");
                        var changesSaved = await _context.SaveChangesAsync();
                        Console.WriteLine($"🔄 Database changes saved: {changesSaved} records affected");
                        
                        Console.WriteLine($"🔄 Committing transaction...");
                        await transaction.CommitAsync();
                        Console.WriteLine($"✅ Transaction committed successfully");

                        // Send confirmation email
                        try
                        {
                            if (!string.IsNullOrEmpty(order.CustomerEmail))
                            {
                                Console.WriteLine($"📧 Sending confirmation email to: {order.CustomerEmail}");
                                var emailSent = await _emailService.SendOrderConfirmationEmailAsync(order, order.OrderItems.ToList());
                                Console.WriteLine($"📧 Email result: {(emailSent ? "✅ Success" : "❌ Failed")}");
                            }
                        }
                        catch (Exception emailEx)
                        {
                            Console.WriteLine($"❌ Email sending failed but payment completed: {emailEx.Message}");
                        }

                        Console.WriteLine($"🎉 VNPay payment completed successfully for order {order.OrderCode}");
                        Console.WriteLine($"🎉 Order final status: {order.OrderStatusID}");
                        Console.WriteLine($"🎉 Payment final status: {payment.PaymentStatus}");
                        
                        TempData["SuccessMessage"] = $"Thanh toán VNPay thành công cho đơn hàng {order.OrderCode}!";
                        
                        Console.WriteLine($"🔄 Redirecting to OrderSuccess with OrderID: {order.OrderID}");
                        Console.WriteLine($"🔄 OrderSuccess URL: /Cart/OrderSuccess?orderId={order.OrderID}");
                        
                        // Store order ID in session as backup
                        HttpContext.Session.SetInt32("LastOrderID", order.OrderID);
                        
                        return RedirectToAction("OrderSuccess", "Cart", new { orderId = order.OrderID });
                    }
                    else
                    {
                        Console.WriteLine("❌ VNPay payment failed - updating records");

                        // Update payment status to failed
                        payment.PaymentStatus = "Failed";
                        payment.TransactionCode = vnPayResponse.TransactionId ?? "FAILED_" + DateTime.Now.Ticks;
                        payment.PaymentDate = GetVietnamLocalTime();
                        payment.Notes = $"VNPay payment failed. Code: {vnPayResponse.ResponseCode}, Message: {vnPayResponse.Message}";
                        
                        _context.Payments.Update(payment);

                        // Update order status to "Hủy" (assuming OrderStatusID = 6 for cancelled)
                        var cancelledStatusId = await GetCancelledOrderStatusIdAsync();
                        if (cancelledStatusId.HasValue)
                        {
                            order.OrderStatusID = cancelledStatusId.Value;
                            _context.Orders.Update(order);
                            Console.WriteLine($"✅ Order status updated to cancelled (StatusID: {cancelledStatusId.Value})");
                        }

                        await _context.SaveChangesAsync();
                        await transaction.CommitAsync();

                        Console.WriteLine($"❌ VNPay payment failed for order {order.OrderCode}: {vnPayResponse.Message}");
                        TempData["ErrorMessage"] = $"Thanh toán VNPay thất bại: {vnPayResponse.Message}";
                        
                        return RedirectToAction("Index", "Home");
                    }
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    Console.WriteLine($"❌ Error updating payment status: {ex.Message}");
                    Console.WriteLine($"❌ Stack trace: {ex.StackTrace}");
                    
                    TempData["ErrorMessage"] = "Có lỗi xảy ra khi cập nhật trạng thái thanh toán.";
                    return RedirectToAction("Index", "Home");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error in VNPay Return: {ex.Message}");
                Console.WriteLine($"❌ Stack trace: {ex.StackTrace}");
                
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi xử lý phản hồi từ VNPay.";
                return RedirectToAction("Index", "Home");
            }
        }

        // GET: /VNPay/Return - Alias cho PaymentCallback để tương thích
        [HttpGet]
        public async Task<IActionResult> Return()
        {
            Console.WriteLine("🔄 VNPay Return endpoint called");
            Console.WriteLine($"🔍 Full URL: {Request.GetDisplayUrl()}");
            return await PaymentCallback();
        }

        // GET: /VNPay/Processing - Trang chờ thanh toán VNPay
        [HttpGet]
        public IActionResult Processing(string orderCode)
        {
            if (string.IsNullOrEmpty(orderCode))
            {
                TempData["ErrorMessage"] = "Mã đơn hàng không hợp lệ.";
                return RedirectToAction("Index", "Home");
            }

            var viewModel = new VNPayProcessingViewModel
            {
                OrderCode = orderCode,
                CreatedDate = DateTime.Now
            };

            return View(viewModel);
        }

        // GET: /VNPay/Debug - Debug endpoint to check order status
        [HttpGet]
        public async Task<IActionResult> Debug(string orderCode)
        {
            try
            {
                if (string.IsNullOrEmpty(orderCode))
                {
                    return Json(new { error = "OrderCode is required" });
                }

                // Find order by OrderCode
                var order = await _context.Orders
                    .Include(o => o.OrderStatus)
                    .Include(o => o.PaymentMethod)
                    .FirstOrDefaultAsync(o => o.OrderCode == orderCode);

                if (order == null)
                {
                    return Json(new { error = "Order not found", orderCode = orderCode });
                }

                // Get payment info
                var payment = await _context.Payments
                    .FirstOrDefaultAsync(p => p.OrderID == order.OrderID);

                return Json(new
                {
                    success = true,
                    order = new
                    {
                        order.OrderID,
                        order.OrderCode,
                        order.TotalAmount,
                        order.OrderDate,
                        StatusName = order.OrderStatus.StatusName,
                        PaymentMethodName = order.PaymentMethod.MethodName
                    },
                    payment = payment == null ? null : new
                    {
                        payment.PaymentID,
                        payment.PaymentStatus,
                        payment.TransactionCode,
                        payment.PaymentDate,
                        payment.Notes
                    }
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error in Debug: {ex.Message}");
                return Json(new { error = ex.Message });
            }
        }

        // GET: /VNPay/TestCallback - Test callback manually (for debugging)
        [HttpGet]
        public async Task<IActionResult> TestCallback(string orderCode, string responseCode = "00")
        {
            try
            {
                Console.WriteLine($"🧪 TEST CALLBACK - OrderCode: {orderCode}, ResponseCode: {responseCode}");
                
                if (string.IsNullOrEmpty(orderCode))
                {
                    return Json(new { error = "OrderCode is required" });
                }

                // Find order by OrderCode
                var order = await _context.Orders
                    .Include(o => o.PaymentMethod)
                    .Include(o => o.DeliveryMethod)
                    .Include(o => o.Store)
                    .Include(o => o.UserAddress)
                    .Include(o => o.OrderItems)
                        .ThenInclude(oi => oi.Product)
                    .FirstOrDefaultAsync(o => o.OrderCode == orderCode);

                if (order == null)
                {
                    return Json(new { error = "Order not found", orderCode = orderCode });
                }

                // Get payment
                var payment = await _context.Payments
                    .FirstOrDefaultAsync(p => p.OrderID == order.OrderID);

                if (payment == null)
                {
                    return Json(new { error = "Payment record not found" });
                }

                using var transaction = await _context.Database.BeginTransactionAsync();
                
                try
                {
                    if (responseCode == "00") // Success
                    {
                        // Update payment status
                        payment.PaymentStatus = "Completed";
                        payment.TransactionCode = "TEST_" + DateTime.Now.Ticks;
                        payment.PaymentDate = GetVietnamLocalTime();
                        payment.Notes = "Test VNPay transaction successful";
                        
                        _context.Payments.Update(payment);

                        // Update order status
                        var orderStatusId = await GetPaidOrderStatusIdAsync();
                        if (orderStatusId.HasValue && order.OrderStatusID != orderStatusId.Value)
                        {
                            order.OrderStatusID = orderStatusId.Value;
                            _context.Orders.Update(order);
                        }

                        await _context.SaveChangesAsync();
                        await transaction.CommitAsync();

                        // Store order ID in session
                        HttpContext.Session.SetInt32("LastOrderID", order.OrderID);

                        return Json(new { 
                            success = true, 
                            message = "Test callback successful",
                            redirectUrl = Url.Action("OrderSuccess", "Cart", new { orderId = order.OrderID }),
                            orderStatus = orderStatusId,
                            paymentStatus = payment.PaymentStatus
                        });
                    }
                    else
                    {
                        return Json(new { success = false, message = "Test callback with failure response" });
                    }
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    return Json(new { error = "Transaction failed: " + ex.Message });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error in TestCallback: {ex.Message}");
                return Json(new { error = ex.Message });
            }
        }

        // GET: /VNPay/TestUrl - Test VNPay URL generation
        [HttpGet]
        public IActionResult TestUrl(string orderCode = "TEST123", decimal amount = 100000)
        {
            try
            {
                Console.WriteLine($"🧪 TestUrl called - OrderCode: {orderCode}, Amount: {amount}");
                
                var vnPayRequest = new VNPayRequestModel
                {
                    OrderCode = orderCode,
                    OrderDescription = $"Test payment for order {orderCode}",
                    Amount = amount,
                    CreatedDate = DateTime.Now,
                    IpAddress = "127.0.0.1"
                };

                var paymentUrl = _vnPayService.CreatePaymentUrl(vnPayRequest);
                
                return Json(new
                {
                    success = true,
                    orderCode = orderCode,
                    amount = amount,
                    paymentUrl = paymentUrl,
                    urlLength = paymentUrl.Length,
                    returnUrl = paymentUrl.Contains("vnp_ReturnUrl") ? "✅ Contains ReturnUrl" : "❌ Missing ReturnUrl",
                    secureHash = paymentUrl.Contains("vnp_SecureHash") ? "✅ Contains SecureHash" : "❌ Missing SecureHash"
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error in TestUrl: {ex.Message}");
                return Json(new { error = ex.Message });
            }
        }

        // Helper methods
        private DateTime GetVietnamLocalTime()
        {
            var vietnamTimeZone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
            return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, vietnamTimeZone);
        }

        private async Task<int?> GetPaidOrderStatusIdAsync()
        {
            try
            {
                // After VNPay payment success, keep order in "Đã xác nhận" (ID=2)
                // If order is already "Chờ xác nhận" (ID=1), change to "Đã xác nhận" (ID=2)
                var paidStatus = await _context.OrderStatuses
                    .FirstOrDefaultAsync(os => os.StatusName.Contains("Đã xác nhận") || 
                                              os.StatusName.Contains("Paid") ||
                                              os.OrderStatusID == 2);
                
                var statusId = paidStatus?.OrderStatusID ?? 2;
                Console.WriteLine($"🔍 GetPaidOrderStatusIdAsync: Target status '{paidStatus?.StatusName}' with ID={statusId}");
                Console.WriteLine($"🔍 Logic: Chờ xác nhận (1) → Đã xác nhận (2) after VNPay success");
                return statusId;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error in GetPaidOrderStatusIdAsync: {ex.Message}");
                return 2; // Default fallback to "Đã xác nhận"
            }
        }

        private async Task<int?> GetCancelledOrderStatusIdAsync()
        {
            try
            {
                // Try to find "Hủy" status
                var cancelledStatus = await _context.OrderStatuses
                    .FirstOrDefaultAsync(os => os.StatusName.Contains("Hủy") || 
                                              os.StatusName.Contains("Cancelled") ||
                                              os.StatusName.Contains("Cancel"));
                
                return cancelledStatus?.OrderStatusID ?? 6; // Default to 6 if not found
            }
            catch
            {
                return 6; // Default fallback
            }
        }
    }
} 