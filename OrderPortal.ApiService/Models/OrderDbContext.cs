using Microsoft.EntityFrameworkCore;
using OrderPortal.ApiService.Models;

namespace OrderPortal.ApiService.Models
{
    public class OrderDbContext : DbContext
    {
        public OrderDbContext(DbContextOptions<OrderDbContext> options) : base(options)
        {
        }

        public DbSet<Product> Products { get; set; }
    }
}
