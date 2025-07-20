using JollibeeClone.Data;
using JollibeeClone.Models;
using JollibeeClone.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace JollibeeClone.Services
{
    public class AutoVoucherService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<AutoVoucherService> _logger;

        // Mốc chi tiêu và phần trăm voucher tương ứng
        private readonly Dictionary<decimal, decimal> _rewardThresholds = new Dictionary<decimal, decimal>
        {
            { 500000m, 5m },   // 500k → 5%
            { 1000000m, 10m },  // 1M → 10%
            { 2000000m, 12m },  // 2M → 12%
            { 3000000m, 15m },  // 3M → 15%
            { 4000000m, 18m },  // 4M → 18%
            { 5000000m, 20m }   // 5M → 20%
        };

        public AutoVoucherService(AppDbContext context, ILogger<AutoVoucherService> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Tính tổng chi tiêu của user trong 7 ngày gần nhất từ các đơn hàng đã hoàn thành
        /// </summary>
        public async Task<UserSpendingInfo> CalculateUserSpendingAsync(int userId)
        {
            var fromDate = DateTime.Now.Date.AddDays(-7);
            var toDate = DateTime.Now.Date.AddDays(1).AddTicks(-1);

            try
            {
                // Status "Completed" thường có ID = 6, nhưng để chắc chắn ta tìm theo tên
                var completedStatusId = await _context.OrderStatuses
                    .Where(s => s.StatusName.ToLower().Contains("completed") || s.StatusName.ToLower().Contains("hoàn thành"))
                    .Select(s => s.OrderStatusID)
                    .FirstOrDefaultAsync();

                if (completedStatusId == 0)
                {
                    // Fallback: tìm status có ID = 6
                    completedStatusId = 6;
                }

                var userOrders = await _context.Orders
                    .Where(o => o.UserID == userId 
                           && o.OrderDate >= fromDate 
                           && o.OrderDate <= toDate
                           && o.OrderStatusID == completedStatusId)
                    .ToListAsync();

                var totalSpending = userOrders.Sum(o => o.TotalAmount);
                var ordersCount = userOrders.Count;

                _logger.LogInformation($"User {userId} spending calculation: {totalSpending:C} from {ordersCount} completed orders in last 7 days");

                return new UserSpendingInfo
                {
                    TotalSpending = totalSpending,
                    CompletedOrdersCount = ordersCount,
                    CalculatedFrom = fromDate,
                    CalculatedTo = toDate
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error calculating user spending for user {userId}");
                return new UserSpendingInfo();
            }
        }

        /// <summary>
        /// Kiểm tra và xử lý voucher reward cho user khi login
        /// </summary>
        public async Task<VoucherRewardViewModel> ProcessUserRewardAsync(int userId)
        {
            try
            {
                // Tính tổng chi tiêu trong 7 ngày
                var spendingInfo = await CalculateUserSpendingAsync(userId);
                var currentSpending = spendingInfo.TotalSpending;

                // Tạo thông tin các mốc reward
                var allThresholds = _rewardThresholds.Select(kvp => new VoucherThresholdInfo
                {
                    Threshold = kvp.Key,
                    Percentage = kvp.Value,
                    IsAchieved = currentSpending >= kvp.Key
                }).OrderBy(t => t.Threshold).ToList();

                // Tìm mốc tiếp theo mà user chưa đạt được
                var nextThreshold = allThresholds.FirstOrDefault(t => !t.IsAchieved);
                if (nextThreshold != null)
                {
                    nextThreshold.IsCurrentTarget = true;
                }

                // Tìm mốc cao nhất mà user đã đạt được
                var achievedThreshold = allThresholds.LastOrDefault(t => t.IsAchieved);
                
                var result = new VoucherRewardViewModel
                {
                    CurrentSpending = currentSpending,
                    AllThresholds = allThresholds
                };

                if (achievedThreshold != null)
                {
                    // User đã đạt ít nhất 1 mốc
                    result.AchievedThreshold = achievedThreshold.Threshold;
                    result.VoucherPercentage = achievedThreshold.Percentage;

                    // Kiểm tra xem user đã claim voucher cho mốc này chưa
                    var existingProgress = await _context.UserRewardProgresses
                        .FirstOrDefaultAsync(p => p.UserID == userId 
                                               && p.RewardThreshold == achievedThreshold.Threshold);

                    if (existingProgress == null || !existingProgress.VoucherClaimed)
                    {
                        // User chưa claim voucher cho mốc này → tạo voucher mới
                        var newVoucher = await GenerateAutoVoucherAsync(userId, achievedThreshold.Threshold, achievedThreshold.Percentage);
                        
                        if (newVoucher != null)
                        {
                            result.IsNewRewardAchieved = true;
                            result.GeneratedPromotionID = newVoucher.PromotionID;
                            result.VoucherCode = newVoucher.CouponCode ?? "";
                            result.StatusMessage = $"🎉 Chúc mừng! Bạn đã đạt mốc chi tiêu {achievedThreshold.Threshold:N0}₫";
                            result.EncouragementMessage = $"Bạn nhận được voucher giảm {achievedThreshold.Percentage}% cho đơn hàng tiếp theo!";

                            // Cập nhật progress
                            await UpdateUserRewardProgressAsync(userId, achievedThreshold.Threshold, currentSpending, newVoucher.PromotionID);
                        }
                    }
                    else if (nextThreshold != null)
                    {
                        // User đã claim voucher cho mốc hiện tại, hiển thị tiến trình tới mốc tiếp theo
                        result.NextThreshold = nextThreshold.Threshold;
                        result.RequiredAmount = nextThreshold.Threshold - currentSpending;
                        result.ProgressPercentage = (int)((currentSpending / nextThreshold.Threshold) * 100);
                        result.ProgressBarClass = result.ProgressPercentage >= 80 ? "bg-success" : 
                                                result.ProgressPercentage >= 50 ? "bg-warning" : "bg-info";
                        result.StatusMessage = $"Tiến trình tích lũy: {currentSpending:N0}₫ / {nextThreshold.Threshold:N0}₫";
                        result.EncouragementMessage = $"Bạn cần mua thêm {result.RequiredAmount:N0}₫ để nhận voucher {nextThreshold.Percentage}%!";
                        result.HasRewardEligible = true;
                    }
                }
                else if (nextThreshold != null)
                {
                    // User chưa đạt mốc nào, hiển thị tiến trình tới mốc đầu tiên
                    result.NextThreshold = nextThreshold.Threshold;
                    result.RequiredAmount = nextThreshold.Threshold - currentSpending;
                    result.ProgressPercentage = (int)((currentSpending / nextThreshold.Threshold) * 100);
                    result.ProgressBarClass = result.ProgressPercentage >= 80 ? "bg-success" : 
                                            result.ProgressPercentage >= 50 ? "bg-warning" : "bg-info";
                    result.StatusMessage = $"Tiến trình tích lũy: {currentSpending:N0}₫ / {nextThreshold.Threshold:N0}₫";
                    result.EncouragementMessage = $"Mua thêm {result.RequiredAmount:N0}₫ để nhận voucher {nextThreshold.Percentage}% đầu tiên!";
                    result.HasRewardEligible = true;
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error processing user reward for user {userId}");
                return new VoucherRewardViewModel();
            }
        }

        /// <summary>
        /// Tạo voucher tự động cho user
        /// </summary>
        private async Task<Promotion?> GenerateAutoVoucherAsync(int userId, decimal threshold, decimal percentage)
        {
            try
            {
                // Kiểm tra xem đã có voucher auto cho mốc này chưa
                var existingVoucher = await _context.Promotions
                    .FirstOrDefaultAsync(p => p.AutoVoucherGenerated == true 
                                           && p.RewardThreshold == threshold
                                           && p.UserPromotions.Any(up => up.UserID == userId));

                if (existingVoucher != null)
                {
                    _logger.LogInformation($"Auto voucher already exists for user {userId} at threshold {threshold}");
                    return existingVoucher;
                }

                // Tạo voucher mới
                var couponCode = GenerateUniqueCouponCode();
                var voucher = new Promotion
                {
                    PromotionName = $"Voucher Tích Lũy {percentage}%",
                    Description = $"Voucher tự động dành cho khách hàng đạt mốc chi tiêu {threshold:N0}₫ trong tuần",
                    CouponCode = couponCode,
                    DiscountType = "Percentage",
                    DiscountValue = percentage,
                    MinOrderValue = 0, // Không giới hạn đơn hàng tối thiểu
                    StartDate = DateTime.Now,
                    EndDate = DateTime.Now.AddDays(30), // Voucher có hiệu lực 30 ngày
                    MaxUses = null, // Không giới hạn tổng số lần sử dụng
                    MaxUsesPerUser = 1, // Mỗi user chỉ dùng 1 lần
                    UsesCount = 0,
                    IsActive = true,
                    AutoVoucherGenerated = true,
                    RewardThreshold = threshold
                };

                _context.Promotions.Add(voucher);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Generated auto voucher {couponCode} for user {userId} at threshold {threshold}");
                return voucher;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error generating auto voucher for user {userId} at threshold {threshold}");
                return null;
            }
        }

        /// <summary>
        /// Cập nhật hoặc tạo mới UserRewardProgress
        /// </summary>
        private async Task UpdateUserRewardProgressAsync(int userId, decimal threshold, decimal currentSpending, int promotionId)
        {
            try
            {
                var progress = await _context.UserRewardProgresses
                    .FirstOrDefaultAsync(p => p.UserID == userId && p.RewardThreshold == threshold);

                if (progress == null)
                {
                    progress = new UserRewardProgress
                    {
                        UserID = userId,
                        RewardThreshold = threshold,
                        CurrentSpending = currentSpending,
                        VoucherClaimed = true,
                        GeneratedPromotionID = promotionId,
                        VoucherClaimedDate = DateTime.Now
                    };
                    _context.UserRewardProgresses.Add(progress);
                }
                else
                {
                    progress.CurrentSpending = currentSpending;
                    progress.VoucherClaimed = true;
                    progress.GeneratedPromotionID = promotionId;
                    progress.VoucherClaimedDate = DateTime.Now;
                    progress.LastUpdatedDate = DateTime.Now;
                }

                await _context.SaveChangesAsync();
                _logger.LogInformation($"Updated reward progress for user {userId} at threshold {threshold}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating reward progress for user {userId} at threshold {threshold}");
            }
        }

        /// <summary>
        /// Sinh mã coupon duy nhất
        /// </summary>
        private string GenerateUniqueCouponCode()
        {
            var prefix = "AUTO";
            var timestamp = DateTime.Now.ToString("yyyyMMdd");
            var random = new Random().Next(1000, 9999);
            return $"{prefix}{timestamp}{random}";
        }

        /// <summary>
        /// Claim voucher khi user click nút "Nhận voucher"
        /// </summary>
        public async Task<bool> ClaimVoucherAsync(int userId, int promotionId)
        {
            try
            {
                var promotion = await _context.Promotions
                    .FirstOrDefaultAsync(p => p.PromotionID == promotionId && p.AutoVoucherGenerated == true);

                if (promotion == null)
                {
                    _logger.LogWarning($"Auto voucher {promotionId} not found for user {userId}");
                    return false;
                }

                // Kiểm tra xem user đã claim voucher này chưa
                var existingClaim = await _context.UserRewardProgresses
                    .FirstOrDefaultAsync(p => p.UserID == userId 
                                           && p.GeneratedPromotionID == promotionId 
                                           && p.VoucherClaimed == true);

                if (existingClaim != null)
                {
                    _logger.LogInformation($"User {userId} already claimed voucher {promotionId}");
                    return true; // Đã claim rồi
                }

                // Cập nhật trạng thái claim
                var progress = await _context.UserRewardProgresses
                    .FirstOrDefaultAsync(p => p.UserID == userId && p.RewardThreshold == promotion.RewardThreshold);

                if (progress != null)
                {
                    progress.VoucherClaimed = true;
                    progress.VoucherClaimedDate = DateTime.Now;
                    progress.LastUpdatedDate = DateTime.Now;
                    await _context.SaveChangesAsync();
                }

                _logger.LogInformation($"User {userId} successfully claimed voucher {promotionId}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error claiming voucher {promotionId} for user {userId}");
                return false;
            }
        }
    }
} 