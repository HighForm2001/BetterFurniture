using System;
using BetterFurniture.Areas.Identity.Data;
using BetterFurniture.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

[assembly: HostingStartup(typeof(BetterFurniture.Areas.Identity.IdentityHostingStartup))]
namespace BetterFurniture.Areas.Identity
{
    public class IdentityHostingStartup : IHostingStartup
    {
        public void Configure(IWebHostBuilder builder)
        {
            builder.ConfigureServices((context, services) => {
                services.AddDbContext<BetterFurnitureContext>(options =>
                    options.UseSqlServer(
                        context.Configuration.GetConnectionString("BetterFurnitureContextConnection")));

                services.AddDefaultIdentity<BetterFurnitureUser>(options => options.SignIn.RequireConfirmedAccount = true)
                    .AddEntityFrameworkStores<BetterFurnitureContext>();
            });
        }
    }
}