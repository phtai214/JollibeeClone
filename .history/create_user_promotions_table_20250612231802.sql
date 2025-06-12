-- Script để tạo bảng UserPromotions
USE [Jollibee_DB];
GO

-- Kiểm tra xem bảng đã tồn tại chưa
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[UserPromotions]') AND type in (N'U'))
BEGIN
    -- Tạo bảng UserPromotions
    CREATE TABLE [dbo].[UserPromotions](
        [UserPromotionID] [int] IDENTITY(1,1) NOT NULL,
        [UserID] [int] NOT NULL,
        [PromotionID] [int] NOT NULL,
        [UsedDate] [datetime2](7) NOT NULL DEFAULT (GETDATE()),
        [DiscountAmount] [decimal](18,2) NOT NULL,
        [OrderID] [int] NULL,
        CONSTRAINT [PK_UserPromotions] PRIMARY KEY CLUSTERED ([UserPromotionID] ASC)
    );

    -- Tạo unique index để ngăn duplicate usage
    CREATE UNIQUE NONCLUSTERED INDEX [IX_UserPromotions_UserID_PromotionID] 
    ON [dbo].[UserPromotions] ([UserID] ASC, [PromotionID] ASC);

    -- Tạo các index khác
    CREATE NONCLUSTERED INDEX [IX_UserPromotions_OrderID] 
    ON [dbo].[UserPromotions] ([OrderID] ASC);

    CREATE NONCLUSTERED INDEX [IX_UserPromotions_PromotionID] 
    ON [dbo].[UserPromotions] ([PromotionID] ASC);

    -- Thêm foreign key constraints
    ALTER TABLE [dbo].[UserPromotions] 
    ADD CONSTRAINT [FK_UserPromotions_Users_UserID] 
    FOREIGN KEY([UserID]) REFERENCES [dbo].[Users] ([UserID]) ON DELETE CASCADE;

    ALTER TABLE [dbo].[UserPromotions] 
    ADD CONSTRAINT [FK_UserPromotions_Promotions_PromotionID] 
    FOREIGN KEY([PromotionID]) REFERENCES [dbo].[Promotions] ([PromotionID]) ON DELETE CASCADE;

    ALTER TABLE [dbo].[UserPromotions] 
    ADD CONSTRAINT [FK_UserPromotions_Orders_OrderID] 
    FOREIGN KEY([OrderID]) REFERENCES [dbo].[Orders] ([OrderID]) ON DELETE SET NULL;

    PRINT 'Bảng UserPromotions đã được tạo thành công!';
END
ELSE
BEGIN
    PRINT 'Bảng UserPromotions đã tồn tại!';
END

-- Thêm record vào migration history nếu cần
IF NOT EXISTS (SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = '20250527030009_InitialCreate')
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion]) 
    VALUES ('20250527030009_InitialCreate', '7.0.13');
    PRINT 'Đã thêm InitialCreate vào migration history!';
END

IF NOT EXISTS (SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = '20250612161239_AddUserPromotionTableOnly')
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion]) 
    VALUES ('20250612161239_AddUserPromotionTableOnly', '7.0.13');
    PRINT 'Đã thêm AddUserPromotionTableOnly vào migration history!';
END

GO 