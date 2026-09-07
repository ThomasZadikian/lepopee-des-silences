using Leds.Catalog.Application.Abstractions.Messaging;
using MediatR;

namespace Leds.Catalog.Application.Archetypes;

public sealed record GetArchetypeDefinitionQuery(string Key) : IQuery<GetArchetypeDefinitionResponse>;
public sealed record GetArchetypeDefinitionResponse(ArchetypeDefinitionDto? Definition);
public sealed record ListArchetypeDefinitionsQuery : IQuery<ListArchetypeDefinitionsResponse>;
public sealed record ListArchetypeDefinitionsResponse(IReadOnlyCollection<ArchetypeDefinitionDto> Definitions);

public sealed class GetArchetypeDefinitionQueryHandler
    : IRequestHandler<GetArchetypeDefinitionQuery, GetArchetypeDefinitionResponse>
{
    private readonly IArchetypeDefinitionReadStore _store;
    public GetArchetypeDefinitionQueryHandler(IArchetypeDefinitionReadStore store) => _store = store;
    public async Task<GetArchetypeDefinitionResponse> Handle(GetArchetypeDefinitionQuery request, CancellationToken cancellationToken) =>
        new(await _store.GetByKeyAsync(request.Key, cancellationToken));
}

public sealed class ListArchetypeDefinitionsQueryHandler
    : IRequestHandler<ListArchetypeDefinitionsQuery, ListArchetypeDefinitionsResponse>
{
    private readonly IArchetypeDefinitionReadStore _store;
    public ListArchetypeDefinitionsQueryHandler(IArchetypeDefinitionReadStore store) => _store = store;
    public async Task<ListArchetypeDefinitionsResponse> Handle(ListArchetypeDefinitionsQuery request, CancellationToken cancellationToken) =>
        new(await _store.ListActiveAsync(cancellationToken));
}
