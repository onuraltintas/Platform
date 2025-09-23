namespace Identity.Core.Constants;

/// <summary>
/// Centralized permission constants for the Identity service
/// Format: Service.Resource.Action (e.g., Identity.Users.Read)
/// </summary>
public static class PermissionConstants
{
    public const string SERVICE_PREFIX = "Identity";

    // Identity service permissions
    public static class Identity
    {
        // User permissions
        public static class Users
        {
            private const string RESOURCE = $"{SERVICE_PREFIX}.Users";

            public const string Read = $"{RESOURCE}.Read";
            public const string Write = $"{RESOURCE}.Write";
            public const string Create = $"{RESOURCE}.Create";
            public const string Update = $"{RESOURCE}.Update";
            public const string Delete = $"{RESOURCE}.Delete";
            public const string Activate = $"{RESOURCE}.Activate";
            public const string Deactivate = $"{RESOURCE}.Deactivate";
            public const string ResetPassword = $"{RESOURCE}.ResetPassword";
            public const string ChangeRole = $"{RESOURCE}.ChangeRole";
            public const string ViewProfile = $"{RESOURCE}.ViewProfile";
        }

        // Role permissions
        public static class Roles
        {
            private const string RESOURCE = $"{SERVICE_PREFIX}.Roles";

            public const string Read = $"{RESOURCE}.Read";
            public const string Write = $"{RESOURCE}.Write";
            public const string Create = $"{RESOURCE}.Create";
            public const string Update = $"{RESOURCE}.Update";
            public const string Delete = $"{RESOURCE}.Delete";
            public const string AssignPermissions = $"{RESOURCE}.AssignPermissions";
            public const string ViewPermissions = $"{RESOURCE}.ViewPermissions";
            public const string Clone = $"{RESOURCE}.Clone";
            public const string Compare = $"{RESOURCE}.Compare";
        }

        // Permission permissions (meta-permissions)
        public static class Permissions
        {
            private const string RESOURCE = $"{SERVICE_PREFIX}.Permissions";

            public const string Read = $"{RESOURCE}.Read";
            public const string Write = $"{RESOURCE}.Write";
            public const string Create = $"{RESOURCE}.Create";
            public const string Update = $"{RESOURCE}.Update";
            public const string Delete = $"{RESOURCE}.Delete";
            public const string Discover = $"{RESOURCE}.Discover";
            public const string Matrix = $"{RESOURCE}.Matrix";
            public const string ByService = $"{RESOURCE}.ByService";
            public const string ByResource = $"{RESOURCE}.ByResource";
        }

        // Group permissions
        public static class Groups
        {
            private const string RESOURCE = $"{SERVICE_PREFIX}.Groups";

            public const string Read = $"{RESOURCE}.Read";
            public const string Write = $"{RESOURCE}.Write";
            public const string Create = $"{RESOURCE}.Create";
            public const string Update = $"{RESOURCE}.Update";
            public const string Delete = $"{RESOURCE}.Delete";
            public const string AddMember = $"{RESOURCE}.AddMember";
            public const string RemoveMember = $"{RESOURCE}.RemoveMember";
            public const string ManageRoles = $"{RESOURCE}.ManageRoles";
        }

        // Authentication permissions
        public static class Auth
        {
            private const string RESOURCE = $"{SERVICE_PREFIX}.Auth";

            public const string Login = $"{RESOURCE}.Login";
            public const string Register = $"{RESOURCE}.Register";
            public const string RefreshToken = $"{RESOURCE}.RefreshToken";
            public const string Logout = $"{RESOURCE}.Logout";
            public const string ForgotPassword = $"{RESOURCE}.ForgotPassword";
            public const string ResetPassword = $"{RESOURCE}.ResetPassword";
            public const string VerifyEmail = $"{RESOURCE}.VerifyEmail";
        }

        // Account permissions
        public static class Account
        {
            private const string RESOURCE = $"{SERVICE_PREFIX}.Account";

            public const string ViewProfile = $"{RESOURCE}.ViewProfile";
            public const string UpdateProfile = $"{RESOURCE}.UpdateProfile";
            public const string ChangePassword = $"{RESOURCE}.ChangePassword";
            public const string DeleteAccount = $"{RESOURCE}.DeleteAccount";
        }

        // System administration permissions
        public static class System
        {
            private const string RESOURCE = $"{SERVICE_PREFIX}.System";

            public const string ViewLogs = $"{RESOURCE}.ViewLogs";
            public const string ViewMetrics = $"{RESOURCE}.ViewMetrics";
            public const string ManageSettings = $"{RESOURCE}.ManageSettings";
            public const string ViewHealth = $"{RESOURCE}.ViewHealth";
            public const string ManageCache = $"{RESOURCE}.ManageCache";
        }
    }

    /// <summary>
    /// Get all permissions as a flat list
    /// </summary>
    public static List<string> GetAllPermissions()
    {
        var permissions = new List<string>();

        // Use reflection to get all string constants
        var identityType = typeof(Identity);
        var nestedTypes = identityType.GetNestedTypes();

        foreach (var nestedType in nestedTypes)
        {
            var fields = nestedType.GetFields()
                .Where(f => f.IsLiteral && f.FieldType == typeof(string));

            foreach (var field in fields)
            {
                var value = field.GetValue(null)?.ToString();
                if (!string.IsNullOrEmpty(value))
                {
                    permissions.Add(value);
                }
            }
        }

        return permissions;
    }

    /// <summary>
    /// Get permissions by resource type
    /// </summary>
    public static List<string> GetPermissionsByResource(string resource)
    {
        return GetAllPermissions()
            .Where(p => p.Contains($".{resource}."))
            .ToList();
    }

    /// <summary>
    /// Check if a permission exists
    /// </summary>
    public static bool IsValidPermission(string permission)
    {
        return GetAllPermissions().Contains(permission);
    }
}

/// <summary>
/// Role-based permission templates for easier management
/// </summary>
public static class RolePermissionTemplates
{
    public static Dictionary<string, List<string>> Templates = new()
    {
        ["SuperAdmin"] = new()
        {
            "*.*.*" // Wildcard - all permissions
        },

        ["Admin"] = new()
        {
            // Full access to Identity service
            "Identity.*.*"
        },

        ["Manager"] = new()
        {
            // User management
            PermissionConstants.Identity.Users.Read,
            PermissionConstants.Identity.Users.Create,
            PermissionConstants.Identity.Users.Update,
            PermissionConstants.Identity.Users.Activate,
            PermissionConstants.Identity.Users.Deactivate,
            PermissionConstants.Identity.Users.ViewProfile,

            // Role viewing only
            PermissionConstants.Identity.Roles.Read,
            PermissionConstants.Identity.Roles.ViewPermissions,

            // Group management
            PermissionConstants.Identity.Groups.Read,
            PermissionConstants.Identity.Groups.Create,
            PermissionConstants.Identity.Groups.Update,
            PermissionConstants.Identity.Groups.AddMember,
            PermissionConstants.Identity.Groups.RemoveMember,

            // Permission viewing
            PermissionConstants.Identity.Permissions.Read,
        },

        ["User"] = new()
        {
            // Self-service permissions
            PermissionConstants.Identity.Account.ViewProfile,
            PermissionConstants.Identity.Account.UpdateProfile,
            PermissionConstants.Identity.Account.ChangePassword,

            // Basic auth
            PermissionConstants.Identity.Auth.Login,
            PermissionConstants.Identity.Auth.Logout,
            PermissionConstants.Identity.Auth.RefreshToken,
        },

        ["Guest"] = new()
        {
            // Public permissions only
            PermissionConstants.Identity.Auth.Login,
            PermissionConstants.Identity.Auth.Register,
            PermissionConstants.Identity.Auth.ForgotPassword,
        }
    };

    public static List<string> GetRolePermissions(string roleName)
    {
        return Templates.TryGetValue(roleName, out var permissions)
            ? permissions
            : new List<string>();
    }
}