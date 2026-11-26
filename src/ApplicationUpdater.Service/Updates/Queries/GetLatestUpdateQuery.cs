using MediatR;

namespace ApplicationUpdater.Service.Updates.Queries;

internal sealed record GetLatestUpdateQuery : IRequest<UpdateVersion?>;
