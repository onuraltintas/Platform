namespace EgitimPlatform.Shared.Security.Constants;

public static class Roles
{
    public const string SuperAdmin = "SuperAdmin";
    public const string Admin = "Admin";
    public const string Teacher = "Teacher";
    public const string Student = "Student";
    public const string ContentCreator = "ContentCreator";
    public const string Moderator = "Moderator";
    public const string Support = "Support";
    
    public static IEnumerable<string> GetAllRoles()
    {
        return new[]
        {
            SuperAdmin,
            Admin,
            Teacher,
            Student,
            ContentCreator,
            Moderator,
            Support
        };
    }
    
    public static Dictionary<string, string[]> GetRolePermissions()
    {
        return new Dictionary<string, string[]>
        {
            [SuperAdmin] = Permissions.GetAllPermissions().ToArray(),
            
            [Admin] = new[]
            {
                Permissions.Users.Read,
                Permissions.Users.Write,
                Permissions.Users.ManageRoles,
                Permissions.Courses.Read,
                Permissions.Courses.Write,
                Permissions.Courses.Delete,
                Permissions.Content.Read,
                Permissions.Content.Write,
                Permissions.Content.ManageLibrary,
                Permissions.Reports.View,
                Permissions.Reports.Export,
                Permissions.System.ViewLogs,
                Permissions.System.ManageSettings,
                Permissions.Notifications.Send,
                Permissions.Notifications.ManageTemplates
            },
            
            [Teacher] = new[]
            {
                Permissions.Courses.Read,
                Permissions.Courses.Write,
                Permissions.Courses.Publish,
                Permissions.Content.Read,
                Permissions.Content.Write,
                Permissions.Content.Upload,
                Permissions.Reports.ViewCourseReports,
                Permissions.Notifications.Send
            },
            
            [Student] = new[]
            {
                Permissions.Courses.Read,
                Permissions.Content.Read,
                Permissions.Content.Download,
                Permissions.Users.ViewProfile,
                Permissions.Users.EditProfile
            },
            
            [ContentCreator] = new[]
            {
                Permissions.Content.Read,
                Permissions.Content.Write,
                Permissions.Content.Upload,
                Permissions.Content.ManageLibrary,
                Permissions.Courses.Read,
                Permissions.Courses.Write
            },
            
            [Moderator] = new[]
            {
                Permissions.Users.Read,
                Permissions.Courses.Read,
                Permissions.Content.Read,
                Permissions.Content.Delete,
                Permissions.Reports.View,
                Permissions.Notifications.Send
            },
            
            [Support] = new[]
            {
                Permissions.Users.Read,
                Permissions.Users.ViewProfile,
                Permissions.Courses.Read,
                Permissions.Reports.ViewUserReports,
                Permissions.Notifications.Send,
                Permissions.System.ViewLogs
            }
        };
    }
}