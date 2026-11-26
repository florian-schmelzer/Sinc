using ApplicationUpdater.Service.Contracts;
using MediatR;

namespace ApplicationUpdater.Service.Updates;

internal sealed record GetLatestUpdateQuery : IRequest<UpdateVersion?>;

internal sealed class GetLatestUpdateHandler(IUpdateService updateService)
    : IRequestHandler<GetLatestUpdateQuery, UpdateVersion?>
{
    public Task<UpdateVersion?> Handle(GetLatestUpdateQuery request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(updateService.GetLatestVersion());
    }
}
