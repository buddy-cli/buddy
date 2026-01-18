namespace Buddy.Core.Instructions;

public static class ProjectInstructionsLoader {
    public static async Task<string?> Load(string workingDirectory) {
        if (string.IsNullOrWhiteSpace(workingDirectory)) {
            workingDirectory = Directory.GetCurrentDirectory();
        }

        var dir = new DirectoryInfo(workingDirectory);
        while (dir is not null) {
            var agents = Path.Combine(dir.FullName, "AGENTS.md");
            if (File.Exists(agents)) {
                return await SafeReadAllText(agents);
            }

            dir = dir.Parent;
        }

        return null;
    }

    private static async Task<string> SafeReadAllText(string path) {
        try {
            return await File.ReadAllTextAsync(path);
        }
        catch (Exception ex) {
            return $"(failed to read {Path.GetFileName(path)}: {ex.Message})";
        }
    }
}
