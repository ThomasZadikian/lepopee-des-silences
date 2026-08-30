using MediatR;

namespace Leds.Catalog.Application.Abstractions.Messaging;

public interface ICommand<out TResponse> : IRequest<TResponse>
{
}