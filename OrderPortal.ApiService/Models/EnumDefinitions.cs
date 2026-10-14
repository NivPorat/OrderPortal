namespace OrderPortal.ApiService.Models
{
    public class EnumDefinitions
    {
        public enum EProductAdded
        {
            PRODUCT_EXISTS = -1,
            PRODUCT_ADD_FAILED = 0,
            PRODUCT_ADD_SUCCEEDED = 1,
        }
    }
}
