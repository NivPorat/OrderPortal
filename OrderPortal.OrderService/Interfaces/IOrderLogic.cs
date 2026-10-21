using OrderPortal.OrderService.Models;
using static OrderPortal.OrderService.Models.EnumDefinitions;

namespace OrderPortal.OrderService.Interfaces
{
    public interface IOrderLogic
    {
        public Task<EOrderCreationStatus> ValidateOrderAsync(string productId, int quantity, CancellationToken cancellationToken);
        public Task<double> CalculateTotalPriceAsync(string productId, int quantity, CancellationToken cancellationToken);
        public Task<Order?> GetOrderByIdAsync(string orderId, CancellationToken cancellationToken);
        public Task<OrderResult?> CreateOrderAsync(string productId, int quantity, CancellationToken cancellationToken);
    }
}
