using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.EntityFrameworkCore;
using OrderPortal.OrderService.Interfaces;
using OrderPortal.OrderService.Models;

namespace OrderPortal.OrderService.Services
{
    public class OrderLogicService : IOrderLogic
    {
        private readonly int _maxProducts;
        private readonly OrderDbContext _context;
        private readonly ILogger<OrderLogicService> _logger;
        private readonly OrderSettingsConfig _settings;
        private readonly IProductClientService _productClientService;

        public OrderLogicService(
            IOptions<OrderSettingsConfig> productSettings,
            ILogger<OrderLogicService> logger,
            OrderDbContext dbset,
            IProductClientService productClientService)
        {
            _settings = productSettings.Value;
            _logger = logger;
            _context = dbset;
            _productClientService = productClientService;
            _maxProducts = int.Parse(_settings.MaxProducts);
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

        public async Task<bool> ValidateOrderAsync(string productId, int quantity, CancellationToken cancellationToken)
        {
            if (quantity <= 0 || quantity > _maxProducts)
            {
                _logger.LogWarning("Validation failed: Quantity {Quantity} is invalid or exceeds max limit {MaxProducts}", quantity, _maxProducts);
                return false;
            }

            var product = await _productClientService.GetProductAsync(productId, cancellationToken);
            if (product == null)
            {
                _logger.LogWarning("Validation failed: Product {ProductId} does not exist", productId);
                return false;
            }

            return true;
        }

        public async Task<Order?> CreateOrderAsync(string productId, int quantity, CancellationToken cancellationToken)
        {
            bool isValid = await ValidateOrderAsync(productId, quantity, cancellationToken);
            if (!isValid)
            {
                return null;
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

            _context.Orders.Add(order);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Order {OrderId} successfully saved to database.", order.Id);

            return order;
        }
    }
}