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
}
