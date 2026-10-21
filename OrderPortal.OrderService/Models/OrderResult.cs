using static OrderPortal.OrderService.Models.EnumDefinitions;

namespace OrderPortal.OrderService.Models
{
    public class OrderResult
    {
        public Order? Order { get; set; }
        public EOrderCreationStatus Status { get; set; }
    }
}
