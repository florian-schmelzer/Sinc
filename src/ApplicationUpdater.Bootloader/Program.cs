using System.Diagnostics;
using System.Text.Json;
using ApplicationUpdater.Bootloader;
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

if (!Uri.TryCreate(configuration["UpdateService:BaseUrl"], UriKind.Absolute, out var serviceAddress) ||
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
Console.WriteLine(latestVersion.IsNewerThan(installedVersion) ? "Update verfügbar." : "Kein Update erforderlich.");
return 0;
