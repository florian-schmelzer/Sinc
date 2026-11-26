using System.IO.Abstractions;

namespace ApplicationUpdater.Bootloader.Updates;

public sealed class InstalledApplication(IFileSystem fileSystem)
{
    public Version? ReadVersion(string applicationPath)
    {
        var versionInfo = fileSystem.FileVersionInfo.GetVersionInfo(applicationPath);
        if (string.IsNullOrWhiteSpace(versionInfo.FileVersion))
        {
            return null;
        }

        return new Version(
            versionInfo.FileMajorPart,
            versionInfo.FileMinorPart,
            versionInfo.FileBuildPart,
            versionInfo.FilePrivatePart);
    }

    public void ReplaceExecutable(string applicationPath, string temporaryPath)
    {
        fileSystem.File.Move(temporaryPath, applicationPath, overwrite: true);
    }
}
