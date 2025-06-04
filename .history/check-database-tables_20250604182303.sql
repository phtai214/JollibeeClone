-- Check tables in Jollibee_DB database
USE [Jollibee_DB]
GO

PRINT '=== Checking Jollibee_DB Database ==='
PRINT 'Current Database: ' + DB_NAME()
PRINT ''

-- Check if PromotionProductScopes table exists
IF OBJECT_ID('PromotionProductScopes', 'U') IS NOT NULL
    PRINT '✅ PromotionProductScopes table EXISTS'
ELSE
    PRINT '❌ PromotionProductScopes table NOT FOUND'

-- Check if PromotionCategoryScopes table exists  
IF OBJECT_ID('PromotionCategoryScopes', 'U') IS NOT NULL
    PRINT '✅ PromotionCategoryScopes table EXISTS'
ELSE
    PRINT '❌ PromotionCategoryScopes table NOT FOUND'

-- Check if Promotions table exists
IF OBJECT_ID('Promotions', 'U') IS NOT NULL
    PRINT '✅ Promotions table EXISTS'
ELSE
    PRINT '❌ Promotions table NOT FOUND'

-- Check if Products table exists
IF OBJECT_ID('Products', 'U') IS NOT NULL
    PRINT '✅ Products table EXISTS'
ELSE
    PRINT '❌ Products table NOT FOUND'

-- Check if Categories table exists
IF OBJECT_ID('Categories', 'U') IS NOT NULL
    PRINT '✅ Categories table EXISTS'
ELSE
    PRINT '❌ Categories table NOT FOUND'

PRINT ''
PRINT '=== All Tables in Database ==='
SELECT TABLE_NAME, TABLE_TYPE 
FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_TYPE = 'BASE TABLE'
ORDER BY TABLE_NAME

PRINT ''
PRINT '=== Migration History ==='
IF OBJECT_ID('__EFMigrationsHistory', 'U') IS NOT NULL
BEGIN
    SELECT MigrationId, ProductVersion 
    FROM __EFMigrationsHistory 
    ORDER BY MigrationId
END
ELSE
    PRINT '❌ __EFMigrationsHistory table NOT FOUND' 