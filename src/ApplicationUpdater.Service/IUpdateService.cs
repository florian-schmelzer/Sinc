namespace ApplicationUpdater.Service;

public interface IUpdateService
{
    UpdateVersion? GetLatestVersion();

    Stream? OpenDownload(Version version);
}
