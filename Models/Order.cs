using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BetterFurniture.Models
{
    public class Order
    {
        public string OrderID { get; set; }
        public string CustomerID { get; set; }
        public string CustomerName { get; set; }
        public string ShippingAddress { get; set; }
        public string CustomerPhone { get; set; }
        public string CustomerEmail { get; set; }
        public List<string> ItemName { get; set; }
        public string Status { get; set; }
        public decimal TotalPrice { get; set; }
        public IEnumerable<SelectListItem> ItemNameList
        {
            get { return ItemName.Select(i => new SelectListItem { Text = i, Value = i }); }
        }
    }
}
