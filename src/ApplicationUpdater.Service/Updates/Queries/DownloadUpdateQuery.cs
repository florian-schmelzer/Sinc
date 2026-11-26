using MediatR;

namespace ApplicationUpdater.Service.Updates.Queries;

internal sealed record DownloadUpdateQuery(string? Version) : IRequest<DownloadUpdateResult>;

internal sealed record DownloadUpdateResult(Stream? Content, string? ValidationError = null);
