/**
 * Permission constants matching the backend PermissionConstants.cs
 * These should be kept in sync with the backend implementation
 */
export class PermissionConstants {
  // Identity Service Permissions
  static readonly IDENTITY = {
    USERS: {
      READ: 'Identity.Users.Read',
      CREATE: 'Identity.Users.Create',
      UPDATE: 'Identity.Users.Update',
      DELETE: 'Identity.Users.Delete',
      MANAGE_ROLES: 'Identity.Users.ManageRoles',
      MANAGE_PERMISSIONS: 'Identity.Users.ManagePermissions',
      VIEW_AUDIT: 'Identity.Users.ViewAudit',
      EXPORT: 'Identity.Users.Export',
      IMPORT: 'Identity.Users.Import'
    },
    ROLES: {
      READ: 'Identity.Roles.Read',
      CREATE: 'Identity.Roles.Create',
      UPDATE: 'Identity.Roles.Update',
      DELETE: 'Identity.Roles.Delete',
      MANAGE_PERMISSIONS: 'Identity.Roles.ManagePermissions',
      VIEW_AUDIT: 'Identity.Roles.ViewAudit'
    },
    GROUPS: {
      READ: 'Identity.Groups.Read',
      CREATE: 'Identity.Groups.Create',
      UPDATE: 'Identity.Groups.Update',
      DELETE: 'Identity.Groups.Delete',
      MANAGE_MEMBERS: 'Identity.Groups.ManageMembers',
      MANAGE_SERVICES: 'Identity.Groups.ManageServices'
    },
    PERMISSIONS: {
      READ: 'Identity.Permissions.Read',
      CREATE: 'Identity.Permissions.Create',
      UPDATE: 'Identity.Permissions.Update',
      DELETE: 'Identity.Permissions.Delete',
      AUDIT: 'Identity.Permissions.Audit'
    },
    AUDIT: {
      READ: 'Identity.Audit.Read',
      EXPORT: 'Identity.Audit.Export',
      DELETE: 'Identity.Audit.Delete'
    }
  };

  // System Administration
  static readonly SYSTEM = {
    ADMIN: {
      FULL_ACCESS: 'System.Admin.FullAccess',
      USER_MANAGEMENT: 'System.Admin.UserManagement',
      SYSTEM_CONFIG: 'System.Admin.SystemConfig',
      SECURITY_AUDIT: 'System.Admin.SecurityAudit'
    }
  };

  // Speed Reading Service Permissions
  static readonly SPEED_READING = {
    TEXTS: {
      READ: 'SpeedReading.Texts.Read',
      CREATE: 'SpeedReading.Texts.Create',
      UPDATE: 'SpeedReading.Texts.Update',
      DELETE: 'SpeedReading.Texts.Delete'
    },
    EXERCISES: {
      READ: 'SpeedReading.Exercises.Read',
      CREATE: 'SpeedReading.Exercises.Create',
      UPDATE: 'SpeedReading.Exercises.Update',
      DELETE: 'SpeedReading.Exercises.Delete'
    },
    ANALYTICS: {
      READ: 'SpeedReading.Analytics.Read',
      EXPORT: 'SpeedReading.Analytics.Export'
    }
  };

  // Wildcard permissions
  static readonly WILDCARDS = {
    SUPER_ADMIN: '*.*.*',
    IDENTITY_ALL: 'Identity.*.*',
    SPEED_READING_ALL: 'SpeedReading.*.*',
    SYSTEM_ALL: 'System.*.*'
  };

  // Common permission groups for UI
  static readonly GROUPS = {
    USER_MANAGEMENT: [
      PermissionConstants.IDENTITY.USERS.READ,
      PermissionConstants.IDENTITY.USERS.CREATE,
      PermissionConstants.IDENTITY.USERS.UPDATE,
      PermissionConstants.IDENTITY.USERS.DELETE
    ],
    ROLE_MANAGEMENT: [
      PermissionConstants.IDENTITY.ROLES.READ,
      PermissionConstants.IDENTITY.ROLES.CREATE,
      PermissionConstants.IDENTITY.ROLES.UPDATE,
      PermissionConstants.IDENTITY.ROLES.DELETE
    ],
    GROUP_MANAGEMENT: [
      PermissionConstants.IDENTITY.GROUPS.READ,
      PermissionConstants.IDENTITY.GROUPS.CREATE,
      PermissionConstants.IDENTITY.GROUPS.UPDATE,
      PermissionConstants.IDENTITY.GROUPS.DELETE
    ],
    ADMIN_ACTIONS: [
      PermissionConstants.IDENTITY.USERS.MANAGE_ROLES,
      PermissionConstants.IDENTITY.USERS.MANAGE_PERMISSIONS,
      PermissionConstants.IDENTITY.ROLES.MANAGE_PERMISSIONS,
      PermissionConstants.SYSTEM.ADMIN.USER_MANAGEMENT
    ],
    AUDIT_ACCESS: [
      PermissionConstants.IDENTITY.AUDIT.READ,
      PermissionConstants.IDENTITY.USERS.VIEW_AUDIT,
      PermissionConstants.IDENTITY.ROLES.VIEW_AUDIT,
      PermissionConstants.IDENTITY.PERMISSIONS.AUDIT
    ]
  };

  // Helper methods
  static getAllIdentityPermissions(): string[] {
    const permissions: string[] = [];

    Object.values(this.IDENTITY).forEach(category => {
      Object.values(category).forEach(permission => {
        if (typeof permission === 'string') {
          permissions.push(permission);
        }
      });
    });

    return permissions;
  }

  static getAllSpeedReadingPermissions(): string[] {
    const permissions: string[] = [];

    Object.values(this.SPEED_READING).forEach(category => {
      Object.values(category).forEach(permission => {
        if (typeof permission === 'string') {
          permissions.push(permission);
        }
      });
    });

    return permissions;
  }

  static getAllPermissions(): string[] {
    return [
      ...this.getAllIdentityPermissions(),
      ...this.getAllSpeedReadingPermissions(),
      ...Object.values(this.SYSTEM.ADMIN)
    ];
  }

  static getPermissionsByService(service: string): string[] {
    switch (service.toLowerCase()) {
      case 'identity':
        return this.getAllIdentityPermissions();
      case 'speedreading':
        return this.getAllSpeedReadingPermissions();
      case 'system':
        return Object.values(this.SYSTEM.ADMIN);
      default:
        return [];
    }
  }

  static isWildcardPermission(permission: string): boolean {
    return permission.includes('*');
  }

  static getServiceFromPermission(permission: string): string {
    return permission.split('.')[0];
  }

  static getResourceFromPermission(permission: string): string {
    const parts = permission.split('.');
    return parts.length > 1 ? parts[1] : '';
  }

  static getActionFromPermission(permission: string): string {
    const parts = permission.split('.');
    return parts.length > 2 ? parts[2] : '';
  }
}