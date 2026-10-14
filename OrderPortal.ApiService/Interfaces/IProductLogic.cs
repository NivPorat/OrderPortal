using Microsoft.AspNetCore.Mvc;
using OrderPortal.ApiService.Models;
using static OrderPortal.ApiService.Models.EnumDefinitions;
namespace OrderPortal.ApiService
{
    public interface IProductLogic
    {
        Task <Product?> GetProduct(string id, CancellationToken cancellationToken);
        Task<EProductAdded> AddProduct(Product product, CancellationToken cancellationToken);
        Task <List<Product>> GetAllProducts(CancellationToken cancellationToken);
    }
}
