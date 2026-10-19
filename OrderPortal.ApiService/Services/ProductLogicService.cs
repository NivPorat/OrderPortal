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
        private readonly CachingService _cachingService;
        public ProductLogicService(IOptions<ProductSettingsConfig> productSettings, ILogger<ProductLogicService> logger,
            OrderDbContext dbset, CachingService cachingService)
        {
            _settings = productSettings.Value;
            _logger = logger;
            _maxProducts = int.Parse(_settings.MaxProducts);
            _context = dbset;
            _cachingService = cachingService;
        }
        public async Task<Product?> GetProduct(string id, CancellationToken cancellationToken)
        {
            var cachedProduct = await _cachingService.GetCachedKeyAsync(id);
            if (cachedProduct != null)
            {
                return cachedProduct;
            }

            var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
            if (product == null)
            {
                _logger.LogWarning("Product with ID {ProductId} not found.", id);
                return null;
            }

            await _cachingService.SetCachedKeyAsync(id, product);

            _logger.LogInformation("Product with ID {ProductId} retrieved from DB and cached.", id);
            return product;
        }

        public async Task<List<Product>> GetAllProducts(CancellationToken cancellationToken)
        {

            _logger.LogInformation("Retrieving all products. Total count: {ProductCount}", await _context.Products.CountAsync(cancellationToken));
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
        public async Task<bool> UpdateProductAsync(Product product, CancellationToken cancellationToken)
        {
            // Find the entity with tracking enabled so EF Core knows about it
            var existingProduct = await _context.Products.FirstOrDefaultAsync(p => p.Id == product.Id, cancellationToken);

            try
            {
                if (existingProduct != null)
                {
                    _context.Entry(existingProduct).CurrentValues.SetValues(product);
                }
                else
                {
                    _logger.LogInformation($"Product: {product.Name} | Product ID:{product.Id} does not exist. Creating new product");
                    await _context.Products.AddAsync(product, cancellationToken);
                }

                await _context.SaveChangesAsync(cancellationToken);
                await _cachingService.InvalidateKeyAsync(product.Id);

                _logger.LogInformation($"Product ID:{product.Id} has been successfully updated/created.");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Exception during update/upsert: {ex}");
                return false;
            }
        }

        public async Task<bool> DeleteProductAsync(string id, CancellationToken cancellationToken)
        {
            Product? existingProduct = await GetProduct(id, cancellationToken);
            if (existingProduct != null)
            {
                _logger.LogInformation($"Attempting to remove Product: {existingProduct.Name} | Product ID:{existingProduct.Id}");
                try
                {
                    _context.Products.Remove(existingProduct);
                    await _context.SaveChangesAsync(cancellationToken);
                    await _cachingService.InvalidateKeyAsync(id);
                    _logger.LogInformation($"Product: {existingProduct.Name} | Product ID:{existingProduct.Id} Has been removed");
                    return true;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex.Message );
                    return false;
                }
            }
            _logger.LogInformation($"Product ID:{id} does not exist. removal failed");
            return false;
        }
    }
}
