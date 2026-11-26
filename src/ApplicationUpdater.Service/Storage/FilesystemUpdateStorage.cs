namespace ApplicationUpdater.Service.Storage;

public sealed class FilesystemUpdateStorage(string rootPath) : IUpdateStorage
{
    private const string ApplicationFileName = "SampleApplication.exe";

    public Version? GetLatestVersion()
    {
        Version? latestVersion = null;

        foreach (var directory in new DirectoryInfo(rootPath).EnumerateDirectories())
        {
            if (!Version.TryParse(directory.Name, out var version) ||
                version.Revision < 0 ||
                directory.Name != version.ToString() ||
                !File.Exists(Path.Combine(directory.FullName, ApplicationFileName)))
            {
                continue;
            }

            if (latestVersion is null || version > latestVersion)
            {
                latestVersion = version;
            }
        }

        return latestVersion;
    }
}
