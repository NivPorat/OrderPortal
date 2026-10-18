using OrderPortal.OrderService.Models;

namespace OrderPortal.OrderService.Interfaces
{
    public interface IOrderLogic
    {
        public Task<bool> ValidateOrderAsync(string productId, int quantity, CancellationToken cancellationToken);
        public Task<double> CalculateTotalPriceAsync(string productId, int quantity, CancellationToken cancellationToken);
        public Task<Order?> GetOrderByIdAsync(string orderId, CancellationToken cancellationToken);
        public Task<Order?> CreateOrderAsync(string productId, int quantity, CancellationToken cancellationToken);
    }
}
