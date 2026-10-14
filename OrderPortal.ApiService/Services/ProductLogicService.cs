using Microsoft.Extensions.Options;
using OrderPortal.ApiService.Controllers;
using OrderPortal.ApiService.Models;
using System.Threading;
using static OrderPortal.ApiService.Models.EnumDefinitions;

namespace OrderPortal.ApiService.Services
{
    public class ProductLogicService : IProductLogic
    {
        private readonly ProductSettingsConfig _settings;
        private readonly ILogger<ProductLogicService> _logger;
        private  readonly List<Product> _products = new List<Product>();
        private readonly int _maxProducts;
        public ProductLogicService(IOptions<ProductSettingsConfig> productSettings, ILogger<ProductLogicService> logger) 
        {
            _settings = productSettings.Value;
            _logger = logger;
            _maxProducts = int.Parse(_settings.MaxProducts);
        }
        public async Task<Product?> GetProduct(string id, CancellationToken cancellationToken)
        {
            // This safely awaits a zero-delay while listening for cancellation
            await Task.Delay(0, cancellationToken);

            var product = _products.FirstOrDefault(p => p.Id == id);
            if (product == null)
            {
                _logger.LogWarning("Product with ID {ProductId} not found.", id);
                return null;
            }

            _logger.LogInformation("Product with ID {ProductId} retrieved successfully.", id);
            return product;
        }

        public async Task<List<Product>> GetAllProducts(CancellationToken cancellationToken)
        {
            await Task.Delay(0, cancellationToken);

           _logger.LogInformation("Retrieving all products. Total count: {ProductCount}", _products.Count);
            return _products;
        }

        public async Task<EProductAdded> AddProduct(Product product, CancellationToken cancellationToken)
        {
            await Task.Delay(0, cancellationToken);
            EProductAdded rv = EProductAdded.PRODUCT_ADD_SUCCEEDED;
            if (_products.Count > _maxProducts)
            {
                _logger.LogWarning($"Product count cannot Exceed maximum {_maxProducts}.");
                rv = EProductAdded.PRODUCT_ADD_FAILED;
                return rv;
            }
            if (product == null || string.IsNullOrEmpty(product.Id))
            {
                _logger.LogWarning("Invalid product data received.");
                rv = EProductAdded.PRODUCT_ADD_FAILED;
                return rv;
            }
            if (_products.Any(p => p.Id == product.Id))
            {
                _logger.LogWarning("Product with ID {ProductId} already exists.", product.Id);
                rv = EProductAdded.PRODUCT_EXISTS;
                return rv;
            }
            _products.Add(product);
            _logger.LogInformation("Product with ID {ProductId} added successfully.", product.Id);
            return rv;
        }

    }
}
