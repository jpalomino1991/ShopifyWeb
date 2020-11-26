using Microsoft.EntityFrameworkCore;
using ShopifyWeb.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ShopifyWeb.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {

        }

        public DbSet<Orders> Orders { get; set; }
        public DbSet<Product> Product { get; set; }
    }
}
