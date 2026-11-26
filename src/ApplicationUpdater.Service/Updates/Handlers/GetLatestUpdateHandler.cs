using ApplicationUpdater.Service.Contracts;
using ApplicationUpdater.Service.Updates.Queries;
using MediatR;

namespace ApplicationUpdater.Service.Updates.Handlers;

internal sealed class GetLatestUpdateHandler(IUpdateService updateService)
    : IRequestHandler<GetLatestUpdateQuery, UpdateVersion?>
{
    public Task<UpdateVersion?> Handle(GetLatestUpdateQuery request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(updateService.GetLatestVersion());
    }
}
