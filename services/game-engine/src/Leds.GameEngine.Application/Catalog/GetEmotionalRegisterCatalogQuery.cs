using Leds.GameEngine.Application.Catalog.Contracts;
using MediatR;

namespace Leds.GameEngine.Application.Catalog;

public sealed record GetEmotionalRegisterCatalogQuery
    : IRequest<CatalogEmotionalRegisterCatalog>;
