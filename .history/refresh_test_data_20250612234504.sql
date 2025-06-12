-- Script để refresh test data cho UserPromotions
USE [Jollibee_DB];
GO

-- Xóa toàn bộ dữ liệu cũ
DELETE FROM UserPromotions;

-- Reset identity nếu cần
DBCC CHECKIDENT ('UserPromotions', RESEED, 0);

-- Lấy IDs có sẵn
DECLARE @FirstUserId INT, @SecondUserId INT, @FirstPromotionId INT, @SecondPromotionId INT;

SELECT TOP 1 @FirstUserId = UserID FROM Users ORDER BY UserID;
SELECT @SecondUserId = UserID FROM Users WHERE UserID > @FirstUserId ORDER BY UserID;
SELECT TOP 1 @FirstPromotionId = PromotionID FROM Promotions ORDER BY PromotionID;
SELECT @SecondPromotionId = PromotionID FROM Promotions WHERE PromotionID > @FirstPromotionId ORDER BY PromotionID;

PRINT 'FirstUserId: ' + CAST(@FirstUserId AS VARCHAR(10));
PRINT 'SecondUserId: ' + CAST(ISNULL(@SecondUserId, 0) AS VARCHAR(10));
PRINT 'FirstPromotionId: ' + CAST(@FirstPromotionId AS VARCHAR(10));
PRINT 'SecondPromotionId: ' + CAST(ISNULL(@SecondPromotionId, 0) AS VARCHAR(10));

-- Thêm test data mới
INSERT INTO UserPromotions (UserID, PromotionID, UsedDate, DiscountAmount, OrderID)
VALUES 
    (@FirstUserId, @FirstPromotionId, GETDATE(), 50000, NULL),
    (@FirstUserId, @FirstPromotionId, DATEADD(hour, -2, GETDATE()), 75000, NULL),
    (@FirstUserId, @FirstPromotionId, DATEADD(day, -1, GETDATE()), 30000, NULL);

-- Nếu có user thứ 2, thêm data cho user đó
IF @SecondUserId IS NOT NULL
BEGIN
    INSERT INTO UserPromotions (UserID, PromotionID, UsedDate, DiscountAmount, OrderID)
    VALUES 
        (@SecondUserId, @FirstPromotionId, DATEADD(hour, -3, GETDATE()), 40000, NULL),
        (@SecondUserId, @FirstPromotionId, DATEADD(day, -2, GETDATE()), 60000, NULL);
END

-- Nếu có promotion thứ 2, thêm data cho promotion đó
IF @SecondPromotionId IS NOT NULL
BEGIN
    INSERT INTO UserPromotions (UserID, PromotionID, UsedDate, DiscountAmount, OrderID)
    VALUES 
        (@FirstUserId, @SecondPromotionId, DATEADD(hour, -4, GETDATE()), 80000, NULL);
END

-- Cập nhật UsesCount
UPDATE Promotions 
SET UsesCount = (
    SELECT COUNT(*) 
    FROM UserPromotions 
    WHERE UserPromotions.PromotionID = Promotions.PromotionID
);

-- Hiển thị kết quả
SELECT 'Kết quả sau khi refresh:' as Info;
SELECT 
    up.UserPromotionID,
    u.FullName as UserName,
    u.Email,
    p.PromotionName,
    up.DiscountAmount,
    up.UsedDate,
    p.UsesCount as PromotionUsesCount
FROM UserPromotions up
INNER JOIN Users u ON up.UserID = u.UserID
INNER JOIN Promotions p ON up.PromotionID = p.PromotionID
ORDER BY up.UsedDate DESC;

-- Hiển thị tổng số record
SELECT 'Tổng số UserPromotions:' as Info, COUNT(*) as Total FROM UserPromotions;

GO 