using BetterFurniture.Models;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BetterFurniture.Controllers
{
    public class PaymentController : Controller
    {
        private const string orderTable = "BetterFurnitureOrder";
        private const string cartTable = "BetterFurnitureCart";
        public IActionResult ProceedPayment(string customerName)
        {
            Console.WriteLine("Received customerName= " + customerName);
            return View();
        }
    }
}
