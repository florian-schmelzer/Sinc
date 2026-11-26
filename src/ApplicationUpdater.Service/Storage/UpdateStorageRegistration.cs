using ApplicationUpdater.Service.Contracts;

namespace ApplicationUpdater.Service.Storage;

public static class UpdateStorageRegistration
{
    public static void AddUpdateStorage(this IServiceCollection services, IConfiguration configuration, string contentRootPath)
    {
        var storage = configuration.GetSection("UpdateStorage");
        var provider = storage["Provider"];
        switch (provider)
        {
            case "FileSystem":
            {
                var rootPath = storage["RootPath"];
                if (string.IsNullOrWhiteSpace(rootPath))
                {
                    throw new InvalidOperationException("Der Pfad in 'UpdateStorage:RootPath' fehlt.");
                }

                var absoluteRootPath = Path.GetFullPath(rootPath, contentRootPath);
                services.AddSingleton<IUpdateStorage>(new FilesystemUpdateStorage(absoluteRootPath));

                // Alternativ:
                // services.AddSingleton(new FilesystemStorageOptions(absoluteRootPath));
                // services.AddSingleton<IUpdateStorage, FilesystemUpdateStorage>();
                return;
            }
            case "Database":
                throw new InvalidOperationException(
                    "Der Storage-Anbieter 'Database' ist noch nicht implementiert. Unterstützt wird 'FileSystem'.");

            default:
                throw new InvalidOperationException(
                    $"Unbekannter Storage-Anbieter '{provider ?? "(fehlt)"}' in 'UpdateStorage:Provider'. Unterstützt wird 'FileSystem'.");
        }
    }
}
