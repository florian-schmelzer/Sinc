namespace ApplicationUpdater.Service.Contracts;

public interface IUpdateStorage
{
    /// <summary>Returns the latest version, or <see langword="null"/> if no version is available.</summary>
    Version? GetLatestVersion();

    /// <summary>Opens the requested version for reading, or returns <see langword="null"/> if it is unavailable. The caller disposes the stream.</summary>
    Stream? OpenRead(Version version);
}
