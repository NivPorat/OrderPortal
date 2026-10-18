namespace OrderPortal.OrderService.Models
{
    public class Order
    {
        public string Id { get; set; } = string.Empty;
        public string ProductId { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public double TotalPrice { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
