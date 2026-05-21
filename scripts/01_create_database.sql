-- =============================================================
-- PasswordVaultDB – Full Setup Script
-- Run this on SQL Server 2019+ or Azure SQL
-- =============================================================

USE master;
GO

-- Create database if it doesn't exist
IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = 'PasswordVaultDB')
BEGIN
    CREATE DATABASE PasswordVaultDB
        COLLATE SQL_Latin1_General_CP1_CI_AS;
    PRINT 'Database PasswordVaultDB created.';
END
GO

USE PasswordVaultDB;
GO

-- ─── Users Table ────────────────────────────────────────────────────────────
IF OBJECT_ID('dbo.Accounts', 'U') IS NOT NULL DROP TABLE dbo.Accounts;
IF OBJECT_ID('dbo.Users',    'U') IS NOT NULL DROP TABLE dbo.Users;
GO

CREATE TABLE dbo.Users (
    UserId       INT IDENTITY(1,1)  PRIMARY KEY,
    FullName     NVARCHAR(200)      NOT NULL,
    Email        NVARCHAR(255)      NOT NULL,
    PasswordHash NVARCHAR(MAX)      NOT NULL,
    PasswordSalt NVARCHAR(MAX)      NOT NULL,
    CreatedDate  DATETIME2          NOT NULL DEFAULT GETDATE(),
    IsActive     BIT                NOT NULL DEFAULT 1,

    CONSTRAINT UQ_Users_Email UNIQUE (Email)
);
GO

-- ─── Accounts Table ─────────────────────────────────────────────────────────
CREATE TABLE dbo.Accounts (
    AccountId          INT IDENTITY(1,1)  PRIMARY KEY,
    UserId             INT                NOT NULL,
    AccountName        NVARCHAR(200)      NOT NULL,
    UserName           NVARCHAR(300)      NOT NULL,
    EncryptedPassword  NVARCHAR(MAX)      NOT NULL,
    EncryptedOtherInfo NVARCHAR(MAX)      NULL,
    Category           NVARCHAR(100)      NULL,
    WebsiteUrl         NVARCHAR(500)      NULL,
    EncryptionIV       NVARCHAR(200)      NOT NULL,
    CreatedDate        DATETIME2          NOT NULL DEFAULT GETDATE(),
    UpdatedDate        DATETIME2          NULL,

    CONSTRAINT FK_Accounts_Users
        FOREIGN KEY (UserId) REFERENCES dbo.Users(UserId)
        ON DELETE CASCADE
);
GO

CREATE INDEX IX_Accounts_UserId ON dbo.Accounts (UserId);
GO

-- ─── Audit Log Table (optional – for password reveal tracking) ───────────────
IF OBJECT_ID('dbo.AuditLogs', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.AuditLogs (
        LogId      INT IDENTITY(1,1)  PRIMARY KEY,
        UserId     INT                NOT NULL,
        AccountId  INT                NOT NULL,
        Action     NVARCHAR(100)      NOT NULL,
        IpAddress  NVARCHAR(64)       NULL,
        CreatedAt  DATETIME2          NOT NULL DEFAULT GETDATE()
    );
END
GO

-- ─── Verification ────────────────────────────────────────────────────────────
SELECT 'Setup complete.' AS Status;
SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE = 'BASE TABLE';
GO
