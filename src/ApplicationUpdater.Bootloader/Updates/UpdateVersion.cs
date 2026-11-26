namespace ApplicationUpdater.Bootloader.Updates;

public sealed record UpdateVersion(Version Version, string FileName)
{
    public bool IsNewerThan(Version installedVersion) => Version > installedVersion;
}
