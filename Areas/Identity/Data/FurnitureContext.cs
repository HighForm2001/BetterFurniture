using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
namespace BetterFurniture.Data
{
    public class FurnitureContext:DbContext
    {
        private readonly IConfiguration config;

        public FurnitureContext(DbContextOptions<FurnitureContext> options, IConfiguration config) : base(options)
        {
            this.config = config;
        }
        public DbSet<Models.Furniture> furnitures { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer(config.GetConnectionString("BetterFurnitureContextConnection"));
        }

    }
}
