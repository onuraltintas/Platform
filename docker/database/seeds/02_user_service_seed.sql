-- Seed data for User Service
USE EgitimPlatform_UserService;
GO

-- Create UserProfiles table if not exists
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'UserProfiles')
BEGIN
    CREATE TABLE UserProfiles (
        Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
        UserId UNIQUEIDENTIFIER NOT NULL UNIQUE,
        Avatar NVARCHAR(500),
        Bio NVARCHAR(1000),
        DateOfBirth DATE,
        Gender NVARCHAR(20),
        Address NVARCHAR(500),
        City NVARCHAR(100),
        Country NVARCHAR(100),
        PostalCode NVARCHAR(20),
        PhoneNumber NVARCHAR(20),
        Website NVARCHAR(500),
        SocialMediaLinks NVARCHAR(MAX),
        Preferences NVARCHAR(MAX),
        CreatedAt DATETIME2 DEFAULT GETUTCDATE(),
        UpdatedAt DATETIME2 DEFAULT GETUTCDATE()
    );
END
GO

-- Create UserSettings table if not exists
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'UserSettings')
BEGIN
    CREATE TABLE UserSettings (
        Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
        UserId UNIQUEIDENTIFIER NOT NULL UNIQUE,
        Theme NVARCHAR(50) DEFAULT 'light',
        Language NVARCHAR(10) DEFAULT 'tr',
        TimeZone NVARCHAR(100) DEFAULT 'Europe/Istanbul',
        EmailNotifications BIT DEFAULT 1,
        PushNotifications BIT DEFAULT 1,
        SMSNotifications BIT DEFAULT 0,
        PrivacyLevel NVARCHAR(20) DEFAULT 'public',
        CreatedAt DATETIME2 DEFAULT GETUTCDATE(),
        UpdatedAt DATETIME2 DEFAULT GETUTCDATE()
    );
END
GO

-- Create UserSessions table if not exists
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'UserSessions')
BEGIN
    CREATE TABLE UserSessions (
        Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
        UserId UNIQUEIDENTIFIER NOT NULL,
        SessionToken NVARCHAR(500) NOT NULL,
        RefreshToken NVARCHAR(500),
        DeviceInfo NVARCHAR(500),
        IPAddress NVARCHAR(45),
        UserAgent NVARCHAR(500),
        IsActive BIT DEFAULT 1,
        ExpiresAt DATETIME2 NOT NULL,
        CreatedAt DATETIME2 DEFAULT GETUTCDATE(),
        UpdatedAt DATETIME2 DEFAULT GETUTCDATE()
    );
END
GO 