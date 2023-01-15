using BetterFurniture.Models;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BetterFurniture.Controllers
{
    public class ProductController : Controller
    {
        public IActionResult ProductDetails(string furniture)
        {
            Console.WriteLine(furniture);
            Furniture passed_furniture = JsonConvert.DeserializeObject<Furniture>(furniture);
            return View(passed_furniture);
        }
    }
}
