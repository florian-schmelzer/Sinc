using System.Net;
using ApplicationUpdater.Bootloader.Updates;
using Xunit;

namespace ApplicationUpdater.Tests;

public sealed class UpdateDownloadTests : IDisposable
{
    private const string InstalledContents = "installed version";
    private static readonly Version RequestedVersion = new(2, 0, 577, 98);
    private readonly DirectoryInfo _installation = Directory.CreateTempSubdirectory("application-updater-tests-");
    private string InstalledPath => Path.Combine(_installation.FullName, "SampleApplication.exe");

    public UpdateDownloadTests()
    {
        File.WriteAllText(InstalledPath, InstalledContents);
    }

    [Fact]
    public async Task Download_WritesNewVersionToSeparateTemporaryFile()
    {
        byte[] contents = [0x4D, 0x5A, 0x00, 0xFF];
        using var httpClient = CreateHttpClient(new ByteArrayContent(contents));
        var client = new UpdateClient(httpClient);

        var temporaryPath = await client.DownloadAsync(RequestedVersion, _installation.FullName);

        Assert.Equal(_installation.FullName, Path.GetDirectoryName(temporaryPath));
        Assert.Equal(".tmp", Path.GetExtension(temporaryPath));
        Assert.Equal(contents, await File.ReadAllBytesAsync(temporaryPath));
        Assert.Equal(InstalledContents, await File.ReadAllTextAsync(InstalledPath));
    }

    [Fact]
    public async Task Download_RemovesPartialFileWhenTransferIsInterrupted()
    {
        using var httpClient = CreateHttpClient(new InterruptedContent());
        var client = new UpdateClient(httpClient);

        await Assert.ThrowsAsync<HttpRequestException>(
            () => client.DownloadAsync(RequestedVersion, _installation.FullName));

        AssertOnlyInstalledFileRemains();
    }

    [Fact]
    public async Task Download_RejectsBodyShorterThanDeclaredLength()
    {
        var content = new ByteArrayContent([0x4D, 0x5A]);
        content.Headers.ContentLength = 10;
        using var httpClient = CreateHttpClient(content);
        var client = new UpdateClient(httpClient);

        await Assert.ThrowsAsync<IOException>(() => client.DownloadAsync(RequestedVersion, _installation.FullName));

        AssertOnlyInstalledFileRemains();
    }

    public void Dispose()
    {
        _installation.Delete(recursive: true);
    }

    private void AssertOnlyInstalledFileRemains()
    {
        var remainingFile = Assert.Single(_installation.GetFiles());
        Assert.Equal(InstalledPath, remainingFile.FullName);
        Assert.Equal(InstalledContents, File.ReadAllText(InstalledPath));
    }

    private static HttpClient CreateHttpClient(HttpContent content)
    {
        return new HttpClient(new DownloadResponseHandler(content)) { BaseAddress = new Uri("http://localhost:5080/") };
    }

    private sealed class DownloadResponseHandler(HttpContent content) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Assert.Equal(HttpMethod.Get, request.Method);
            Assert.Equal($"/api/updates/{RequestedVersion}/download", request.RequestUri?.AbsolutePath);
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = content });
        }
    }

    private sealed class InterruptedContent : HttpContent
    {
        protected override async Task SerializeToStreamAsync(Stream stream, TransportContext? context)
        {
            await stream.WriteAsync(new byte[] { 0x4D, 0x5A });
            throw new IOException("Connection interrupted.");
        }

        protected override bool TryComputeLength(out long length)
        {
            length = 10;
            return true;
        }
    }
}
