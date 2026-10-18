using OrderPortal.OrderService.Models;
namespace OrderPortal.OrderService.Interfaces
{
    public interface IProductClientService
    {
        public Task<Product?> GetProductAsync(string productId, CancellationToken cancellationToken);

    }
}
