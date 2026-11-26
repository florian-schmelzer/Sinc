namespace ApplicationUpdater.Service.Storage;

public interface IUpdateStorage
{
    /// <summary>Returns the latest available version, or null if there are no versions.</summary>
    Version? GetLatestVersion();

    /// <summary>Opens the requested version for reading, or returns null if unavailable. The caller disposes the stream.</summary>
    Stream? OpenRead(Version version);
}
