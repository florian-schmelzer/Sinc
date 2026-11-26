using System.Net;
using System.Text.Json;
using ApplicationUpdater.Service.Storage;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

namespace ApplicationUpdater.Tests;

public sealed class LatestUpdateEndpointTests : IDisposable
{
    private readonly DirectoryInfo _root = Directory.CreateTempSubdirectory("application-updater-tests-");

    [Fact]
    public async Task GetLatest_ReturnsVersionAndFilename()
    {
        foreach (var version in new[] { "2.0.577.100", "2.0.577.99" })
        {
            var directory = _root.CreateSubdirectory(version);
            File.WriteAllText(Path.Combine(directory.FullName, "SampleApplication.exe"), version);
        }

        using var factory = CreateFactory();
        using var client = factory.CreateClient();
        using var response = await client.GetAsync("/api/updates/latest");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
        using var metadata = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("2.0.577.100", metadata.RootElement.GetProperty("version").GetString());
        Assert.Equal("SampleApplication.exe", metadata.RootElement.GetProperty("fileName").GetString());
    }

    [Fact]
    public async Task GetLatest_ReturnsNotFoundWhenStorageIsEmpty()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();
        using var response = await client.GetAsync("/api/updates/latest");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    public void Dispose()
    {
        _root.Delete(recursive: true);
    }

    private WebApplicationFactory<Program> CreateFactory()
    {
        return new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IUpdateStorage>();
                services.AddSingleton<IUpdateStorage>(new FilesystemUpdateStorage(_root.FullName));
            }));
    }
}
