using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using ShopifyWeb.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ShopifyWeb.Data
{
    public class ApplicationDbContext : IdentityDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {

        }

        public DbSet<Orders> Orders { get; set; }
        public DbSet<OrderStatus> OrderStatus { get; set; }
        public DbSet<Customer> Customer { get; set; }
        public DbSet<CustomerAddress> CustomerAddress { get; set; }
        public DbSet<Item> Item { get; set; }
        public DbSet<ShipAddress> ShipAddress { get; set; }
        public DbSet<BillAddress> BillAddress { get; set; }
        public DbSet<Logs> Logs { get; set; }
        public DbSet<Product> Product { get; set; }
        public DbSet<ProductImage> ProductImage { get; set; }
        public DbSet<ProductTempImage> ProductTempImage { get; set; }
        public DbSet<Web> Web { get; set; }
        public DbSet<Brand> Brand { get; set; }
        public DbSet<ProductType> ProductType { get; set; }
        public virtual DbSet<ProductKelly> ProductKelly { get; set; }
        public virtual DbSet<KellyChild> KellyChild { get; set; }
        public virtual DbSet<ProductDownload> ProductDownload { get; set; }
        public DbSet<ApplicationUser> ApplicationUsers { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder
                .Entity<ProductKelly>(eb =>
                {
                    eb.HasNoKey();
                    eb.ToTable("ProductKelly");
                })
                .Entity<KellyChild>(eb =>
                {
                    eb.HasNoKey();
                    eb.ToTable("KellyChild");
                })
                .Entity<ProductDownload>(eb =>
                {
                    eb.HasNoKey();
                    eb.ToTable("ProductDownload");
                });
        }
    }
}
