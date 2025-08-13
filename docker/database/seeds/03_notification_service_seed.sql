-- Seed data for Notification Service
USE EgitimPlatform_NotificationService;
GO

-- Create NotificationTemplates table if not exists
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'NotificationTemplates')
BEGIN
    CREATE TABLE NotificationTemplates (
        Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
        Name NVARCHAR(100) NOT NULL UNIQUE,
        Type NVARCHAR(50) NOT NULL, -- Email, SMS, Push
        Subject NVARCHAR(200),
        Body NVARCHAR(MAX) NOT NULL,
        IsActive BIT DEFAULT 1,
        CreatedAt DATETIME2 DEFAULT GETUTCDATE(),
        UpdatedAt DATETIME2 DEFAULT GETUTCDATE()
    );
END
GO

-- Create UserDevices table if not exists
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'UserDevices')
BEGIN
    CREATE TABLE UserDevices (
        Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
        UserId UNIQUEIDENTIFIER NOT NULL,
        DeviceToken NVARCHAR(500) NOT NULL,
        DeviceType NVARCHAR(50) NOT NULL, -- iOS, Android, Web
        DeviceName NVARCHAR(200),
        IsActive BIT DEFAULT 1,
        CreatedAt DATETIME2 DEFAULT GETUTCDATE(),
        UpdatedAt DATETIME2 DEFAULT GETUTCDATE()
    );
END
GO

-- Create Notifications table if not exists
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Notifications')
BEGIN
    CREATE TABLE Notifications (
        Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
        UserId UNIQUEIDENTIFIER NOT NULL,
        Type NVARCHAR(50) NOT NULL, -- Email, SMS, Push
        Title NVARCHAR(200) NOT NULL,
        Message NVARCHAR(MAX) NOT NULL,
        Status NVARCHAR(20) DEFAULT 'Pending', -- Pending, Sent, Failed, Read
        SentAt DATETIME2,
        ReadAt DATETIME2,
        CreatedAt DATETIME2 DEFAULT GETUTCDATE(),
        UpdatedAt DATETIME2 DEFAULT GETUTCDATE()
    );
END
GO

-- Insert default notification templates
IF NOT EXISTS (SELECT * FROM NotificationTemplates WHERE Name = 'Welcome Email')
BEGIN
    INSERT INTO NotificationTemplates (Name, Type, Subject, Body)
    VALUES ('Welcome Email', 'Email', 'EgitimPlatform''a Hoş Geldiniz!', 
            '<h1>Hoş Geldiniz!</h1><p>EgitimPlatform''a kayıt olduğunuz için teşekkür ederiz.</p>');
END

IF NOT EXISTS (SELECT * FROM NotificationTemplates WHERE Name = 'Password Reset')
BEGIN
    INSERT INTO NotificationTemplates (Name, Type, Subject, Body)
    VALUES ('Password Reset', 'Email', 'Şifre Sıfırlama', 
            '<h1>Şifre Sıfırlama</h1><p>Şifrenizi sıfırlamak için aşağıdaki linke tıklayın: {{ResetLink}}</p>');
END

IF NOT EXISTS (SELECT * FROM NotificationTemplates WHERE Name = 'Course Reminder')
BEGIN
    INSERT INTO NotificationTemplates (Name, Type, Subject, Body)
    VALUES ('Course Reminder', 'Push', 'Ders Hatırlatması', 
            '{{CourseName}} dersiniz {{StartTime}} tarihinde başlayacak.');
END
GO 