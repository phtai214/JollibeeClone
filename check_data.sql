-- Kiểm tra dữ liệu UserPromotions
USE [Jollibee_DB];
GO

SELECT 'Tổng số UserPromotions:' as Info, COUNT(*) as Total FROM UserPromotions;

SELECT TOP 10 
    up.UserPromotionID,
    u.FullName as UserName,
    p.PromotionName,
    up.DiscountAmount,
    up.UsedDate
FROM UserPromotions up
INNER JOIN Users u ON up.UserID = u.UserID
INNER JOIN Promotions p ON up.PromotionID = p.PromotionID
ORDER BY up.UsedDate DESC;

-- Kiểm tra UsesCount của promotions
SELECT 
    p.PromotionID,
    p.PromotionName,
    p.UsesCount,
    COUNT(up.UserPromotionID) as ActualUsage
FROM Promotions p
LEFT JOIN UserPromotions up ON p.PromotionID = up.PromotionID
GROUP BY p.PromotionID, p.PromotionName, p.UsesCount
ORDER BY p.PromotionID;

GO 