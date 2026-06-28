namespace TaskFlow.Application.Caching;

public static class CacheKeys
{
    public static string UserWorkspaces(string userId)       => $"user:{userId}:workspaces";
    public static string WorkspaceMembers(Guid workspaceId)  => $"workspace:{workspaceId}:members";
    public static string WorkspaceColumns(Guid workspaceId)  => $"workspace:{workspaceId}:columns";
    public static string TempFile(Guid attachmentId)         => $"file:temp:{attachmentId}";

    public static string HitCounter(string category)  => $"stats:cache:hits:{category}";
    public static string MissCounter(string category) => $"stats:cache:misses:{category}";

    public static class Category
    {
        public const string UserWorkspaces = "user-workspaces";
        public const string Members        = "members";
        public const string Columns        = "columns";
    }

    public static class Ttl
    {
        public static readonly TimeSpan UserWorkspaces = TimeSpan.FromMinutes(5);
        public static readonly TimeSpan Members        = TimeSpan.FromMinutes(10);
        public static readonly TimeSpan Columns        = TimeSpan.FromMinutes(10);
        public static readonly TimeSpan TempFile       = TimeSpan.FromMinutes(30);
    }
}
