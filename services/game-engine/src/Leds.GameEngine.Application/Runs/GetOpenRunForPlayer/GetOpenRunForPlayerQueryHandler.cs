using Leds.GameEngine.Application.Abstractions;
using Leds.GameEngine.Application.Runs.Dtos;
using MediatR;

namespace Leds.GameEngine.Application.Runs.GetOpenRunForPlayer;

public sealed class GetOpenRunForPlayerQueryHandler
    : IRequestHandler<GetOpenRunForPlayerQuery, GetOpenRunForPlayerResponse>
{
    private readonly IRunRepository _runRepository;

    public GetOpenRunForPlayerQueryHandler(IRunRepository runRepository)
    {
        _runRepository = runRepository;
    }

    public async Task<GetOpenRunForPlayerResponse> Handle(
        GetOpenRunForPlayerQuery request,
        CancellationToken cancellationToken)
    {
        var run = await _runRepository.GetOpenByPlayerIdAsync(
            request.PlayerId,
            cancellationToken);

        return new GetOpenRunForPlayerResponse(
            run is null ? null : RunDto.FromDomain(run));
    }
}
