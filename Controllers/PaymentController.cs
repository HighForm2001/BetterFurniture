using BetterFurniture.Models;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using BetterFurniture.Models.Repositories;
using Amazon.DynamoDBv2;
using Microsoft.Extensions.Configuration;
using System.IO;
using Amazon;
using Amazon.DynamoDBv2.Model;
using BetterFurniture.Areas.Identity.Data;

namespace BetterFurniture.Controllers
{
    public class PaymentController : Controller
    {
        private const string orderTable = "BetterFurnitureOrder";
        private const string cartTable = "BetterFurnitureCart";
        private readonly FurnitureRepository _repository;

        // inject connection to the database
        public PaymentController(FurnitureRepository repository)
        {
            _repository = repository;
        }

        public IActionResult ProceedPayment(string cart)
        {
            Cart cart_to_proceed = JsonConvert.DeserializeObject<Cart>(cart);
            List<Furniture> all_furnitures = _repository.GetAll();
            List<Furniture> selected_furnitures = new List<Furniture>();
            ViewBag.total_price = cart_to_proceed.total_price;
            ViewBag.customerName = cart_to_proceed.CustomerName;
            foreach(var item_name in cart_to_proceed.ItemName)
            {
                selected_furnitures.Add(all_furnitures.Find(x => x.Name.Equals(item_name)));
            }
            return View(selected_furnitures);
        }
        
        public IActionResult FinishPayment(string furnitures, string customerName)
        {
            List<Furniture> paid_furnitures = JsonConvert.DeserializeObject<List<Furniture>>(furnitures);
            ViewBag.Msg = createOrder(paid_furnitures);
            return View();
        }
        public async Task<string> createOrder(List<Furniture> furnitures, BetterFurnitureUser user)
        {
            var client = connect();
            //create unique id 
            string uniqueId = Guid.NewGuid().ToString();
            try
            {
                Dictionary<string, AttributeValue> item = new Dictionary<string, AttributeValue>
                {
                    { "OrderID",new AttributeValue{S=uniqueId} },
                    {"CustomerName",new AttributeValue{S=user.CustomerFullName} },
                    { "ShippingAddress",new AttributeValue{S=user.CustomerAddress } },
                    { "CustomerEmail",new AttributeValue{S="Sample123@email.com" } }, // need to edit
                    { "CustomerPhone",new AttributeValue{S="0123456789" } }, // need to edit
                    { "Status",new AttributeValue{S="Packed" } },
                    { "ItemName",new AttributeValue{L=furnitures.Select(x=>new AttributeValue{S=x.Name }).ToList() } }
                };
                // List<string> test = item["ItemName"].L.Select(av => av.S).ToList();

                PutItemRequest request = new PutItemRequest
                {
                    TableName = orderTable,
                    Item = item
                };
                await client.PutItemAsync(request);
                foreach(var furniture in furnitures)
                {
                    furniture.Quantity -= 1;
                    _repository.Update(furniture);
                }
            }
            catch (AmazonDynamoDBException ex)
            {
                return "Error: " + ex.Message;
            }
            return "Created Succesfully";
        }

        // DynamoDBClient
        private AmazonDynamoDBClient connect()
        {
            List<string> keys = new List<string>();
            var builder = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory()).AddJsonFile("appsettings.json");
            IConfiguration config = builder.Build();

            keys.Add(config["AWS:id"]);
            keys.Add(config["AWS:key"]);
            keys.Add(config["AWS:token"]);
            AmazonDynamoDBClient client = new AmazonDynamoDBClient(keys[0], keys[1], keys[2], RegionEndpoint.USEast1);
            return client;
        }
    }
}
