namespace Buddy.Core.Tools;

internal static class PathResolver
{
    public static string Resolve(string workingDirectory, string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("Path is required.", nameof(path));
        }

        if (string.IsNullOrWhiteSpace(workingDirectory))
        {
            workingDirectory = Directory.GetCurrentDirectory();
        }

        path = ExpandTilde(path.Trim());

        if (!Path.IsPathRooted(path))
        {
            path = Path.Combine(workingDirectory, path);
        }

        return Path.GetFullPath(path);
    }

    private static string ExpandTilde(string path)
    {
        if (!path.StartsWith('~'))
        {
            return path;
        }

        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        if (string.IsNullOrWhiteSpace(home))
        {
            return path;
        }

        if (path == "~")
        {
            return home;
        }

        if (path.StartsWith("~/", StringComparison.Ordinal))
        {
            return Path.Combine(home, path[2..]);
        }

        return path;
    }
}
