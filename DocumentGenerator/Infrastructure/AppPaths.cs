namespace DocumentGenerator.Infrastructure;

public static class AppPaths
{
    public static string Root { get; private set; } = AppContext.BaseDirectory;
    public static string Data => Path.Combine(Root, "data");
    public static string Templates => Path.Combine(Root, "templates");
    public static string Exports => Path.Combine(Root, "exports");
    public static string Backups => Path.Combine(Root, "backups");
    public static string Logs => Path.Combine(Root, "logs");
    public static string Database => Path.Combine(Data, "document-generator.db");

    public static void Initialize()
    {
        // Portable mode: data is stored beside the executable. If the folder is read-only,
        // fail visibly rather than silently scattering user data in AppData.
        Root = AppContext.BaseDirectory;
        foreach (var folder in new[] { Data, Templates, Exports, Backups, Logs })
            Directory.CreateDirectory(folder);
        var probe = Path.Combine(Data, $".write-test-{Guid.NewGuid():N}");
        File.WriteAllText(probe, "ok");
        File.Delete(probe);
    }

    public static string ResolveTemplate(string relativePath)
    {
        var fullPath = Path.GetFullPath(Path.Combine(Root, relativePath));
        var rootPath = Path.GetFullPath(Templates) + Path.DirectorySeparatorChar;
        if (!fullPath.StartsWith(rootPath, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Некоректний шлях до шаблону.");
        return fullPath;
    }
}
