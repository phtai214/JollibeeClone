-- Script để thêm test data cho UserPromotions
USE [Jollibee_DB];
GO

-- Kiểm tra xem có data trong các bảng liên quan không
DECLARE @UserCount INT, @PromotionCount INT, @OrderCount INT;

SELECT @UserCount = COUNT(*) FROM Users;
SELECT @PromotionCount = COUNT(*) FROM Promotions;
SELECT @OrderCount = COUNT(*) FROM Orders;

PRINT 'Users: ' + CAST(@UserCount AS VARCHAR(10));
PRINT 'Promotions: ' + CAST(@PromotionCount AS VARCHAR(10));
PRINT 'Orders: ' + CAST(@OrderCount AS VARCHAR(10));

-- Chỉ thêm test data nếu có users và promotions
IF @UserCount > 0 AND @PromotionCount > 0
BEGIN
    -- Tạo test data cho UserPromotions
    DECLARE @FirstUserId INT, @FirstPromotionId INT, @FirstOrderId INT;
    
    SELECT TOP 1 @FirstUserId = UserID FROM Users ORDER BY UserID;
    SELECT TOP 1 @FirstPromotionId = PromotionID FROM Promotions ORDER BY PromotionID;
    SELECT TOP 1 @FirstOrderId = OrderID FROM Orders ORDER BY OrderID;
    
    -- Xóa test data cũ nếu có
    DELETE FROM UserPromotions WHERE UserPromotionID < 1000; -- Giả sử test data có ID < 1000
    
    -- Thêm test data
    INSERT INTO UserPromotions (UserID, PromotionID, UsedDate, DiscountAmount, OrderID)
    VALUES 
        (@FirstUserId, @FirstPromotionId, GETDATE(), 50000, @FirstOrderId),
        (@FirstUserId + 1, @FirstPromotionId, DATEADD(hour, -2, GETDATE()), 75000, NULL),
        (@FirstUserId + 2, @FirstPromotionId, DATEADD(day, -1, GETDATE()), 30000, @FirstOrderId + 1),
        (@FirstUserId, @FirstPromotionId + 1, DATEADD(day, -3, GETDATE()), 100000, NULL),
        (@FirstUserId + 3, @FirstPromotionId, DATEADD(hour, -5, GETDATE()), 25000, @FirstOrderId + 2);
    
    -- Cập nhật UsesCount trong bảng Promotions
    UPDATE Promotions 
    SET UsesCount = (
        SELECT COUNT(*) 
        FROM UserPromotions 
        WHERE UserPromotions.PromotionID = Promotions.PromotionID
    );
    
    PRINT 'Test data đã được thêm thành công!';
    
    -- Hiển thị kết quả
    SELECT 
        up.UserPromotionID,
        u.FullName as UserName,
        u.Email,
        p.PromotionName,
        up.DiscountAmount,
        up.UsedDate,
        o.OrderCode
    FROM UserPromotions up
    INNER JOIN Users u ON up.UserID = u.UserID
    INNER JOIN Promotions p ON up.PromotionID = p.PromotionID
    LEFT JOIN Orders o ON up.OrderID = o.OrderID
    ORDER BY up.UsedDate DESC;
END
ELSE
BEGIN
    PRINT 'Không thể thêm test data vì thiếu Users hoặc Promotions!';
    PRINT 'Hãy đảm bảo có ít nhất 1 user và 1 promotion trong database.';
END

GO 