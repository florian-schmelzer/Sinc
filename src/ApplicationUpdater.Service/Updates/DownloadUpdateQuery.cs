using ApplicationUpdater.Service.Contracts;
using MediatR;

namespace ApplicationUpdater.Service.Updates;

internal sealed record DownloadUpdateQuery(string? Version) : IRequest<DownloadUpdateResult>;

internal sealed record DownloadUpdateResult(Stream? Content, string? ValidationError = null);

internal sealed class DownloadUpdateHandler(IUpdateService updateService)
    : IRequestHandler<DownloadUpdateQuery, DownloadUpdateResult>
{
    public Task<DownloadUpdateResult> Handle(DownloadUpdateQuery request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!Version.TryParse(request.Version, out var version) || version.Revision < 0)
        {
            return Task.FromResult(new DownloadUpdateResult(
                Content: null,
                ValidationError: "Die Version muss aus vier Zahlen bestehen, zum Beispiel 2.0.577.98."));
        }

        return Task.FromResult(new DownloadUpdateResult(updateService.OpenDownload(version)));
    }
}
