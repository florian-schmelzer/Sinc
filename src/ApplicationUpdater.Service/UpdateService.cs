using ApplicationUpdater.Service.Contracts;

namespace ApplicationUpdater.Service;

public sealed class UpdateService(IUpdateStorage storage) : IUpdateService
{
    public UpdateVersion? GetLatestVersion()
    {
        var version = storage.GetLatestVersion();
        return version is null ? null : new UpdateVersion(version, SampleApplication.FileName);
    }

    public Stream? OpenDownload(Version version) => storage.OpenRead(version);
}
