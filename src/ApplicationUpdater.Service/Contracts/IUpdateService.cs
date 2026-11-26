using ApplicationUpdater.Service.Updates;

namespace ApplicationUpdater.Service.Contracts;

public interface IUpdateService
{
    UpdateVersion? GetLatestVersion();

    Stream? OpenDownload(Version version);
}
