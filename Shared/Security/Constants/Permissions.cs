namespace EgitimPlatform.Shared.Security.Constants;

public static class Permissions
{
    public static class Users
    {
        public const string Read = "Users.Read";
        public const string Write = "Users.Write";
        public const string Delete = "Users.Delete";
        public const string ViewProfile = "Users.ViewProfile";
        public const string EditProfile = "Users.EditProfile";
        public const string ManageRoles = "Users.ManageRoles";
        public const string ManagePermissions = "Users.ManagePermissions";
    }
    
    public static class Courses
    {
        public const string Read = "Courses.Read";
        public const string Write = "Courses.Write";
        public const string Delete = "Courses.Delete";
        public const string Publish = "Courses.Publish";
        public const string Unpublish = "Courses.Unpublish";
        public const string ViewAnalytics = "Courses.ViewAnalytics";
        public const string ManageEnrollments = "Courses.ManageEnrollments";
    }
    
    public static class Content
    {
        public const string Read = "Content.Read";
        public const string Write = "Content.Write";
        public const string Delete = "Content.Delete";
        public const string Upload = "Content.Upload";
        public const string Download = "Content.Download";
        public const string ManageLibrary = "Content.ManageLibrary";
    }
    
    public static class Reports
    {
        public const string View = "Reports.View";
        public const string Export = "Reports.Export";
        public const string ViewUserReports = "Reports.ViewUserReports";
        public const string ViewCourseReports = "Reports.ViewCourseReports";
        public const string ViewSystemReports = "Reports.ViewSystemReports";
    }
    
    public static class System
    {
        public const string Admin = "System.Admin";
        public const string ViewLogs = "System.ViewLogs";
        public const string ManageSettings = "System.ManageSettings";
        public const string ManageIntegrations = "System.ManageIntegrations";
        public const string ViewMetrics = "System.ViewMetrics";
        public const string ManageBackups = "System.ManageBackups";
    }
    
    public static class Notifications
    {
        public const string Send = "Notifications.Send";
        public const string SendBulk = "Notifications.SendBulk";
        public const string ManageTemplates = "Notifications.ManageTemplates";
        public const string ViewHistory = "Notifications.ViewHistory";
        
        // Additional permissions for NotificationService
        public const string NotificationRead = "Notifications.NotificationRead";
        public const string NotificationCreate = "Notifications.NotificationCreate";
        public const string NotificationUpdate = "Notifications.NotificationUpdate";
        public const string NotificationDelete = "Notifications.NotificationDelete";
        public const string NotificationSend = "Notifications.NotificationSend";
        public const string NotificationManage = "Notifications.NotificationManage";
        
        // Template permissions
        public const string NotificationTemplateRead = "Notifications.NotificationTemplateRead";
        public const string NotificationTemplateCreate = "Notifications.NotificationTemplateCreate";
        public const string NotificationTemplateUpdate = "Notifications.NotificationTemplateUpdate";
        public const string NotificationTemplateDelete = "Notifications.NotificationTemplateDelete";
        public const string NotificationTemplateManage = "Notifications.NotificationTemplateManage";
    }
    
    public static class FeatureFlags
    {
        public const string View = "FeatureFlags.View";
        public const string Manage = "FeatureFlags.Manage";
        public const string Toggle = "FeatureFlags.Toggle";
    }
    
    public static IEnumerable<string> GetAllPermissions()
    {
        var permissions = new List<string>();
        
        permissions.AddRange(typeof(Users).GetFields().Select(f => f.GetValue(null)?.ToString() ?? string.Empty));
        permissions.AddRange(typeof(Courses).GetFields().Select(f => f.GetValue(null)?.ToString() ?? string.Empty));
        permissions.AddRange(typeof(Content).GetFields().Select(f => f.GetValue(null)?.ToString() ?? string.Empty));
        permissions.AddRange(typeof(Reports).GetFields().Select(f => f.GetValue(null)?.ToString() ?? string.Empty));
        permissions.AddRange(typeof(System).GetFields().Select(f => f.GetValue(null)?.ToString() ?? string.Empty));
        permissions.AddRange(typeof(Notifications).GetFields().Select(f => f.GetValue(null)?.ToString() ?? string.Empty));
        permissions.AddRange(typeof(FeatureFlags).GetFields().Select(f => f.GetValue(null)?.ToString() ?? string.Empty));
        
        return permissions.Where(p => !string.IsNullOrEmpty(p));
    }
}