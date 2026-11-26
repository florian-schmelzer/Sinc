using ApplicationUpdater.Service.Contracts;
using ApplicationUpdater.Service.Storage;
using ApplicationUpdater.Service.Updates;

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
builder.Services.AddMediatR(options => options.RegisterServicesFromAssemblyContaining<Program>());
builder.Services.AddControllers()
    .ConfigureApiBehaviorOptions(options => options.SuppressMapClientErrors = true);
var app = builder.Build();

app.MapControllers();
app.Run();

return 0;

public partial class Program { }
