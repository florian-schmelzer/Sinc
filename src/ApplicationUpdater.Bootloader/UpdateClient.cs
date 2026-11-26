using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace ApplicationUpdater.Bootloader;

public sealed class UpdateClient(HttpClient httpClient)
{
    public async Task<UpdateVersion?> GetLatestVersionAsync(CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.GetAsync("api/updates/latest", cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        var latestVersion = await response.Content.ReadFromJsonAsync<UpdateVersion>(cancellationToken);
        if (latestVersion?.Version is null)
        {
            throw new JsonException("Die Antwort des UpdateService enthält keine Version.");
        }

        return latestVersion;
    }

    public async Task<string> DownloadAsync(Version version, string destinationDirectory, CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.GetAsync(
            $"api/updates/{version}/download", HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();

        var temporaryPath = Path.Combine(destinationDirectory, $"{Guid.NewGuid():N}.tmp");
        var file = new FileStream(temporaryPath, FileMode.CreateNew, FileAccess.Write, FileShare.None);
        try
        {
            await using (file)
            {
                await response.Content.CopyToAsync(file, cancellationToken);
                if (response.Content.Headers.ContentLength is long expectedLength && file.Length != expectedLength)
                {
                    throw new IOException($"Unvollständiger Download: {file.Length} von {expectedLength} Byte.");
                }
            }

            return temporaryPath;
        }
        catch
        {
            File.Delete(temporaryPath);
            throw;
        }
    }
}
