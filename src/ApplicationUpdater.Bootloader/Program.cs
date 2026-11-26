using System.ComponentModel;
using System.Diagnostics;
using System.Text.Json;
using ApplicationUpdater.Bootloader.Updates;
using Microsoft.Extensions.Configuration;

var configuration = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json")
    .AddCommandLine(args)
    .Build();

var configuredPath = configuration["ApplicationPath"];

if (string.IsNullOrWhiteSpace(configuredPath))
{
    Console.Error.WriteLine("Der Pfad in 'ApplicationPath' fehlt.");
    return 1;
}

var applicationPath = Path.GetFullPath(configuredPath, AppContext.BaseDirectory);
FileVersionInfo versionInfo;
try
{
    versionInfo = FileVersionInfo.GetVersionInfo(applicationPath);
}
catch (FileNotFoundException)
{
    Console.Error.WriteLine($"Die SampleApplication wurde nicht gefunden: {applicationPath}");
    return 1;
}

if (string.IsNullOrWhiteSpace(versionInfo.FileVersion))
{
    Console.Error.WriteLine($"Die Datei enthält keine lesbare Versionsinformation: {applicationPath}");
    return 1;
}

var installedVersion = new Version(
    versionInfo.FileMajorPart,
    versionInfo.FileMinorPart,
    versionInfo.FileBuildPart,
    versionInfo.FilePrivatePart);

Console.WriteLine($"Installierte Version: {installedVersion}");

var updateExitCode = await UpdateApplicationAsync(configuration["UpdateService:BaseUrl"], applicationPath, installedVersion);
if (updateExitCode != 0)
{
    Console.WriteLine("Die vorhandene Installation wird gestartet.");
}

try
{
    using var application = Process.Start(new ProcessStartInfo
    {
        FileName = applicationPath,
        WorkingDirectory = Path.GetDirectoryName(applicationPath)!,
        UseShellExecute = true
    });
    Console.WriteLine("SampleApplication gestartet.");
}
catch (Win32Exception exception)
{
    Console.Error.WriteLine($"SampleApplication konnte nicht gestartet werden: {exception.Message}");
    return 1;
}

return updateExitCode;

static async Task<int> UpdateApplicationAsync(string? serviceUrl, string applicationPath, Version installedVersion)
{
    if (!Uri.TryCreate(serviceUrl, UriKind.Absolute, out var serviceAddress) ||
        serviceAddress.Scheme is not ("http" or "https"))
    {
        Console.Error.WriteLine("In 'UpdateService:BaseUrl' muss eine HTTP- oder HTTPS-Adresse stehen.");
        return 1;
    }

    using var httpClient = new HttpClient { BaseAddress = serviceAddress };
    var updateClient = new UpdateClient(httpClient);
    UpdateVersion? latestVersion;
    try
    {
        latestVersion = await updateClient.GetLatestVersionAsync();
    }
    catch (Exception exception) when (exception is HttpRequestException or JsonException or TaskCanceledException)
    {
        Console.Error.WriteLine($"Versionsabfrage fehlgeschlagen: {exception.Message}");
        return 1;
    }

    if (latestVersion is null)
    {
        Console.WriteLine("Der UpdateService stellt derzeit keine Version bereit.");
        return 0;
    }

    Console.WriteLine($"Serverversion: {latestVersion.Version}");
    if (!latestVersion.IsNewerThan(installedVersion))
    {
        Console.WriteLine("Kein Update erforderlich.");
        return 0;
    }

    Console.WriteLine("Update verfügbar. Download wird gestartet.");
    string temporaryPath;
    try
    {
        var installationDirectory = Path.GetDirectoryName(applicationPath)!;
        temporaryPath = await updateClient.DownloadAsync(latestVersion.Version, installationDirectory);
    }
    catch (Exception exception) when (exception is HttpRequestException or IOException or UnauthorizedAccessException or OperationCanceledException)
    {
        Console.Error.WriteLine($"Download fehlgeschlagen: {exception.Message}");
        return 1;
    }

    Console.WriteLine("Download abgeschlossen. Update wird installiert.");
    try
    {
        File.Move(temporaryPath, applicationPath, overwrite: true);
    }
    catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
    {
        Console.Error.WriteLine($"Installation fehlgeschlagen: {exception.Message}");
        Console.Error.WriteLine($"Der vollständige Download liegt unter: {temporaryPath}");
        return 1;
    }

    Console.WriteLine($"Update installiert: {latestVersion.Version}");
    return 0;
}
