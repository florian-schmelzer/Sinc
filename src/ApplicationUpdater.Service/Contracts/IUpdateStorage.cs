namespace ApplicationUpdater.Service.Contracts;

public interface IUpdateStorage
{
    Version? GetLatestVersion();

    Stream? OpenRead(Version version);
}
