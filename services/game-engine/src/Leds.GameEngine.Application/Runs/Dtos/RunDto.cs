using Leds.GameEngine.Domain.Runs;

namespace Leds.GameEngine.Application.Runs.Dtos;

public sealed record RunDto(
    Guid Id,
    Guid PlayerId,
    string Seed,
    string GeneratorVersion,
    string MarkovMatrixVersion,
    string Status,
    int CurrentDepth,
    RoomDto CurrentRoom)
{
    public static RunDto FromDomain(Run run)
    {
        return new RunDto(
            run.Id.Value,
            run.PlayerId,
            run.Seed,
            run.GeneratorVersion,
            run.MarkovMatrixVersion,
            run.Status.ToString(),
            run.CurrentDepth,
            RoomDto.FromDomain(run.CurrentRoom));
    }
}