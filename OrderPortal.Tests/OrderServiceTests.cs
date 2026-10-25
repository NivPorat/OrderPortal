using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using OrderPortal.OrderService.Interfaces;
using OrderPortal.OrderService.Models;
using OrderPortal.OrderService.Services;
using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using static OrderPortal.OrderService.Models.EnumDefinitions;

namespace OrderPortal.Tests
{
    public class OrderServiceTests
    {
        private class DownstreamServiceDownClient : IProductClientService
        {
            public Task<Product?> GetProductAsync(string productId, CancellationToken cancellationToken)
            {
                return Task.FromException<Product?>(new HttpRequestException("ProductService is offline."));
            }
        }

        // Add a test double for the event publisher so the test doesn't need a real RabbitMQ connection
        private class DummyEventPublisher : IEventPublisher
        {
            public Task PublishAsync<TEvent>(string exchange, string routeKey, TEvent eventData, CancellationToken cancellationToken = default) where TEvent : class
            {
                return Task.CompletedTask;
            }
        }

        [Fact]
        public async Task CreateOrderAsync_WhenProductServiceIsDown_ReturnsDependencyUnavailable()
        {
            var options = new DbContextOptionsBuilder<OrderDbContext>()
                .UseInMemoryDatabase(databaseName: "TestOrderDb_" + Guid.NewGuid())
                .Options;

            var dbContext = new OrderDbContext(options);
            var settings = Options.Create(new OrderSettingsConfig { MaxProducts = "5" });
            var messageQueueSettings = Options.Create(new MessageQueueConfig { OrdersExchange = "orders-exchange", OrdersRouteKey = "order.created" });
            var logger = NullLogger<OrderLogicService>.Instance;

            var failingClient = new DownstreamServiceDownClient();
            var eventPublisher = new DummyEventPublisher(); // <-- Added publisher dependency

            // Pass the event publisher into the OrderLogicService constructor
            var service = new OrderLogicService(settings, logger, dbContext, failingClient, eventPublisher,messageQueueSettings);

            // Act - call the service and check that our error mapping catches the dependency failure
            var result = await service.CreateOrderAsync("prod-1", 2, CancellationToken.None);

            // Assert - verify it mapped to DependencyUnavailable (503) instead of crashing
            Assert.Equal(EOrderCreationStatus.DependencyUnavailable, result.Status);
        }
    }
}