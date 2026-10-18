using OrderPortal.OrderService.Interfaces;
using OrderPortal.OrderService.Models;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
namespace OrderPortal.OrderService.Services
{
    public class ProductClientService : IProductClientService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<ProductClientService> _logger;

        public ProductClientService(HttpClient httpClient, ILogger<ProductClientService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task<Product?> GetProductAsync(string productId, CancellationToken cancellationToken)
        {
            try
            {
                var response = await _httpClient.GetAsync($"api/product/{productId}", cancellationToken);

                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    return null;
                }

                response.EnsureSuccessStatusCode();

                var product = await response.Content.ReadFromJsonAsync<Product>(cancellationToken: cancellationToken);

                _logger.LogInformation("Product API call successful for product {ProductId}", productId);
                return product;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling ProductService for product {ProductId}", productId);
                throw; 
            }
        }
    }
}