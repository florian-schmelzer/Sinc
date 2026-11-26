using ApplicationUpdater.Service;
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

app.Run();
return 0;

public partial class Program { }
