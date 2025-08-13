-- Create databases for EgitimPlatform services
CREATE DATABASE EgitimPlatform_Identity;
GO

CREATE DATABASE EgitimPlatform_UserService;
GO

CREATE DATABASE EgitimPlatform_NotificationService;
GO

CREATE DATABASE EgitimPlatform_FeatureFlagService;
GO

-- Create login for application
IF NOT EXISTS (SELECT * FROM sys.server_principals WHERE name = 'EgitimPlatformUser')
BEGIN
    CREATE LOGIN EgitimPlatformUser WITH PASSWORD = '${DB_PASSWORD}';
END
GO

-- Grant permissions to databases
USE EgitimPlatform_Identity;
IF NOT EXISTS (SELECT * FROM sys.database_principals WHERE name = 'EgitimPlatformUser')
BEGIN
    CREATE USER EgitimPlatformUser FOR LOGIN EgitimPlatformUser;
    ALTER ROLE db_owner ADD MEMBER EgitimPlatformUser;
END
GO

USE EgitimPlatform_UserService;
IF NOT EXISTS (SELECT * FROM sys.database_principals WHERE name = 'EgitimPlatformUser')
BEGIN
    CREATE USER EgitimPlatformUser FOR LOGIN EgitimPlatformUser;
    ALTER ROLE db_owner ADD MEMBER EgitimPlatformUser;
END
GO

USE EgitimPlatform_NotificationService;
IF NOT EXISTS (SELECT * FROM sys.database_principals WHERE name = 'EgitimPlatformUser')
BEGIN
    CREATE USER EgitimPlatformUser FOR LOGIN EgitimPlatformUser;
    ALTER ROLE db_owner ADD MEMBER EgitimPlatformUser;
END
GO

USE EgitimPlatform_FeatureFlagService;
IF NOT EXISTS (SELECT * FROM sys.database_principals WHERE name = 'EgitimPlatformUser')
BEGIN
    CREATE USER EgitimPlatformUser FOR LOGIN EgitimPlatformUser;
    ALTER ROLE db_owner ADD MEMBER EgitimPlatformUser;
END
GO
