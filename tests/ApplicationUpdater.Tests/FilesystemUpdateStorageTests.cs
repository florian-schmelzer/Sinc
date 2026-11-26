using ApplicationUpdater.Service.Storage;
using Xunit;

namespace ApplicationUpdater.Tests;

public sealed class FilesystemUpdateStorageTests : IDisposable
{
    private readonly DirectoryInfo _root = Directory.CreateTempSubdirectory("application-updater-tests-");

    [Fact]
    public void GetLatestVersion_ChoosesHighestNumericalVersion()
    {
        CreateVersionWithExecutable("2.0.577.100");
        CreateVersionWithExecutable("1.2.352.32");
        CreateVersionWithExecutable("2.0.577.99");

        var storage = new FilesystemUpdateStorage(_root.FullName);

        Assert.Equal(new Version(2, 0, 577, 100), storage.GetLatestVersion());
    }

    [Fact]
    public void GetLatestVersion_ReturnsNullForEmptyStorage()
    {
        var storage = new FilesystemUpdateStorage(_root.FullName);

        Assert.Null(storage.GetLatestVersion());
    }

    [Theory]
    [InlineData("latest")]
    [InlineData("9.0.0")]
    [InlineData("9.0.0.00")]
    public void GetLatestVersion_IgnoresInvalidVersionDirectories(string directoryName)
    {
        CreateVersionWithExecutable(directoryName);

        var storage = new FilesystemUpdateStorage(_root.FullName);

        Assert.Null(storage.GetLatestVersion());
    }

    [Fact]
    public void GetLatestVersion_IgnoresHigherVersionWithoutExecutable()
    {
        CreateVersionWithExecutable("1.2.352.32");
        _root.CreateSubdirectory("2.0.577.98");

        var storage = new FilesystemUpdateStorage(_root.FullName);

        Assert.Equal(new Version(1, 2, 352, 32), storage.GetLatestVersion());
    }

    [Fact]
    public void GetLatestVersion_ReportsMissingStorageDirectory()
    {
        var storage = new FilesystemUpdateStorage(Path.Combine(_root.FullName, "missing"));

        Assert.Throws<DirectoryNotFoundException>(() => storage.GetLatestVersion());
    }

    public void Dispose()
    {
        _root.Delete(recursive: true);
    }

    private void CreateVersionWithExecutable(string directoryName)
    {
        var directory = _root.CreateSubdirectory(directoryName);
        using var executable = File.Create(Path.Combine(directory.FullName, "SampleApplication.exe"));
    }
}
