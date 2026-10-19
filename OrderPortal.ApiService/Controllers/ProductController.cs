using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using OrderPortal.ApiService.Models;
using static OrderPortal.ApiService.Models.EnumDefinitions;

namespace OrderPortal.ApiService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductController : ControllerBase
    {
        private readonly ILogger<ProductController> _logger;
        private readonly IProductLogic _productLogic;

        public ProductController(ILogger<ProductController> logger, IProductLogic productLogic)
        {
            _logger = logger;
            _productLogic = productLogic;
        }

        [HttpGet("{id}", Name = "GetProductById")]
        public async Task<IActionResult> GetProductAsync(string id, CancellationToken cancellationToken)
        {
            var product = await _productLogic.GetProduct(id, cancellationToken);
            if (product is null)
            {
                return NotFound();
            }
            return Ok(product);
        }
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateProductAsync(string id, [FromBody] Product product, CancellationToken cancellationToken)
        {
            // Safety check: ensure the URL ID matches the body ID
            if (id != product.Id)
            {
                return BadRequest("The ID in the URL does not match the ID in the product body.");
            }

            bool updateSuccess = await _productLogic.UpdateProductAsync(product, cancellationToken);
            if (!updateSuccess)
            {
                return NotFound();
            }

            return Ok(product);
        }
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteProductAsync(string id, CancellationToken cancellationToken)
        {
            bool deletionSuccess = await _productLogic.DeleteProductAsync(id, cancellationToken);
            if (!deletionSuccess)
            {
                return NotFound();
            }

            return Ok();
        }

        [HttpGet]
        public async Task<IActionResult> GetAllProductsAsync(CancellationToken cancellationToken)
        {
            var products = await _productLogic.GetAllProducts(cancellationToken);
            return Ok(products);
        }
        [HttpPost]
        public async Task<IActionResult> AddProductAsync(Product product, CancellationToken cancellationToken)
        {
            
            EProductAdded retVal = await _productLogic.AddProduct(product, cancellationToken);

            if (retVal == EProductAdded.PRODUCT_ADD_FAILED)
            {
                return BadRequest("Invalid product data.");
            }
            if (retVal == EProductAdded.PRODUCT_EXISTS)
            {
                return Conflict("Product with the same ID already exists.");
            }

            return CreatedAtRoute("GetProductById", new { id = product.Id }, product);
        }
    }
}
