-- Create missing promotion-related tables
-- Run this script to create the tables that are missing

USE [JollibeeClone]
GO

-- Create PromotionProductScopes table if it doesn't exist
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='PromotionProductScopes' AND xtype='U')
BEGIN
    CREATE TABLE [PromotionProductScopes] (
        [PromotionID] int NOT NULL,
        [ProductID] int NOT NULL,
        CONSTRAINT [PK_PromotionProductScopes] PRIMARY KEY ([PromotionID], [ProductID]),
        CONSTRAINT [FK_PromotionProductScopes_Promotions_PromotionID] FOREIGN KEY ([PromotionID]) REFERENCES [Promotions] ([PromotionID]) ON DELETE CASCADE,
        CONSTRAINT [FK_PromotionProductScopes_Products_ProductID] FOREIGN KEY ([ProductID]) REFERENCES [Products] ([ProductID]) ON DELETE CASCADE
    );
    PRINT 'Created PromotionProductScopes table';
END
ELSE
BEGIN
    PRINT 'PromotionProductScopes table already exists';
END

-- Create PromotionCategoryScopes table if it doesn't exist
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='PromotionCategoryScopes' AND xtype='U')
BEGIN
    CREATE TABLE [PromotionCategoryScopes] (
        [PromotionID] int NOT NULL,
        [CategoryID] int NOT NULL,
        CONSTRAINT [PK_PromotionCategoryScopes] PRIMARY KEY ([PromotionID], [CategoryID]),
        CONSTRAINT [FK_PromotionCategoryScopes_Promotions_PromotionID] FOREIGN KEY ([PromotionID]) REFERENCES [Promotions] ([PromotionID]) ON DELETE CASCADE,
        CONSTRAINT [FK_PromotionCategoryScopes_Categories_CategoryID] FOREIGN KEY ([CategoryID]) REFERENCES [Categories] ([CategoryID]) ON DELETE CASCADE
    );
    PRINT 'Created PromotionCategoryScopes table';
END
ELSE
BEGIN
    PRINT 'PromotionCategoryScopes table already exists';
END

-- Create indexes for better performance
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_PromotionProductScopes_ProductID')
BEGIN
    CREATE INDEX [IX_PromotionProductScopes_ProductID] ON [PromotionProductScopes] ([ProductID]);
    PRINT 'Created index IX_PromotionProductScopes_ProductID';
END

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_PromotionCategoryScopes_CategoryID')
BEGIN
    CREATE INDEX [IX_PromotionCategoryScopes_CategoryID] ON [PromotionCategoryScopes] ([CategoryID]);
    PRINT 'Created index IX_PromotionCategoryScopes_CategoryID';
END

-- Mark the migration as applied to prevent future conflicts
IF NOT EXISTS (SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = '20250527030009_InitialCreate')
BEGIN
    -- Create the migrations history table if it doesn't exist
    IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='__EFMigrationsHistory' AND xtype='U')
    BEGIN
        CREATE TABLE [__EFMigrationsHistory] (
            [MigrationId] nvarchar(150) NOT NULL,
            [ProductVersion] nvarchar(32) NOT NULL,
            CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
        );
    END
    
    -- Insert the migration record
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES ('20250527030009_InitialCreate', '8.0.0');
    PRINT 'Marked migration as applied';
END

-- Verify tables exist
SELECT 'PromotionProductScopes' as TableName, COUNT(*) as RecordCount FROM PromotionProductScopes
UNION ALL
SELECT 'PromotionCategoryScopes' as TableName, COUNT(*) as RecordCount FROM PromotionCategoryScopes

PRINT 'Setup completed successfully!';
PRINT 'You can now run your application: dotnet run'; 