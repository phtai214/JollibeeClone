# 🎫 Hệ Thống Voucher Jollibee - Hướng Dẫn Sử Dụng

## 📋 Tổng Quan

Hệ thống voucher mới đã được triển khai với khả năng:
- ✅ **Nhiều người có thể sử dụng cùng một voucher**
- ✅ **Mỗi người chỉ được sử dụng 1 lần**
- ✅ **Theo dõi lịch sử sử dụng voucher của từng user**
- ✅ **Validation đầy đủ (thời gian, số lượng, điều kiện)**

## 🗃️ Cấu Trúc Database

### Bảng UserPromotions (Mới)
```sql
CREATE TABLE [UserPromotions](
    [UserPromotionID] [int] IDENTITY(1,1) NOT NULL,
    [UserID] [int] NOT NULL,
    [PromotionID] [int] NOT NULL,
    [UsedDate] [datetime2](7) NOT NULL DEFAULT (GETDATE()),
    [DiscountAmount] [decimal](18,2) NOT NULL,
    [OrderID] [int] NULL,
    -- Unique constraint để ngăn duplicate usage
    CONSTRAINT [IX_UserPromotions_UserID_PromotionID] UNIQUE ([UserID], [PromotionID])
);
```

## 🔧 API Endpoints

### 1. Validate Voucher cho User
**POST** `/Admin/Promotion/ValidateVoucher`

```json
// Request
{
    "userId": 1,
    "couponCode": "DISCOUNT10",
    "orderAmount": 100000
}

// Response (Success)
{
    "success": true,
    "message": "",
    "discountAmount": 10000,
    "promotion": {
        "id": 1,
        "name": "Giảm 10%",
        "discountType": "Percentage",
        "discountValue": 10
    }
}

// Response (Error)
{
    "success": false,
    "message": "Bạn đã sử dụng voucher này rồi.",
    "discountAmount": 0,
    "promotion": null
}
```

### 2. Apply Voucher cho User
**POST** `/Admin/Promotion/ApplyVoucher`

```json
// Request
{
    "userId": 1,
    "promotionId": 1,
    "orderId": 100,
    "discountAmount": 10000
}

// Response (Success)
{
    "success": true,
    "message": "Voucher đã được áp dụng thành công",
    "userPromotionId": 1,
    "discountAmount": 10000,
    "usedDate": "2024-12-12T10:30:00"
}
```

### 3. Lấy Voucher khả dụng cho User
**GET** `/Admin/Promotion/AvailableForUser/{userId}`

```json
// Response
{
    "success": true,
    "vouchers": [
        {
            "id": 1,
            "name": "Giảm giá 10%",
            "description": "Giảm giá cho đơn hàng từ 50k",
            "couponCode": "DISCOUNT10",
            "discountType": "Percentage",
            "discountValue": 10,
            "minOrderValue": 50000,
            "startDate": "2024-01-01T00:00:00",
            "endDate": "2024-12-31T23:59:59",
            "maxUses": 1000,
            "usesCount": 150,
            "remainingUses": 850
        }
    ]
}
```

### 4. Kiểm tra User đã sử dụng Voucher chưa
**GET** `/Admin/Promotion/CheckUserUsage/{userId}/{promotionId}`

```json
// Response
{
    "success": true,
    "hasUsed": false,
    "message": "User chưa sử dụng voucher này"
}
```

### 5. Lịch sử sử dụng Voucher của User
**GET** `/Admin/Promotion/UserHistory/{userId}`

```json
// Response
{
    "success": true,
    "history": [
        {
            "id": 1,
            "promotionName": "Giảm giá 10%",
            "couponCode": "DISCOUNT10",
            "discountAmount": 10000,
            "usedDate": "2024-12-12T10:30:00",
            "orderCode": "ORD001",
            "orderId": 100
        }
    ]
}
```

## 💻 Service Methods

### IPromotionService Interface

```csharp
public interface IPromotionService
{
    // Kiểm tra user đã sử dụng voucher chưa
    Task<bool> HasUserUsedPromotionAsync(int userId, int promotionId);
    
    // Validate voucher cho user
    Task<PromotionValidationResult> ValidatePromotionForUserAsync(int userId, string couponCode, decimal orderAmount);
    
    // Apply voucher cho user
    Task<UserPromotion> ApplyPromotionAsync(int userId, int promotionId, int? orderId, decimal discountAmount);
    
    // Tính toán discount amount
    decimal CalculateDiscountAmount(Promotion promotion, decimal orderAmount);
    
    // Lấy voucher khả dụng cho user
    Task<List<Promotion>> GetAvailablePromotionsForUserAsync(int userId);
}
```

## 📝 Ví Dụ Sử Dụng

### 1. Trong Controller/Service khác

```csharp
public class OrderController : Controller
{
    private readonly IPromotionService _promotionService;
    
    [HttpPost]
    public async Task<IActionResult> CreateOrder(CreateOrderRequest request)
    {
        // 1. Validate voucher nếu có
        if (!string.IsNullOrEmpty(request.CouponCode))
        {
            var validation = await _promotionService.ValidatePromotionForUserAsync(
                request.UserId, request.CouponCode, request.TotalAmount);
                
            if (!validation.IsValid)
            {
                return BadRequest(validation.ErrorMessage);
            }
            
            // 2. Tạo order
            var order = CreateOrder(request);
            order.DiscountAmount = validation.DiscountAmount;
            order.PromotionID = validation.Promotion.PromotionID;
            
            await _context.SaveChangesAsync();
            
            // 3. Apply voucher
            await _promotionService.ApplyPromotionAsync(
                request.UserId, validation.Promotion.PromotionID, 
                order.OrderID, validation.DiscountAmount);
        }
        
        return Ok();
    }
}
```

### 2. Trong Frontend/JavaScript

```javascript
// Validate voucher khi user nhập mã
async function validateVoucher(couponCode, orderAmount) {
    const response = await fetch('/Admin/Promotion/ValidateVoucher', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
            userId: getCurrentUserId(),
            couponCode: couponCode,
            orderAmount: orderAmount
        })
    });
    
    const result = await response.json();
    
    if (result.success) {
        showDiscountInfo(result.discountAmount, result.promotion);
    } else {
        showErrorMessage(result.message);
    }
}

// Lấy voucher khả dụng cho user
async function loadAvailableVouchers(userId) {
    const response = await fetch(`/Admin/Promotion/AvailableForUser/${userId}`);
    const result = await response.json();
    
    if (result.success) {
        displayVouchers(result.vouchers);
    }
}
```

## 🔍 Validation Rules

### Kiểm tra áp dụng voucher:
1. ✅ **Voucher tồn tại và active**
2. ✅ **Trong thời gian hiệu lực** (StartDate ≤ now ≤ EndDate)
3. ✅ **Đơn hàng đạt giá trị tối thiểu** (MinOrderValue)
4. ✅ **Voucher còn lượt sử dụng** (UsesCount < MaxUses)
5. ✅ **User chưa sử dụng voucher này** (UserPromotions table)

### Khi apply voucher:
1. ✅ **Tạo record trong UserPromotions**
2. ✅ **Tăng UsesCount của Promotion**
3. ✅ **Liên kết với Order nếu có**

## 🚀 Testing

### Test Scenarios:

1. **User A sử dụng voucher DISCOUNT10 lần đầu** → ✅ Thành công
2. **User A sử dụng voucher DISCOUNT10 lần thứ 2** → ❌ "Bạn đã sử dụng voucher này rồi"
3. **User B sử dụng voucher DISCOUNT10 lần đầu** → ✅ Thành công (khác user)
4. **Voucher hết hạn** → ❌ "Voucher đã hết hạn"
5. **Đơn hàng không đủ điều kiện** → ❌ "Đơn hàng tối thiểu XXX để sử dụng voucher"

## 📊 Database Migration

Đã tạo bảng `UserPromotions` với:
- ✅ Primary Key: `UserPromotionID`
- ✅ Unique Index: `(UserID, PromotionID)` để ngăn duplicate
- ✅ Foreign Keys: Users, Promotions, Orders
- ✅ Migration history đã được cập nhật

## 🎯 Kết Luận

Hệ thống voucher mới đã đáp ứng được yêu cầu:
- **Nhiều người có thể dùng cùng voucher**
- **Mỗi người chỉ dùng 1 lần**
- **Theo dõi đầy đủ lịch sử sử dụng**
- **API đầy đủ cho frontend tích hợp**

Sẵn sàng để sử dụng trong production! 🎉 