using JollibeeClone.Areas.Admin.Models;

namespace JollibeeClone.Areas.Admin.Services
{
    public interface IPromotionService
    {
        /// <summary>
        /// Kiểm tra xem user đã sử dụng voucher này chưa
        /// </summary>
        Task<bool> HasUserUsedPromotionAsync(int userId, int promotionId);

        /// <summary>
        /// Kiểm tra voucher có thể áp dụng cho user không
        /// </summary>
        Task<PromotionValidationResult> ValidatePromotionForUserAsync(int userId, string couponCode, decimal orderAmount);

        /// <summary>
        /// Áp dụng voucher cho user và order
        /// </summary>
        Task<UserPromotion> ApplyPromotionAsync(int userId, int promotionId, int? orderId, decimal discountAmount);

        /// <summary>
        /// Tính toán số tiền giảm giá
        /// </summary>
        decimal CalculateDiscountAmount(Promotion promotion, decimal orderAmount);

        /// <summary>
        /// Lấy danh sách voucher có thể sử dụng cho user
        /// </summary>
        Task<List<Promotion>> GetAvailablePromotionsForUserAsync(int userId);
    }

    public class PromotionValidationResult
    {
        public bool IsValid { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
        public Promotion? Promotion { get; set; }
        public decimal DiscountAmount { get; set; }
    }
} 