namespace Atlas.DependencyInjection.Test;

internal static class RepositoryPaths
{
    /// <summary>The folder that holds <c>Atlas.slnx</c>. The tests read the settings files of the apps from it.</summary>
    public static string Root()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "Atlas.slnx")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new DirectoryNotFoundException("Could not find the repository root (Atlas.slnx).");
    }
}
