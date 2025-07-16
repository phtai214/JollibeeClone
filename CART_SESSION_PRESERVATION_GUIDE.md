# Hướng Dẫn Test Cart Session Preservation

## Tổng Quan Tính Năng

Hệ thống đã được cập nhật để **tự động preserve cart session** khi user chuyển từ anonymous sang logged-in state:

- ✅ **Anonymous User**: Thêm sản phẩm vào cart, cart được lưu theo SessionID
- ✅ **Login/Register**: Cart anonymous được merge với cart user (nếu có)
- ✅ **Smart Merge**: Nếu user chưa có cart → transfer ownership, nếu có cart → merge items
- ✅ **Quantity Merge**: Items giống nhau được cộng dồn số lượng
- ✅ **Configuration Check**: Items được so sánh chính xác theo configuration

## Các File Đã Implement

### 1. **Services/CartMergeService.cs** (NEW)
- Service chuyên xử lý cart merge logic
- Method `MergeAnonymousCartToUserAsync(userId, sessionId)`
- Smart handling: Transfer ownership vs Merge items

### 2. **Program.cs**
- Register `ICartMergeService` vào DI container

### 3. **Controllers/AccountController.cs**  
- Inject `ICartMergeService` 
- Call cart merge sau khi login thành công
- Call cart merge sau khi register thành công

### 4. **Controllers/CartController.cs**
- Remove duplicate cart merge logic (moved to service)

## Test Scenarios

### 🧪 **Test Case 1: Anonymous → Login (No Existing User Cart)**

**Scenario**: User chưa từng có cart, login lần đầu

1. **Step 1**: Mở browser **chưa đăng nhập**
2. **Step 2**: Thêm **3-4 sản phẩm khác nhau** vào cart
3. **Step 3**: Verify cart có đúng items (check UI và console logs)
4. **Step 4**: **Login** với account chưa từng có cart
5. **Step 5**: Check cart sau login

**Expected Result**:
- ✅ Cart vẫn có đủ 3-4 items
- ✅ Cart ownership được transfer từ SessionID sang UserID
- ✅ Có thể proceed checkout bình thường

### 🧪 **Test Case 2: Anonymous → Login (Existing User Cart)**

**Scenario**: User đã có cart cũ, login và merge

1. **Setup**: Login trước, thêm **2 items**, logout
2. **Step 1**: Mở browser mới, **chưa đăng nhập**  
3. **Step 2**: Thêm **2 items khác** vào cart anonymous
4. **Step 3**: **Login** với account đã có cart cũ
5. **Step 4**: Check cart sau login

**Expected Result**:
- ✅ Cart có tổng **4 items** (2 cũ + 2 mới)
- ✅ Items được merge chính xác
- ✅ Anonymous cart bị xóa, chỉ còn user cart

### 🧪 **Test Case 3: Same Items Merge**

**Scenario**: Anonymous cart và user cart có items giống nhau

1. **Setup**: Login, thêm **"Burger A" x2**, logout
2. **Step 1**: Browser mới, thêm **"Burger A" x3** (cùng configuration)
3. **Step 2**: **Login** account đã có "Burger A" x2
4. **Step 3**: Check cart quantity

**Expected Result**:
- ✅ "Burger A" có quantity = **5** (2+3)
- ✅ Chỉ có 1 cart item cho "Burger A"
- ✅ Configuration được giữ nguyên

### 🧪 **Test Case 4: Complex Configuration Merge**

**Scenario**: Test với combo có nhiều options

1. **Setup**: Login, thêm **Combo A** với specific options, logout
2. **Step 1**: Browser mới, thêm **Combo A** với **different options**
3. **Step 2**: Thêm **Combo A** với **same options** như setup
4. **Step 3**: **Login**

**Expected Result**:
- ✅ Có 2 cart items cho Combo A (different configs)
- ✅ Items với same config được merge quantity
- ✅ Configuration snapshot được so sánh chính xác

### 🧪 **Test Case 5: Register Flow**

**Scenario**: User tạo account mới với cart

1. **Step 1**: Browser mới, thêm **3 items** vào cart
2. **Step 2**: **Register** account mới (không login)
3. **Step 3**: **Login** với account vừa tạo

**Expected Result**:
- ✅ Sau register: Cart vẫn có đủ items  
- ✅ Sau login: Cart được transfer ownership
- ✅ Flow hoạt động seamless

### 🧪 **Test Case 6: Authentication Flow from Cart**

**Scenario**: Test full flow từ cart checkout

1. **Step 1**: Thêm items vào cart, click "Thanh Toán"
2. **Step 2**: Modal xuất hiện yêu cầu login
3. **Step 3**: Click "Đăng nhập ngay" 
4. **Step 4**: Login thành công
5. **Step 5**: Redirect về `/Cart/Shipping`

**Expected Result**:
- ✅ Cart items preserved qua authentication
- ✅ Redirect về shipping page
- ✅ Có thể proceed checkout

## Debug & Monitoring

### Console Logs để Track
```
🔄 MergeAnonymousCartToUserAsync - UserID: X, SessionID: Y
🛒 Found anonymous cart with N items  
🎯 No existing user cart - transferring ownership
🔀 User already has cart - merging N items
📝 Merging quantities: X + Y
✅ Successfully merged anonymous cart to user cart
```

### Database Tables Check
```sql
-- Check cart ownership
SELECT CartID, UserID, SessionID, CreatedDate FROM Carts;

-- Check cart items
SELECT ci.CartItemID, ci.CartID, ci.ProductID, p.ProductName, ci.Quantity 
FROM CartItems ci 
JOIN Products p ON ci.ProductID = p.ProductID;
```

## Troubleshooting

### ❌ **Cart Items Missing After Login**
- Check service registration trong `Program.cs`
- Verify `ICartMergeService` được inject
- Check console logs for merge errors

### ❌ **Duplicate Items Instead of Merge**
- Verify `SelectedConfigurationSnapshot` comparison
- Check JSON serialization consistency
- Debug configuration data

### ❌ **Session ID Issues**
- Ensure session middleware registered correctly
- Check session timeout settings
- Verify session ID consistency

---

## Technical Notes

### Cart Merge Logic
```csharp
// Transfer ownership nếu user chưa có cart
anonymousCart.UserID = userId;
anonymousCart.SessionID = null;

// Merge items nếu user đã có cart  
existingItem.Quantity += anonymousItem.Quantity;
```

### Safety Features
- Cart merge **không fail login** nếu có lỗi
- Anonymous cart **được cleanup** sau merge
- **Quantity validation** để tránh overflow

**🚀 Status**: ✅ **READY FOR COMPREHENSIVE TESTING**

All scenarios implemented and ready for end-to-end testing across different user flows. 