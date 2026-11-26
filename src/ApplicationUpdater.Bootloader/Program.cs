using System.Diagnostics;
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
return 0;
