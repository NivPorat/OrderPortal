using Microsoft.AspNetCore.Mvc;
using OrderPortal.OrderService.Interfaces;
using OrderPortal.OrderService.Models;

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
            var order = await _orderLogic.CreateOrderAsync(request.ProductId, request.Quantity, cancellationToken);

            if (order == null)
            {
                return BadRequest("Invalid product or quantity limit exceeded.");
            }

            return CreatedAtAction(nameof(GetOrderById), new { id = order.Id }, order);
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
