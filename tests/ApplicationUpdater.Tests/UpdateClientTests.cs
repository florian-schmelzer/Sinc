using System.Net;
using System.Net.Http.Json;
using ApplicationUpdater.Bootloader.Updates;
using Xunit;

namespace ApplicationUpdater.Tests;

public sealed class UpdateClientTests
{
    [Theory]
    [InlineData("3.0.0.0", true)]
    [InlineData("2.1.0.0", true)]
    [InlineData("2.0.578.0", true)]
    [InlineData("2.0.577.100", true)]
    [InlineData("2.0.577.99", false)]
    [InlineData("2.0.577.98", false)]
    public async Task LatestVersion_IndicatesWhetherAnUpdateIsNeeded(string serverVersion, bool updateExpected)
    {
        var installedVersion = new Version(2, 0, 577, 99);
        using var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(new { version = serverVersion, fileName = "SampleApplication.exe" })
        };
        using var httpClient = CreateHttpClient(response);
        var client = new UpdateClient(httpClient);

        var latestVersion = await client.GetLatestVersionAsync();

        Assert.NotNull(latestVersion);
        Assert.Equal(Version.Parse(serverVersion), latestVersion.Version);
        Assert.Equal("SampleApplication.exe", latestVersion.FileName);
        Assert.Equal(updateExpected, latestVersion.IsNewerThan(installedVersion));
    }

    [Fact]
    public async Task GetLatestVersion_ReturnsNullOnNotFound()
    {
        using var response = new HttpResponseMessage(HttpStatusCode.NotFound);
        using var httpClient = CreateHttpClient(response);
        var client = new UpdateClient(httpClient);

        Assert.Null(await client.GetLatestVersionAsync());
    }

    [Fact]
    public async Task GetLatestVersion_ReportsServerErrors()
    {
        using var response = new HttpResponseMessage(HttpStatusCode.InternalServerError);
        using var httpClient = CreateHttpClient(response);
        var client = new UpdateClient(httpClient);

        await Assert.ThrowsAsync<HttpRequestException>(() => client.GetLatestVersionAsync());
    }

    private static HttpClient CreateHttpClient(HttpResponseMessage response)
    {
        return new HttpClient(new LatestResponseHandler(response)) { BaseAddress = new Uri("http://localhost:5080/") };
    }

    private sealed class LatestResponseHandler(HttpResponseMessage response) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Assert.Equal(HttpMethod.Get, request.Method);
            Assert.Equal("http://localhost:5080/api/updates/latest", request.RequestUri?.AbsoluteUri);
            return Task.FromResult(response);
        }
    }
}
