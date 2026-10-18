namespace OrderPortal.OrderService.Models
{
    public class CreateOrderRequest
    {
        public string ProductId { get; set; } = string.Empty;
        public int Quantity { get; set; }
    }
}
