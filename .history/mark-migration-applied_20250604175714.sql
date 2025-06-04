-- Mark the InitialCreate migration as applied
-- Run this in SQL Server Management Studio or via sqlcmd

USE [JollibeeClone]
GO

-- Check if __EFMigrationsHistory table exists
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='__EFMigrationsHistory' AND xtype='U')
BEGIN
    -- Create the migrations history table if it doesn't exist
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END

-- Check if the migration is already recorded
IF NOT EXISTS (SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = '20250527030009_InitialCreate')
BEGIN
    -- Insert the migration record to mark it as applied
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES ('20250527030009_InitialCreate', '8.0.0');
    
    PRINT 'Migration marked as applied successfully!';
END
ELSE
BEGIN
    PRINT 'Migration already marked as applied.';
END

-- Verify the result
SELECT * FROM [__EFMigrationsHistory]; 