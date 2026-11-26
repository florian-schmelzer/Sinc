using ApplicationUpdater.Service;
using ApplicationUpdater.Service.Contracts;
using ApplicationUpdater.Service.Storage;

var builder = WebApplication.CreateBuilder(args);
try
{
    builder.Services.AddUpdateStorage(builder.Configuration, builder.Environment.ContentRootPath);
}
catch (InvalidOperationException exception)
{
    Console.Error.WriteLine(exception.Message);
    return 1;
}

builder.Services.AddSingleton<IUpdateService, UpdateService>();
var app = builder.Build();

app.MapGet("/", () => "UpdateService ist erreichbar.");

app.MapGet("/api/updates/latest", (IUpdateService updateService) =>
{
    var latestVersion = updateService.GetLatestVersion();
    return latestVersion is null ? Results.NotFound() : Results.Ok(latestVersion);
});

app.MapGet("/api/updates/{version}/download", (string version, IUpdateService updateService) =>
{
    if (!Version.TryParse(version, out var requestedVersion) || requestedVersion.Revision < 0)
    {
        return Results.BadRequest("Die Version muss aus vier Zahlen bestehen, zum Beispiel 2.0.577.98.");
    }

    var stream = updateService.OpenDownload(requestedVersion);
    return stream is null
        ? Results.NotFound()
        : Results.File(stream, "application/octet-stream", SampleApplication.FileName);
});

app.Run();
return 0;

public partial class Program { }
