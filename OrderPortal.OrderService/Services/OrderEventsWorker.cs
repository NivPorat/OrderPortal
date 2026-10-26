using RabbitMQ.Client;
using System.Text;
using System.Text.Json;
using OrderPortal.OrderService.Interfaces;
using OrderPortal.OrderService.Models;
using Microsoft.Extensions.Options;
using RabbitMQ.Client.Events;
using Microsoft.EntityFrameworkCore;

namespace OrderPortal.OrderService.Services
{
    public class OrderEventsWorker : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<OrderEventsWorker> _logger;
        private readonly MessageQueueConfig _messageQueueConfig;
        private readonly IConnection _connection;

        public OrderEventsWorker(IServiceScopeFactory scopeFactory, ILogger<OrderEventsWorker> logger,
            IOptions<MessageQueueConfig> messageQueueConfig, IConnection connection)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
            _messageQueueConfig = messageQueueConfig.Value;
            _connection = connection;
        }

        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            using IChannel channel = await InitializeTopologyAsync(cancellationToken);

            await StartConsumingAsync(channel, cancellationToken);

            var tcs = new TaskCompletionSource();
            using (cancellationToken.Register(tcs.SetResult))
            {
                await tcs.Task;
            }
        }

        private async Task StartConsumingAsync(IChannel channel, CancellationToken cancellationToken)
        {
            var consumer = new AsyncEventingBasicConsumer(channel);
            consumer.ReceivedAsync += async (model, ea) =>
            {
                try
                {
                    var body = ea.Body.ToArray();
                    var jsonMessage = Encoding.UTF8.GetString(body);
                    _logger.LogInformation($"Received message: {jsonMessage}");
                    var orderCreatedEvent = JsonSerializer.Deserialize<OrderCreatedEvent>(jsonMessage);
                    if (orderCreatedEvent != null)
                    {
                        using (var scope = _scopeFactory.CreateScope())
                        {
                            var orderDbContext = scope.ServiceProvider.GetRequiredService<OrderDbContext>();

                            bool isDuplicate = await orderDbContext.Orders.AnyAsync(o => o.Id == orderCreatedEvent.OrderId, cancellationToken);
                            if (isDuplicate)
                            {
                                _logger.LogWarning($"Duplicate order event received for OrderId: {orderCreatedEvent.OrderId}. Ignoring.");
                                await channel.BasicAckAsync(deliveryTag: ea.DeliveryTag, multiple: false, cancellationToken: cancellationToken);
                                return;
                            }
                            using var transaction = await orderDbContext.Database.BeginTransactionAsync();
                            try
                            {
                               if(await SaveTransactionToDB(orderCreatedEvent, orderDbContext, transaction))
                                {
                                    _logger.LogInformation($"Order {orderCreatedEvent.OrderId} processed and saved to database successfully.");
                                }

                                await channel.BasicAckAsync(ea.DeliveryTag, multiple: false);
                            }
                            catch (Exception ex)
                            {
                                await transaction.RollbackAsync();
                                _logger.LogError(ex, "Failed to process order {OrderId}", orderCreatedEvent.OrderId);
                                await channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: true);
                            }
                        }
                    }
                }

                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing consumed order event.");
                }
            };

            await channel.BasicConsumeAsync(
          queue: _messageQueueConfig.OrdersCreationQueue,
          autoAck: false,
          consumer: consumer,
          cancellationToken: cancellationToken);
        }

        private async Task<bool> SaveTransactionToDB(OrderCreatedEvent orderCreatedEvent, OrderDbContext orderDbContext, Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction transaction)
        {
            bool rv = true;
            try
            {
                var order = new Order
                {
                    Id = orderCreatedEvent.OrderId,
                    ProductId = orderCreatedEvent.ProductId,
                    CreatedAt = orderCreatedEvent.CreatedAt,
                    TotalPrice = orderCreatedEvent.TotalPrice
                };
                orderDbContext.Orders.Add(order);
                await orderDbContext.SaveChangesAsync();
                await transaction.CommitAsync();
                return rv;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Failed to save order {OrderId} to database", orderCreatedEvent.OrderId);
                return false;
            }
        }
        private async Task<IChannel> InitializeTopologyAsync(CancellationToken cancellationToken)
        {
            IChannel channel = await _connection.CreateChannelAsync(cancellationToken: cancellationToken);
            await channel.ExchangeDeclareAsync(exchange: _messageQueueConfig.OrdersExchange,
                type: ExchangeType.Topic,
                durable: true,
                cancellationToken: cancellationToken);
            await channel.QueueDeclareAsync(_messageQueueConfig.OrdersCreationQueue,
                durable: true,
                autoDelete: false,
                cancellationToken: cancellationToken);
            await channel.QueueBindAsync(_messageQueueConfig.OrdersCreationQueue,
                _messageQueueConfig.OrdersExchange,
                _messageQueueConfig.OrdersRouteKey,
                cancellationToken: cancellationToken);
            return channel;
        }
    }
}
