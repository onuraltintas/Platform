-- Seed: AuthorizationPolicies
-- Varsayilan tenant yok; tek tenant icin TenantId NULL birakildi

USE EgitimPlatform_Identity;
GO

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'AuthorizationPolicies')
BEGIN
    PRINT 'AuthorizationPolicies tablosu bulunamadi; migration calistiktan sonra tekrar deneyin.'
END
ELSE
BEGIN
    -- Users listeleme: Admin veya Support + Users.Read
    IF NOT EXISTS (SELECT 1 FROM AuthorizationPolicies WHERE MatchRegex = '^/users$')
    INSERT INTO AuthorizationPolicies (Id, MatchRegex, RequiredRolesJson, RequiredPermissionsJson, IsActive, TenantId, UpdatedAt, UpdatedBy)
    VALUES (NEWID(), '^/users$', '["Admin","Support"]', '["Users.Read"]', 1, NULL, SYSUTCDATETIME(), 'seed');

    -- Users create/edit: Admin + Users.Write
    IF NOT EXISTS (SELECT 1 FROM AuthorizationPolicies WHERE MatchRegex = '^/users/(new|[^/]+)$')
    INSERT INTO AuthorizationPolicies (Id, MatchRegex, RequiredRolesJson, RequiredPermissionsJson, IsActive, TenantId, UpdatedAt, UpdatedBy)
    VALUES (NEWID(), '^/users/(new|[^/]+)$', '["Admin"]', '["Users.Write"]', 1, NULL, SYSUTCDATETIME(), 'seed');

    -- Roles: Admin + Users.ManageRoles
    IF NOT EXISTS (SELECT 1 FROM AuthorizationPolicies WHERE MatchRegex = '^/roles(/.*)?$')
    INSERT INTO AuthorizationPolicies (Id, MatchRegex, RequiredRolesJson, RequiredPermissionsJson, IsActive, TenantId, UpdatedAt, UpdatedBy)
    VALUES (NEWID(), '^/roles(/.*)?$', '["Admin"]', '["Users.ManageRoles"]', 1, NULL, SYSUTCDATETIME(), 'seed');

    -- Permissions: Admin + Users.ManagePermissions
    IF NOT EXISTS (SELECT 1 FROM AuthorizationPolicies WHERE MatchRegex = '^/permissions(/.*)?$')
    INSERT INTO AuthorizationPolicies (Id, MatchRegex, RequiredRolesJson, RequiredPermissionsJson, IsActive, TenantId, UpdatedAt, UpdatedBy)
    VALUES (NEWID(), '^/permissions(/.*)?$', '["Admin"]', '["Users.ManagePermissions"]', 1, NULL, SYSUTCDATETIME(), 'seed');

    -- Categories: Admin + Content.ManageLibrary
    IF NOT EXISTS (SELECT 1 FROM AuthorizationPolicies WHERE MatchRegex = '^/categories(/.*)?$')
    INSERT INTO AuthorizationPolicies (Id, MatchRegex, RequiredRolesJson, RequiredPermissionsJson, IsActive, TenantId, UpdatedAt, UpdatedBy)
    VALUES (NEWID(), '^/categories(/.*)?$', '["Admin"]', '["Content.ManageLibrary"]', 1, NULL, SYSUTCDATETIME(), 'seed');
END

GO

-- Tenant bazli ornek (TENANT_01)
IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'AuthorizationPolicies')
BEGIN
    IF NOT EXISTS (SELECT 1 FROM AuthorizationPolicies WHERE MatchRegex = '^/reports(/.*)?$' AND TenantId = 'TENANT_01')
    INSERT INTO AuthorizationPolicies (Id, MatchRegex, RequiredRolesJson, RequiredPermissionsJson, IsActive, TenantId, UpdatedAt, UpdatedBy)
    VALUES (NEWID(), '^/reports(/.*)?$', '["Admin","Moderator","Support"]', '["Reports.View"]', 1, 'TENANT_01', SYSUTCDATETIME(), 'seed');
END

