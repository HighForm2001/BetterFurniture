using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BetterFurniture.Models
{
    public class Cart
    {
        public string CustomerName { get; set; }
        public string CustomerId { get; set; }
        public List<string> ItemName { get; set; }
        public decimal total_price { get; set; }
        
    }
}
