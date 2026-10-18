using Microsoft.EntityFrameworkCore;
using OrderPortal.OrderService.Models;
using System.Collections.Generic;

namespace OrderPortal.OrderService.Models
{
    public class OrderDbContext : DbContext
    {
        public OrderDbContext(DbContextOptions<OrderDbContext> options) : base(options)
        {
        }

        public DbSet<Order> Orders { get; set; }
    }
}
