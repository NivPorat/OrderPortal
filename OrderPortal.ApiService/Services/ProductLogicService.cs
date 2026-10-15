using Microsoft.EntityFrameworkCore;
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
        private readonly int _maxProducts;
        private readonly OrderDbContext _context;
        public ProductLogicService(IOptions<ProductSettingsConfig> productSettings, ILogger<ProductLogicService> logger, OrderDbContext dbset) 
        {
            _settings = productSettings.Value;
            _logger = logger;
            _maxProducts = int.Parse(_settings.MaxProducts);
            _context = dbset;
        }
        public async Task<Product?> GetProduct(string id, CancellationToken cancellationToken)
        {
            var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
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

           _logger.LogInformation("Retrieving all products. Total count: {ProductCount}",await _context.Products.CountAsync(cancellationToken));
            return await _context.Products.ToListAsync(cancellationToken);
        }

        public async Task<EProductAdded> AddProduct(Product product, CancellationToken cancellationToken)
        {
            EProductAdded rv = EProductAdded.PRODUCT_ADD_SUCCEEDED;
            if (await _context.Products.CountAsync(cancellationToken) > _maxProducts)
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
            if (await _context.Products.AnyAsync(p => p.Id == product.Id, cancellationToken))
            {
                _logger.LogWarning("Product with ID {ProductId} already exists.", product.Id);
                rv = EProductAdded.PRODUCT_EXISTS;
                return rv;
            }
            await _context.Products.AddAsync(product, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Product with ID {ProductId} added successfully.", product.Id);
            return rv;
        }

    }
}
