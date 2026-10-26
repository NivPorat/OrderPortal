using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OrderPortal.OrderService.Interfaces;
using OrderPortal.OrderService.Models;
using static OrderPortal.OrderService.Models.EnumDefinitions;

namespace OrderPortal.OrderService.Services
{
    public class OrderLogicService : IOrderLogic
    {
        private readonly int _maxProducts;
        private readonly OrderDbContext _context;
        private readonly ILogger<OrderLogicService> _logger;
        private readonly OrderSettingsConfig _settings;
        private readonly IProductClientService _productClientService;
        private readonly IEventPublisher _eventPublisher;
        private readonly MessageQueueConfig _messageQueueSettings;

        public OrderLogicService(
            IOptions<OrderSettingsConfig> productSettings,
            ILogger<OrderLogicService> logger,
            OrderDbContext dbset,
            IProductClientService productClientService,
            IEventPublisher eventPublisher,
            IOptions<MessageQueueConfig> messageQueueSettings)
        {
            _settings = productSettings.Value;
            _logger = logger;
            _context = dbset;
            _productClientService = productClientService;
            _maxProducts = int.Parse(_settings.MaxProducts);
            _eventPublisher = eventPublisher;
            _messageQueueSettings = messageQueueSettings.Value;
        }

        public async Task<double> CalculateTotalPriceAsync(string productId, int quantity, CancellationToken cancellationToken)
        {
            var product = await _productClientService.GetProductAsync(productId, cancellationToken);
            if (product == null)
            {
                _logger.LogError("Product {ProductId} does not exist", productId);
                return 0.0;
            }
            return product.Price * quantity;
        }

        public async Task<Order?> GetOrderByIdAsync(string orderId, CancellationToken cancellationToken)
        {
            var order = await _context.Orders.FirstOrDefaultAsync(o => o.Id == orderId, cancellationToken);

            if (order == null)
            {
                _logger.LogWarning("Order {OrderId} not found in database", orderId);
            }

            return order;
        }

        public async Task<EOrderCreationStatus> ValidateOrderAsync(string productId, int quantity, CancellationToken cancellationToken)
        {
            if (quantity <= 0 || quantity > _maxProducts)
            {
                _logger.LogWarning("Validation failed: Quantity {Quantity} is invalid or exceeds max limit {MaxProducts}", quantity, _maxProducts);
                return EOrderCreationStatus.ValidationError;
            }

            try
            {
                var product = await _productClientService.GetProductAsync(productId, cancellationToken);
                if (product == null)
                {
                    _logger.LogWarning("Validation failed: Product {ProductId} does not exist", productId);
                    return EOrderCreationStatus.ProductNotFound;
                }
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Dependency failure: Unable to reach ProductService.");
                return EOrderCreationStatus.DependencyUnavailable; // Maps to 503
            }
            catch (TaskCanceledException ex)
            {
                _logger.LogError(ex, "Dependency timeout: ProductService took too long to respond.");
                return EOrderCreationStatus.DependencyUnavailable; // Maps to 503 (or Gateway Timeout)
            }

            return EOrderCreationStatus.Success;
        }

        public async Task<OrderResult?> CreateOrderAsync(string productId, int quantity, CancellationToken cancellationToken)
        {
            var validationResult = await ValidateOrderAsync(productId, quantity, cancellationToken);
            if (validationResult != EOrderCreationStatus.Success)
            {
                return new OrderResult { Status = validationResult, Order = null };
            }


            double totalPrice = await CalculateTotalPriceAsync(productId, quantity, cancellationToken);

            var order = new Order
            {
                Id = Guid.NewGuid().ToString(), // Generate unique ID for this order
                ProductId = productId,
                Quantity = quantity,
                TotalPrice = totalPrice,
                CreatedAt = DateTime.UtcNow
            };
            var OrderCreationEvent = new OrderCreatedEvent(
            order.Id,
            order.ProductId,
            order.Quantity,
            order.TotalPrice,
            order.CreatedAt
        );

            await _eventPublisher.PublishAsync(_messageQueueSettings.OrdersExchange, _messageQueueSettings.OrdersRouteKey, OrderCreationEvent, cancellationToken);
            _logger.LogInformation("Published OrderCreated event for Order {OrderId}", order.Id);

            return new OrderResult { Status = EOrderCreationStatus.Success, Order = order };
        }
    }
}