namespace OrderPortal.OrderService.Models
{
    public class MessageQueueConfig
    {
       public string OrdersExchange {  get; set; } = string.Empty;
       public string OrdersRouteKey {  get; set; } = string.Empty;
    }
}
