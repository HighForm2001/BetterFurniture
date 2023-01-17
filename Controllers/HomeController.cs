using BetterFurniture.Models;
using BetterFurniture.Models.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace BetterFurniture.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly FurnitureRepository _repository;

        public HomeController(ILogger<HomeController> logger, FurnitureRepository repository)
        {
            _logger = logger;
            _repository = repository;
        }

        public IActionResult Index(List<Furniture>? searched_furniture)
        {
            if (searched_furniture.Count() != 0)
            {
                return View(searched_furniture);
            }
            List<Furniture> furnitures = _repository.GetAll();
            return View(furnitures);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
        
        public IActionResult Search(string query)
        {
            if (query == null)
            {
                var results = _repository.GetAll();
                return View("Index",results);
            }
            else
            {
                var results = _repository.GetAll().Where(f => f.Name.ToLower().Contains(query.ToLower())).ToList();
                
                if (results.Count == 0) {
                    TempData["msg"] = "No result found for this term: " + query;
                    TempData.Keep("msg");
                    Console.WriteLine("results == null");
                }
                return View("Index", results);
            }
            
        }
    }
}
