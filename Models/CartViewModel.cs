using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BetterFurniture.Models
{
    public class CartViewModel
    {
        public Cart Cart { get; set; }
        public List<Furniture> Furniture{ get; set; }
    }
}
