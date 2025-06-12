-- Script đơn giản để thêm test data cho UserPromotions
USE [Jollibee_DB];
GO

-- Lấy thông tin users và promotions có sẵn
SELECT 'Users có sẵn:' as Info;
SELECT UserID, FullName, Email FROM Users;

SELECT 'Promotions có sẵn:' as Info;
SELECT PromotionID, PromotionName, CouponCode FROM Promotions;

-- Thêm test data
DECLARE @FirstUserId INT, @FirstPromotionId INT;

SELECT TOP 1 @FirstUserId = UserID FROM Users ORDER BY UserID;
SELECT TOP 1 @FirstPromotionId = PromotionID FROM Promotions ORDER BY PromotionID;

-- Xóa test data cũ
DELETE FROM UserPromotions;

-- Thêm test data đơn giản
INSERT INTO UserPromotions (UserID, PromotionID, UsedDate, DiscountAmount, OrderID)
VALUES 
    (@FirstUserId, @FirstPromotionId, GETDATE(), 50000, NULL),
    (@FirstUserId, @FirstPromotionId, DATEADD(hour, -2, GETDATE()), 75000, NULL),
    (@FirstUserId, @FirstPromotionId, DATEADD(day, -1, GETDATE()), 30000, NULL);

-- Cập nhật UsesCount
UPDATE Promotions 
SET UsesCount = (
    SELECT COUNT(*) 
    FROM UserPromotions 
    WHERE UserPromotions.PromotionID = Promotions.PromotionID
);

-- Hiển thị kết quả
SELECT 'Kết quả test data:' as Info;
SELECT 
    up.UserPromotionID,
    u.FullName as UserName,
    u.Email,
    p.PromotionName,
    up.DiscountAmount,
    up.UsedDate
FROM UserPromotions up
INNER JOIN Users u ON up.UserID = u.UserID
INNER JOIN Promotions p ON up.PromotionID = p.PromotionID
ORDER BY up.UsedDate DESC;

GO 