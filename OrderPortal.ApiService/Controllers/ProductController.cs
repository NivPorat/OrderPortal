using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using OrderPortal.ApiService.Models;

namespace OrderPortal.ApiService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductController : ControllerBase
    {
        private readonly ILogger<ProductController> _logger;
        private static readonly List<Product> _products = new List<Product>();
        public ProductController(ILogger logger)
        {
            _logger = logger as ILogger<ProductController> ?? throw new ArgumentNullException(nameof(logger));
        }

        [HttpGet("{id}")]
        public IActionResult GetProduct(string id)
        {
            var product = _products.FirstOrDefault(p => p.Id == id);
            if (product == null)
            {
                _logger.LogWarning("Product with ID {ProductId} not found.", id);
                return NotFound();
            }
            _logger.LogInformation("Product with ID {ProductId} retrieved successfully.", id);
            return Ok(product);
        }
        [HttpGet]
        public IActionResult GetAllProducts()
        {
            _logger.LogInformation("Retrieving all products. Total count: {ProductCount}", _products.Count);
            return Ok(_products);
        }

        [HttpPost]
        public IActionResult AddProduct(Product product)
        {
            if (product == null || string.IsNullOrEmpty(product.Id))
            {
                _logger.LogWarning("Invalid product data received.");
                return BadRequest("Invalid product data.");
            }
            if (_products.Any(p => p.Id == product.Id))
            {
                _logger.LogWarning("Product with ID {ProductId} already exists.", product.Id);
                return Conflict("Product with the same ID already exists.");
            }
            _products.Add(product);
            _logger.LogInformation("Product with ID {ProductId} added successfully.", product.Id);
            return CreatedAtAction(nameof(GetProduct), new { id = product.Id }, product);
        }
    }
}
