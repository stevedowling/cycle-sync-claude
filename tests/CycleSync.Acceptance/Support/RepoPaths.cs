namespace CycleSync.Acceptance.Support;

/// <summary>
/// Locates repository paths at test time by walking up from the test assembly location
/// until the solution file is found.
/// </summary>
public static class RepoPaths
{
    public static string RepoRoot { get; } = FindRepoRoot();

    public static string WebSource => Path.Combine(RepoRoot, "src", "web");

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "CycleSync.slnx")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException("Could not locate repository root (CycleSync.slnx not found).");
    }
}
