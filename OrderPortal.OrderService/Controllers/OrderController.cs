using Microsoft.AspNetCore.Mvc;
using OrderPortal.OrderService.Interfaces;
using OrderPortal.OrderService.Models;
using static OrderPortal.OrderService.Models.EnumDefinitions;

namespace OrderPortal.OrderService.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class OrderController : ControllerBase
    {
       
        private readonly ILogger<OrderController> _logger;
        private readonly IOrderLogic _orderLogic;

        public OrderController(ILogger<OrderController> logger, IOrderLogic orderLogic)
        {
            _logger = logger;
            _orderLogic = orderLogic;
        }
        [HttpPost]
        public async Task<IActionResult> CreateOrder([FromBody] CreateOrderRequest request, CancellationToken cancellationToken)
        {

            var result = await _orderLogic.CreateOrderAsync(request.ProductId, request.Quantity, cancellationToken);
            return result!.Status switch
            {
                EOrderCreationStatus.Success => CreatedAtAction(nameof(GetOrderById), new { id = result.Order!.Id }, result.Order),
                EOrderCreationStatus.ValidationError => BadRequest(new { error = "Invalid quantity or input data." }), // 400
                EOrderCreationStatus.ProductNotFound => NotFound(new { error = "The referenced product does not exist." }), // 404
                EOrderCreationStatus.DependencyUnavailable => StatusCode(StatusCodes.Status503ServiceUnavailable, new { error = "Product service is currently unavailable." }), // 503
                _ => StatusCode(500)
            };
        }
        [HttpGet("{id}")]
        public async Task<IActionResult> GetOrderById(string id, CancellationToken cancellationToken)
        {
            var order = await _orderLogic.GetOrderByIdAsync(id, cancellationToken);
            if (order == null)
            {
                return NotFound();
            }

            return Ok(order);
        }
    }
}
