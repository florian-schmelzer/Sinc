using System.IO.Abstractions;
using ApplicationUpdater.Service.Contracts;

namespace ApplicationUpdater.Service.Storage;

public sealed class FilesystemUpdateStorage(string rootPath, IFileSystem fileSystem) : IUpdateStorage
{
    public Version? GetLatestVersion()
    {
        Version? latestVersion = null;

        foreach (var directory in fileSystem.DirectoryInfo.New(rootPath).EnumerateDirectories())
        {
            if (!Version.TryParse(directory.Name, out var version) ||
                version.Revision < 0 ||
                directory.Name != version.ToString() ||
                !fileSystem.File.Exists(Path.Combine(directory.FullName, SampleApplication.FileName)))
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
            return fileSystem.File.OpenRead(executablePath);
        }
        catch (FileNotFoundException)
        {
            return null;
        }
        catch (DirectoryNotFoundException) when (fileSystem.Directory.Exists(rootPath))
        {
            return null;
        }
        catch (UnauthorizedAccessException) when (fileSystem.Directory.Exists(executablePath))
        {
            return null;
        }
    }
}
