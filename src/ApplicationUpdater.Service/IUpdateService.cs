namespace ApplicationUpdater.Service;

public interface IUpdateService
{
    UpdateVersion? GetLatestVersion();
}
