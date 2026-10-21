namespace OrderPortal.OrderService.Models
{
    public class EnumDefinitions
    {
        public enum EOrderCreationStatus
        {
            Success,
            ValidationError,
            ProductNotFound,
            DependencyUnavailable
        }
    }
}
