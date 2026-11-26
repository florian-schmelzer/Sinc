using ApplicationUpdater.Service.Contracts;

namespace ApplicationUpdater.Service.Storage;

public sealed class FilesystemUpdateStorage(string rootPath) : IUpdateStorage
{
    public Version? GetLatestVersion()
    {
        Version? latestVersion = null;

        foreach (var directory in new DirectoryInfo(rootPath).EnumerateDirectories())
        {
            if (!Version.TryParse(directory.Name, out var version) ||
                version.Revision < 0 ||
                directory.Name != version.ToString() ||
                !File.Exists(Path.Combine(directory.FullName, SampleApplication.FileName)))
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

    public Stream? OpenRead(Version version)
    {
        ArgumentNullException.ThrowIfNull(version);
        if (version.Revision < 0)
        {
            return null;
        }

        var executablePath = Path.Combine(rootPath, version.ToString(), SampleApplication.FileName);
        try
        {
            return File.OpenRead(executablePath);
        }
        catch (FileNotFoundException)
        {
            return null;
        }
        catch (DirectoryNotFoundException) when (Directory.Exists(rootPath))
        {
            return null;
        }
        catch (UnauthorizedAccessException) when (Directory.Exists(executablePath))
        {
            return null;
        }
    }
}
