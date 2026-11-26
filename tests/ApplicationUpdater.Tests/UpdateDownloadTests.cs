using System.IO.Abstractions.TestingHelpers;
using System.Net;
using ApplicationUpdater.Bootloader.Updates;
using Xunit;

namespace ApplicationUpdater.Tests;

public sealed class UpdateDownloadTests
{
    private const string InstalledContents = "installed version";
    private static readonly Version RequestedVersion = new(2, 0, 577, 98);
    private readonly MockFileSystem _fileSystem = new();
    private readonly string _installationDirectory;
    private string InstalledPath => _fileSystem.Path.Combine(_installationDirectory, "SampleApplication.exe");

    public UpdateDownloadTests()
    {
        _installationDirectory = _fileSystem.Path.GetFullPath("installation");
        _fileSystem.Directory.CreateDirectory(_installationDirectory);
        _fileSystem.File.WriteAllText(InstalledPath, InstalledContents);
    }

    [Fact]
    public async Task Download_WritesNewVersionToSeparateTemporaryFile()
    {
        byte[] contents = [0x4D, 0x5A, 0x00, 0xFF];
        using var httpClient = CreateHttpClient(new ByteArrayContent(contents));
        var client = new UpdateClient(httpClient, _fileSystem);

        var temporaryPath = await client.DownloadAsync(RequestedVersion, _installationDirectory);

        Assert.Equal(_installationDirectory, _fileSystem.Path.GetDirectoryName(temporaryPath));
        Assert.Equal(".tmp", _fileSystem.Path.GetExtension(temporaryPath));
        Assert.Equal(contents, _fileSystem.File.ReadAllBytes(temporaryPath));
        Assert.Equal(InstalledContents, _fileSystem.File.ReadAllText(InstalledPath));
    }

    [Fact]
    public async Task Download_RemovesPartialFileWhenTransferIsInterrupted()
    {
        using var httpClient = CreateHttpClient(new InterruptedContent());
        var client = new UpdateClient(httpClient, _fileSystem);

        await Assert.ThrowsAsync<HttpRequestException>(
            () => client.DownloadAsync(RequestedVersion, _installationDirectory));

        AssertOnlyInstalledFileRemains();
    }

    [Fact]
    public async Task Download_RejectsBodyShorterThanDeclaredLength()
    {
        var content = new ByteArrayContent([0x4D, 0x5A]);
        content.Headers.ContentLength = 10;
        using var httpClient = CreateHttpClient(content);
        var client = new UpdateClient(httpClient, _fileSystem);

        await Assert.ThrowsAsync<IOException>(() => client.DownloadAsync(RequestedVersion, _installationDirectory));

        AssertOnlyInstalledFileRemains();
    }

    private void AssertOnlyInstalledFileRemains()
    {
        var remainingFile = Assert.Single(_fileSystem.Directory.GetFiles(_installationDirectory));
        Assert.Equal(InstalledPath, remainingFile);
        Assert.Equal(InstalledContents, _fileSystem.File.ReadAllText(InstalledPath));
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
