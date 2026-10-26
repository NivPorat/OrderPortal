namespace OrderPortal.OrderService.Models
{
    public record OrderCreatedEvent(
    string OrderId,
    string ProductId,
    int Quantity,
    double TotalPrice,
    DateTime CreatedAt
);
}
