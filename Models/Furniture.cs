using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BetterFurniture.Models
{
    public class Furniture
    {
        public int ID { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public decimal Price { get; set; }
        public int Quantity{ get; set; }
        public string ImageUrls { get; set; }
    }
}
