-- Seed data for Feature Flag Service
USE EgitimPlatform_FeatureFlagService;
GO

-- Create FeatureFlags table if not exists
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'FeatureFlags')
BEGIN
    CREATE TABLE FeatureFlags (
        Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
        Name NVARCHAR(100) NOT NULL UNIQUE,
        Description NVARCHAR(500),
        IsEnabled BIT DEFAULT 0,
        IsGlobal BIT DEFAULT 0,
        RolloutPercentage DECIMAL(5,2) DEFAULT 0,
        Rules NVARCHAR(MAX), -- JSON string for complex rules
        CreatedAt DATETIME2 DEFAULT GETUTCDATE(),
        UpdatedAt DATETIME2 DEFAULT GETUTCDATE()
    );
END
GO

-- Create FeatureFlagEnvironments table if not exists
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'FeatureFlagEnvironments')
BEGIN
    CREATE TABLE FeatureFlagEnvironments (
        Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
        FeatureFlagId UNIQUEIDENTIFIER NOT NULL,
        Environment NVARCHAR(50) NOT NULL, -- Development, Staging, Production
        IsEnabled BIT DEFAULT 0,
        RolloutPercentage DECIMAL(5,2) DEFAULT 0,
        Rules NVARCHAR(MAX),
        CreatedAt DATETIME2 DEFAULT GETUTCDATE(),
        UpdatedAt DATETIME2 DEFAULT GETUTCDATE(),
        FOREIGN KEY (FeatureFlagId) REFERENCES FeatureFlags(Id) ON DELETE CASCADE
    );
END
GO

-- Create FeatureFlagUsers table if not exists
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'FeatureFlagUsers')
BEGIN
    CREATE TABLE FeatureFlagUsers (
        Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
        FeatureFlagId UNIQUEIDENTIFIER NOT NULL,
        UserId UNIQUEIDENTIFIER NOT NULL,
        IsEnabled BIT DEFAULT 0,
        CreatedAt DATETIME2 DEFAULT GETUTCDATE(),
        UpdatedAt DATETIME2 DEFAULT GETUTCDATE(),
        FOREIGN KEY (FeatureFlagId) REFERENCES FeatureFlags(Id) ON DELETE CASCADE
    );
END
GO

-- Insert default feature flags
IF NOT EXISTS (SELECT * FROM FeatureFlags WHERE Name = 'NewUI')
BEGIN
    INSERT INTO FeatureFlags (Name, Description, IsEnabled, IsGlobal, RolloutPercentage)
    VALUES ('NewUI', 'Yeni kullanıcı arayüzü', 0, 1, 0);
END

IF NOT EXISTS (SELECT * FROM FeatureFlags WHERE Name = 'AdvancedSearch')
BEGIN
    INSERT INTO FeatureFlags (Name, Description, IsEnabled, IsGlobal, RolloutPercentage)
    VALUES ('AdvancedSearch', 'Gelişmiş arama özelliği', 1, 1, 50);
END

IF NOT EXISTS (SELECT * FROM FeatureFlags WHERE Name = 'VideoChat')
BEGIN
    INSERT INTO FeatureFlags (Name, Description, IsEnabled, IsGlobal, RolloutPercentage)
    VALUES ('VideoChat', 'Video sohbet özelliği', 0, 0, 0);
END

IF NOT EXISTS (SELECT * FROM FeatureFlags WHERE Name = 'DarkMode')
BEGIN
    INSERT INTO FeatureFlags (Name, Description, IsEnabled, IsGlobal, RolloutPercentage)
    VALUES ('DarkMode', 'Karanlık tema', 1, 1, 100);
END
GO

-- Insert environment-specific settings
IF NOT EXISTS (SELECT * FROM FeatureFlagEnvironments WHERE FeatureFlagId = (SELECT Id FROM FeatureFlags WHERE Name = 'NewUI') AND Environment = 'Development')
BEGIN
    INSERT INTO FeatureFlagEnvironments (FeatureFlagId, Environment, IsEnabled, RolloutPercentage)
    SELECT Id, 'Development', 1, 100 FROM FeatureFlags WHERE Name = 'NewUI';
END

IF NOT EXISTS (SELECT * FROM FeatureFlagEnvironments WHERE FeatureFlagId = (SELECT Id FROM FeatureFlags WHERE Name = 'AdvancedSearch') AND Environment = 'Development')
BEGIN
    INSERT INTO FeatureFlagEnvironments (FeatureFlagId, Environment, IsEnabled, RolloutPercentage)
    SELECT Id, 'Development', 1, 100 FROM FeatureFlags WHERE Name = 'AdvancedSearch';
END
GO 