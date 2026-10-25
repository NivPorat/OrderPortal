using OrderPortal.OrderService.Models;
using RabbitMQ.Client;
using System.Text;
using System.Text.Json;
using OrderPortal.OrderService.Interfaces;

namespace OrderPortal.OrderService.Services
{
    public class RabbitMqEventPublisher : IEventPublisher
    {
        private readonly IConnection _connection;
        private readonly ILogger<RabbitMqEventPublisher> _logger;
        public RabbitMqEventPublisher(IConnection connection, ILogger<RabbitMqEventPublisher> logger)
        {
            _connection = connection;
            _logger = logger;
        }

        public async Task PublishAsync<TEvent>(string exchange, string routeKey, TEvent eventData, CancellationToken cancellationToken = default) where TEvent : class
        {
            {
                using IChannel channel = await _connection.CreateChannelAsync(cancellationToken: cancellationToken);
                await channel.ExchangeDeclareAsync(
                exchange: exchange,
                type: ExchangeType.Topic,
                durable: true,
                cancellationToken: cancellationToken);

                _logger.LogInformation($"Publishing message to Exchange:{exchange}");
                string jsonMessage = JsonSerializer.Serialize(eventData);
                var body = Encoding.UTF8.GetBytes(jsonMessage);
                _logger.LogInformation($"Message body:{jsonMessage}");

                await channel.BasicPublishAsync(
                exchange: exchange,
                routingKey: routeKey,
                mandatory: false,
                body: body,
                cancellationToken: cancellationToken
            );


            }
        }
    }
}
