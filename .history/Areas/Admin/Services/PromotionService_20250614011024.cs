using JollibeeClone.Data;
using JollibeeClone.Models;
using Microsoft.EntityFrameworkCore;

namespace JollibeeClone.Areas.Admin.Services
{
    public class PromotionService : IPromotionService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<PromotionService> _logger;

        public PromotionService(AppDbContext context, ILogger<PromotionService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<bool> HasUserUsedPromotionAsync(int userId, int promotionId)
        {
            try
            {
                var hasUsed = await _context.UserPromotions
                    .AnyAsync(up => up.UserID == userId && up.PromotionID == promotionId);
                
                _logger.LogInformation("User {UserId} has used promotion {PromotionId}: {HasUsed}", 
                    userId, promotionId, hasUsed);
                
                return hasUsed;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if user {UserId} used promotion {PromotionId}", 
                    userId, promotionId);
                return true; // Safe side - assume used to prevent abuse
            }
        }

        public async Task<PromotionValidationResult> ValidatePromotionForUserAsync(int userId, string couponCode, decimal orderAmount)
        {
            try
            {
                _logger.LogInformation("Validating promotion for user {UserId}, coupon: {CouponCode}, amount: {Amount}", 
                    userId, couponCode, orderAmount);

                var promotion = await _context.Promotions
                    .FirstOrDefaultAsync(p => p.CouponCode == couponCode && p.IsActive);

                if (promotion == null)
                {
                    return new PromotionValidationResult
                    {
                        IsValid = false,
                        ErrorMessage = "Mã voucher không tồn tại hoặc đã bị vô hiệu hóa."
                    };
                }

                // Kiểm tra thời gian
                var now = DateTime.Now;
                if (now < promotion.StartDate)
                {
                    return new PromotionValidationResult
                    {
                        IsValid = false,
                        ErrorMessage = "Voucher chưa có hiệu lực."
                    };
                }

                if (now > promotion.EndDate)
                {
                    return new PromotionValidationResult
                    {
                        IsValid = false,
                        ErrorMessage = "Voucher đã hết hạn."
                    };
                }

                // Kiểm tra giá trị đơn hàng tối thiểu
                if (promotion.MinOrderValue.HasValue && orderAmount < promotion.MinOrderValue.Value)
                {
                    return new PromotionValidationResult
                    {
                        IsValid = false,
                        ErrorMessage = $"Đơn hàng tối thiểu {promotion.MinOrderValue.Value:C} để sử dụng voucher này."
                    };
                }

                // Kiểm tra số lần sử dụng tối đa
                if (promotion.MaxUses.HasValue && promotion.UsesCount >= promotion.MaxUses.Value)
                {
                    return new PromotionValidationResult
                    {
                        IsValid = false,
                        ErrorMessage = "Voucher đã hết lượt sử dụng."
                    };
                }

                // Kiểm tra user đã sử dụng voucher này chưa
                var hasUserUsed = await HasUserUsedPromotionAsync(userId, promotion.PromotionID);
                if (hasUserUsed)
                {
                    return new PromotionValidationResult
                    {
                        IsValid = false,
                        ErrorMessage = "Bạn đã sử dụng voucher này rồi."
                    };
                }

                // Tính toán số tiền giảm giá
                var discountAmount = CalculateDiscountAmount(promotion, orderAmount);

                return new PromotionValidationResult
                {
                    IsValid = true,
                    Promotion = promotion,
                    DiscountAmount = discountAmount
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating promotion for user {UserId}, coupon: {CouponCode}", 
                    userId, couponCode);
                
                return new PromotionValidationResult
                {
                    IsValid = false,
                    ErrorMessage = "Có lỗi xảy ra khi kiểm tra voucher."
                };
            }
        }

        public async Task<UserPromotion> ApplyPromotionAsync(int userId, int promotionId, int? orderId, decimal discountAmount)
        {
            try
            {
                _logger.LogInformation("Applying promotion {PromotionId} for user {UserId}, order {OrderId}, discount: {DiscountAmount}", 
                    promotionId, userId, orderId, discountAmount);

                // Kiểm tra lại user chưa sử dụng voucher này
                var hasUsed = await HasUserUsedPromotionAsync(userId, promotionId);
                if (hasUsed)
                {
                    throw new InvalidOperationException("User đã sử dụng voucher này rồi.");
                }

                // Tạo record UserPromotion
                var userPromotion = new UserPromotion
                {
                    UserID = userId,
                    PromotionID = promotionId,
                    UsedDate = DateTime.Now,
                    DiscountAmount = discountAmount,
                    OrderID = orderId
                };

                _context.UserPromotions.Add(userPromotion);

                // Cập nhật UsesCount của promotion
                var promotion = await _context.Promotions.FindAsync(promotionId);
                if (promotion != null)
                {
                    promotion.UsesCount++;
                    _context.Promotions.Update(promotion);
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation("Successfully applied promotion {PromotionId} for user {UserId}", 
                    promotionId, userId);

                return userPromotion;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error applying promotion {PromotionId} for user {UserId}", 
                    promotionId, userId);
                throw;
            }
        }

        public decimal CalculateDiscountAmount(Promotion promotion, decimal orderAmount)
        {
            decimal discountAmount = 0;

            switch (promotion.DiscountType.ToLower())
            {
                case "percentage":
                    discountAmount = orderAmount * (promotion.DiscountValue / 100);
                    break;
                case "fixed":
                case "amount":
                    discountAmount = promotion.DiscountValue;
                    break;
                default:
                    _logger.LogWarning("Unknown discount type: {DiscountType}", promotion.DiscountType);
                    break;
            }

            // Đảm bảo discount không vượt quá giá trị đơn hàng
            return Math.Min(discountAmount, orderAmount);
        }

        public async Task<List<Promotion>> GetAvailablePromotionsForUserAsync(int userId)
        {
            try
            {
                var now = DateTime.Now;
                
                // Lấy danh sách promotion mà user chưa sử dụng
                var usedPromotionIds = await _context.UserPromotions
                    .Where(up => up.UserID == userId)
                    .Select(up => up.PromotionID)
                    .ToListAsync();

                var availablePromotions = await _context.Promotions
                    .Where(p => p.IsActive && 
                                p.StartDate <= now && 
                                p.EndDate >= now &&
                                !usedPromotionIds.Contains(p.PromotionID) &&
                                (!p.MaxUses.HasValue || p.UsesCount < p.MaxUses.Value))
                    .OrderBy(p => p.MinOrderValue ?? 0)
                    .ToListAsync();

                return availablePromotions;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting available promotions for user {UserId}", userId);
                return new List<Promotion>();
            }
        }
    }
} 