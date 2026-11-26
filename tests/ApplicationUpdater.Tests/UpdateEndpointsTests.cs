using System.Net;
using System.Text.Json;
using ApplicationUpdater.Service.Contracts;
using ApplicationUpdater.Service.Storage;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

namespace ApplicationUpdater.Tests;

public sealed class UpdateEndpointsTests : IDisposable
{
    private readonly DirectoryInfo _root = Directory.CreateTempSubdirectory("application-updater-tests-");

    [Fact]
    public async Task GetRoot_ReturnsPlainTextStatus()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();
        using var response = await client.GetAsync("/");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/plain", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal("UpdateService ist erreichbar.", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task GetLatest_ReturnsVersionAndFilename()
    {
        foreach (var version in new[] { "2.0.577.100", "2.0.577.99" })
        {
            CreateVersionWithExecutable(version, []);
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
        Assert.Empty(await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Download_ReturnsTheRequestedExecutableAsAnAttachment()
    {
        byte[] expectedContents = [0x4D, 0x5A, 0x00, 0xFF];
        CreateVersionWithExecutable("1.2.352.32", expectedContents);
        CreateVersionWithExecutable("2.0.577.98", [0x4D, 0x5A, 0x01]);
        using var factory = CreateFactory();
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/api/updates/1.2.352.32/download");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/octet-stream", response.Content.Headers.ContentType?.MediaType);
        var disposition = response.Content.Headers.ContentDisposition;
        Assert.NotNull(disposition);
        Assert.Equal("attachment", disposition.DispositionType);
        Assert.Equal("SampleApplication.exe", disposition.FileNameStar);
        Assert.Equal(expectedContents, await response.Content.ReadAsByteArrayAsync());
    }

    [Fact]
    public async Task Download_ReturnsNotFoundWhenVersionIsMissing()
    {
        CreateVersionWithExecutable("2.0.577.98", []);
        using var factory = CreateFactory();
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/api/updates/1.2.352.32/download");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Empty(await response.Content.ReadAsStringAsync());
    }

    [Theory]
    [InlineData("unknown")]
    [InlineData("2.0.577")]
    public async Task Download_ReturnsBadRequestForInvalidVersion(string version)
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();

        using var response = await client.GetAsync($"/api/updates/{version}/download");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
        var message = JsonSerializer.Deserialize<string>(await response.Content.ReadAsStringAsync());
        Assert.Equal("Die Version muss aus vier Zahlen bestehen, zum Beispiel 2.0.577.98.", message);
    }

    public void Dispose()
    {
        _root.Delete(recursive: true);
    }

    private void CreateVersionWithExecutable(string version, byte[] contents)
    {
        var directory = _root.CreateSubdirectory(version);
        File.WriteAllBytes(Path.Combine(directory.FullName, "SampleApplication.exe"), contents);
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
