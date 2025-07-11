using JollibeeClone.Data;
using JollibeeClone.Models;
using Microsoft.EntityFrameworkCore;

namespace JollibeeClone.Services
{
    public class ShippingService
    {
        private readonly AppDbContext _context;

        public ShippingService(AppDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Tính phí giao hàng dựa trên logic Jollibee:
        /// - Đơn đầu tiên của khách mới: FREE SHIP cho đơn từ 60k trở lên
        /// - Các đơn tiếp theo: Phí ship 10k cho đơn từ 60k trở lên
        /// - Dưới 60k: Phí ship 15k
        /// </summary>
        public async Task<ShippingCalculationResult> CalculateShippingFeeAsync(int? userId, decimal orderAmount, int deliveryMethodId)
        {
            var result = new ShippingCalculationResult();

            // Chỉ áp dụng cho giao hàng tận nơi (DeliveryMethodID = 1)
            if (deliveryMethodId != 1)
            {
                result.ShippingFee = 0;
                result.IsFreeship = false;
                result.FreeshipeReason = "";
                result.Message = "Miễn phí - Nhận tại cửa hàng";
                return result;
            }

            // Kiểm tra điều kiện minimum order
            const decimal MIN_ORDER_FOR_REDUCED_SHIPPING = 60000m;
            const decimal REGULAR_SHIPPING_FEE = 10000m;
            const decimal SMALL_ORDER_SHIPPING_FEE = 15000m;

            if (orderAmount < MIN_ORDER_FOR_REDUCED_SHIPPING)
            {
                // Đơn hàng dưới 60k - phí ship 15k
                result.ShippingFee = SMALL_ORDER_SHIPPING_FEE;
                result.IsFreeship = false;
                result.FreeshipeReason = "";
                result.Message = $"Phí giao hàng {SMALL_ORDER_SHIPPING_FEE:N0}₫";
                result.RequiredAmountForFreeship = MIN_ORDER_FOR_REDUCED_SHIPPING - orderAmount;
                return result;
            }

            // Đơn hàng từ 60k trở lên
            if (!userId.HasValue)
            {
                // Guest user - áp dụng phí ship thường
                result.ShippingFee = REGULAR_SHIPPING_FEE;
                result.IsFreeship = false;
                result.FreeshipeReason = "";
                result.Message = $"Phí giao hàng {REGULAR_SHIPPING_FEE:N0}₫";
                return result;
            }

            // Kiểm tra xem đây có phải đơn hàng đầu tiên của user không
            var previousOrdersCount = await _context.Orders
                .Where(o => o.UserID == userId.Value && o.OrderStatusID != 7) // Loại trừ đơn đã hủy
                .CountAsync();

            if (previousOrdersCount == 0)
            {
                // Đây là đơn hàng đầu tiên - FREESHIP!
                result.ShippingFee = 0;
                result.IsFreeship = true;
                result.FreeshipeReason = "first_order";
                result.Message = "🎉 MIỄN PHÍ GIAO HÀNG - Đơn hàng đầu tiên!";
                return result;
            }
            else
            {
                // Các đơn hàng tiếp theo - phí ship 10k
                result.ShippingFee = REGULAR_SHIPPING_FEE;
                result.IsFreeship = false;
                result.FreeshipeReason = "";
                result.Message = $"Phí giao hàng {REGULAR_SHIPPING_FEE:N0}₫";
                return result;
            }
        }

        /// <summary>
        /// Lấy thông tin freeship cho user để hiển thị khuyến khích
        /// </summary>
        public async Task<FreeshipePromotionInfo> GetFreeshipePromotionInfoAsync(int? userId, decimal currentOrderAmount)
        {
            var info = new FreeshipePromotionInfo();

            if (!userId.HasValue)
            {
                // Guest user
                info.IsEligibleForFirstOrderFreeship = false;
                info.IsFirstOrder = false;
                info.RequiredAmountForFreeship = Math.Max(0, 60000m - currentOrderAmount);
                info.Message = currentOrderAmount >= 60000m 
                    ? "Đăng ký tài khoản để được FREESHIP đơn đầu tiên!" 
                    : $"Mua thêm {info.RequiredAmountForFreeship:N0}₫ để giảm phí ship!";
                return info;
            }

            // Kiểm tra đơn hàng trước đó
            var previousOrdersCount = await _context.Orders
                .Where(o => o.UserID == userId.Value && o.OrderStatusID != 7)
                .CountAsync();

            info.IsFirstOrder = previousOrdersCount == 0;
            info.IsEligibleForFirstOrderFreeship = info.IsFirstOrder;

            if (info.IsFirstOrder)
            {
                if (currentOrderAmount >= 60000m)
                {
                    info.Message = "🎉 Chúc mừng! Bạn được FREESHIP đơn hàng đầu tiên!";
                    info.RequiredAmountForFreeship = 0;
                }
                else
                {
                    info.RequiredAmountForFreeship = 60000m - currentOrderAmount;
                    info.Message = $"Mua thêm {info.RequiredAmountForFreeship:N0}₫ để được FREESHIP đơn đầu tiên! 🎉";
                }
            }
            else
            {
                info.RequiredAmountForFreeship = Math.Max(0, 60000m - currentOrderAmount);
                // BỎ LUÔN message "Phí giao hàng ưu đãi" để tránh confuse user
                info.Message = currentOrderAmount >= 60000m 
                    ? "" // Không hiển thị message gì hết khi đủ điều kiện
                    : $"Mua thêm {info.RequiredAmountForFreeship:N0}₫ để giảm phí ship!";
            }

            return info;
        }
    }

    public class ShippingCalculationResult
    {
        public decimal ShippingFee { get; set; }
        public bool IsFreeship { get; set; }
        public string FreeshipeReason { get; set; } = "";
        public string Message { get; set; } = "";
        public decimal RequiredAmountForFreeship { get; set; }
    }

    public class FreeshipePromotionInfo
    {
        public bool IsFirstOrder { get; set; }
        public bool IsEligibleForFirstOrderFreeship { get; set; }
        public decimal RequiredAmountForFreeship { get; set; }
        public string Message { get; set; } = "";
    }
} 