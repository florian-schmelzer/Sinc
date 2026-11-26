using System.IO.Abstractions.TestingHelpers;
using ApplicationUpdater.Bootloader.Updates;
using Xunit;

namespace ApplicationUpdater.Tests;

public sealed class InstalledApplicationTests
{
    private readonly MockFileSystem _fileSystem = new();
    private readonly string _applicationPath;
    private readonly string _downloadPath;

    public InstalledApplicationTests()
    {
        var directory = _fileSystem.Path.GetFullPath("installation");
        _applicationPath = _fileSystem.Path.Combine(directory, "SampleApplication.exe");
        _downloadPath = _fileSystem.Path.Combine(directory, "download.tmp");
        _fileSystem.AddFile(_applicationPath, new MockFileData("installed")
        {
            FileVersionInfo = new MockFileVersionInfo(_applicationPath, fileVersion: "1.0.172.5")
        });
    }

    [Fact]
    public void ReadVersion_ReturnsTheFourComponentsOfTheFileVersion()
    {
        var installation = new InstalledApplication(_fileSystem);

        Assert.Equal(new Version(1, 0, 172, 5), installation.ReadVersion(_applicationPath));
    }

    [Fact]
    public void ReplaceExecutable_MovesTheDownloadOverTheInstalledFile()
    {
        _fileSystem.AddFile(_downloadPath, new MockFileData("downloaded"));
        var installation = new InstalledApplication(_fileSystem);

        installation.ReplaceExecutable(_applicationPath, _downloadPath);

        Assert.Equal("downloaded", _fileSystem.File.ReadAllText(_applicationPath));
        Assert.False(_fileSystem.File.Exists(_downloadPath));
    }

    [Fact]
    public void ReplaceExecutable_LeavesInstalledFileIntactWhenSourceIsMissing()
    {
        var installation = new InstalledApplication(_fileSystem);

        Assert.Throws<FileNotFoundException>(() => installation.ReplaceExecutable(_applicationPath, _downloadPath));

        Assert.Equal("installed", _fileSystem.File.ReadAllText(_applicationPath));
    }
}
