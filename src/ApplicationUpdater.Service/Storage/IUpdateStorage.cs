namespace ApplicationUpdater.Service.Storage;

public interface IUpdateStorage
{
    /// <summary>Returns the latest available version, or null if there are no versions.</summary>
    Version? GetLatestVersion();
}
