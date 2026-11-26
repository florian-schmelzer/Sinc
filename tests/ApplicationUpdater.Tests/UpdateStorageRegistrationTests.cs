using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using ApplicationUpdater.Service.Contracts;
using ApplicationUpdater.Service.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace ApplicationUpdater.Tests;

public sealed class UpdateStorageRegistrationTests
{
    [Fact]
    public void AddUpdateStorage_ResolvesRelativeRootFromContentRoot()
    {
        var contentRoot = Directory.CreateTempSubdirectory("application-updater-tests-");
        try
        {
            var versionDirectory = contentRoot.CreateSubdirectory("updates/1.2.352.32");
            File.WriteAllText(Path.Combine(versionDirectory.FullName, "SampleApplication.exe"), "sample");
            var configuration = CreateConfiguration("FileSystem", "updates");
            var services = new ServiceCollection();

            services.AddUpdateStorage(configuration, contentRoot.FullName);
            using var serviceProvider = services.BuildServiceProvider();
            var storage = Assert.IsType<FilesystemUpdateStorage>(serviceProvider.GetRequiredService<IUpdateStorage>());

            Assert.Equal(new Version(1, 2, 352, 32), storage.GetLatestVersion());
        }
        finally
        {
            contentRoot.Delete(recursive: true);
        }
    }

    [Fact]
    public void AddUpdateStorage_UsesRegisteredFileSystem()
    {
        var contentRoot = Path.Combine(AppContext.BaseDirectory, "service-content");
        var fileSystem = new MockFileSystem();
        fileSystem.AddFile(Path.Combine(contentRoot, "updates", "1.2.352.32", "SampleApplication.exe"),
            new MockFileData("sample"));
        var configuration = CreateConfiguration("FileSystem", "updates");
        var services = new ServiceCollection();
        services.AddSingleton<IFileSystem>(fileSystem);

        services.AddUpdateStorage(configuration, contentRoot);
        using var serviceProvider = services.BuildServiceProvider();
        var storage = serviceProvider.GetRequiredService<IUpdateStorage>();

        Assert.Equal(new Version(1, 2, 352, 32), storage.GetLatestVersion());
    }

    [Theory]
    [InlineData("Cloud", "Cloud")]
    [InlineData(null, "(fehlt)")]
    public void AddUpdateStorage_RejectsUnsupportedProvider(string? provider, string expectedName)
    {
        var configuration = CreateConfiguration(provider, "updates");
        var services = new ServiceCollection();

        var exception = Assert.Throws<InvalidOperationException>(
            () => services.AddUpdateStorage(configuration, AppContext.BaseDirectory));

        Assert.Contains("UpdateStorage:Provider", exception.Message);
        Assert.Contains(expectedName, exception.Message);
    }

    [Fact]
    public void AddUpdateStorage_ReportsDatabaseProviderAsNotImplemented()
    {
        var configuration = CreateConfiguration("Database", null);
        var services = new ServiceCollection();

        var exception = Assert.Throws<InvalidOperationException>(
            () => services.AddUpdateStorage(configuration, AppContext.BaseDirectory));

        Assert.Contains("'Database' ist noch nicht implementiert", exception.Message);
    }

    [Fact]
    public void AddUpdateStorage_RejectsMissingRootPath()
    {
        var configuration = CreateConfiguration("FileSystem", null);
        var services = new ServiceCollection();

        var exception = Assert.Throws<InvalidOperationException>(
            () => services.AddUpdateStorage(configuration, AppContext.BaseDirectory));

        Assert.Contains("UpdateStorage:RootPath", exception.Message);
    }

    private static IConfiguration CreateConfiguration(string? provider, string? rootPath)
    {
        return new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["UpdateStorage:Provider"] = provider,
            ["UpdateStorage:RootPath"] = rootPath
        }).Build();
    }
}
