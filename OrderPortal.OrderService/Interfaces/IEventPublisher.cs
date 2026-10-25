using OrderPortal.OrderService.Models;

namespace OrderPortal.OrderService.Interfaces;

public interface IEventPublisher
{
    Task PublishAsync<TEvent>(string exchange, string routeKey, TEvent eventData, CancellationToken cancellationToken = default) where TEvent : class;
}