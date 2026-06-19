using Leds.Catalog.Application.RewardTemplates.Ports;
using MediatR;

namespace Leds.Catalog.Application.RewardTemplates.GetRewardTemplateByKey;

public sealed class GetRewardTemplateByKeyQueryHandler
    : IRequestHandler<GetRewardTemplateByKeyQuery, GetRewardTemplateByKeyResponse>
{
    private readonly IRewardTemplateReadStore _readStore;

    public GetRewardTemplateByKeyQueryHandler(IRewardTemplateReadStore readStore)
    {
        _readStore = readStore;
    }

    public async Task<GetRewardTemplateByKeyResponse> Handle(
        GetRewardTemplateByKeyQuery request,
        CancellationToken cancellationToken)
    {
        return new GetRewardTemplateByKeyResponse(
            await _readStore.GetDtoByKeyAsync(request.Key, cancellationToken));
    }
}
