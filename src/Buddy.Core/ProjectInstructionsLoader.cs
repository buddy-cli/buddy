namespace Buddy.Core;

public static class ProjectInstructionsLoader {
    public static string? Load(string workingDirectory) {
        if (string.IsNullOrWhiteSpace(workingDirectory)) {
            workingDirectory = Directory.GetCurrentDirectory();
        }

        var dir = new DirectoryInfo(workingDirectory);
        while (dir is not null) {
            var buddy = Path.Combine(dir.FullName, "BUDDY.md");
            if (File.Exists(buddy)) {
                return SafeReadAllText(buddy);
            }

            var agents = Path.Combine(dir.FullName, "AGENTS.md");
            if (File.Exists(agents)) {
                return SafeReadAllText(agents);
            }

            dir = dir.Parent;
        }

        return null;
    }

    private static string SafeReadAllText(string path) {
        try {
            return File.ReadAllText(path);
        }
        catch (Exception ex) {
            return $"(failed to read {Path.GetFileName(path)}: {ex.Message})";
        }
    }
}
