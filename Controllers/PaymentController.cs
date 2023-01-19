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
using Microsoft.AspNetCore.Authentication;
using System.IO;
using Amazon;
using Amazon.DynamoDBv2.Model;
using BetterFurniture.Areas.Identity.Data;
using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;

namespace BetterFurniture.Controllers
{
    public class PaymentController : Controller
    {
        private const string orderTable = "BetterFurnitureOrder";
        private const string cartTable = "BetterFurnitureCart";
        private readonly FurnitureRepository _repository;
        private readonly UserManager<BetterFurnitureUser> _userManager;

        // inject connection to the database
        public PaymentController(FurnitureRepository repository, UserManager<BetterFurnitureUser> userManager)
        {
            _repository = repository;
            _userManager = userManager;
        }

        // view
        public IActionResult ProceedPayment(string cart)
        {
            Cart cart_to_proceed = JsonConvert.DeserializeObject<Cart>(cart);
            bool isExceeded = false;
            string paymentError = "The following furniture is out of stock: |";
            List<Furniture> all_furnitures = _repository.GetAll();
            List<Furniture> selected_furnitures = new List<Furniture>();
            ViewBag.total_price = cart_to_proceed.total_price;

            foreach(var item_name in cart_to_proceed.ItemName)
            {
                selected_furnitures.Add(all_furnitures.Find(x => x.Name.Equals(item_name)));
            }
            var furnitureCount = selected_furnitures.GroupBy(f => f.Name)
                                .Select(g => new { FurnitureName = g.Key, Quantity = g.Count() })
                                .ToList();
            foreach (var furniture_item in furnitureCount)
            {
                var furniture = _repository.GetByName(furniture_item.FurnitureName);
                int originalQuantity = furniture.Quantity;
                furniture.Quantity -= furniture_item.Quantity;
                if (furniture.Quantity < 0)
                {
                    isExceeded = true;
                    paymentError += furniture.Name + "|Required: " + furniture_item.Quantity + "|Stock(s) left: " + originalQuantity + "|";
                }
            }
            if (isExceeded)
            {
                TempData["no_stock"] = paymentError;
                return RedirectToAction("CartPage", "Cart");
            }
            return View(selected_furnitures);
        }
        
        public async Task<IActionResult> FinishPayment(string furnitures, string total)
        {
            List<Furniture> paid_furnitures = JsonConvert.DeserializeObject<List<Furniture>>(furnitures);
            BetterFurnitureUser user = await _userManager.GetUserAsync(HttpContext.User);
            string order_id = await createOrder(paid_furnitures, user, decimal.Parse(total));
            Console.WriteLine(order_id);
            Order order = await getOrder(order_id);
            string result = await checkFurnitureQuantity();
            Console.WriteLine(result);
            return View(order);
        }

        // functions
        public async Task<Order> getOrder(string orderID)
        {
            var client = connectDynamoDb();
            List<Order> Orders = new List<Order>();
            try
            {
                ScanRequest request = new ScanRequest
                {
                    TableName = orderTable
                };

                ScanResponse response = await client.ScanAsync(request);

                foreach (var item in response.Items)
                {
                    Order order = new Order();
                    order.OrderID = item["OrderID"].S;
                    order.CustomerEmail = item["CustomerEmail"].S;
                    order.CustomerName = item["CustomerName"].S;
                    order.CustomerPhone = item["CustomerPhone"].S;
                    order.ItemName = item["ItemName"].L.Select(av => av.S).ToList();
                    order.ShippingAddress = item["ShippingAddress"].S;
                    order.Status = item["Status"].S;
                    order.TotalPrice = decimal.Parse(item["TotalPrice"].N);
                    Orders.Add(order);
                }
            }
            catch (AmazonDynamoDBException ex)
            {
                return null;
            }
            return Orders.Find(x=>x.OrderID.Equals(orderID));
        }

        public async Task<string> createOrder(List<Furniture> furnitures, BetterFurnitureUser user, decimal totalPrice)
        {
            var client = connectDynamoDb();
            //create unique id 
            string uniqueId = Guid.NewGuid().ToString();
            
            try
            {
                Dictionary<string, AttributeValue> item = new Dictionary<string, AttributeValue>
                {
                    { "OrderID",new AttributeValue{S=uniqueId} },
                    {"CustomerName",new AttributeValue{S=user.CustomerFullName} },
                    { "ShippingAddress",new AttributeValue{S=user.CustomerAddress } },
                    { "CustomerEmail",new AttributeValue{S=user.Email } }, // need to edit
                    { "CustomerPhone",new AttributeValue{S=user.PhoneNumber } }, // need to edit
                    { "Status",new AttributeValue{S="Waiting to be packed" } },
                    { "ItemName",new AttributeValue{L=furnitures.Select(x=>new AttributeValue{S=x.Name }).ToList() } },
                     {"TotalPrice",new AttributeValue{N=totalPrice.ToString()} }
                };
                // List<string> test = item["ItemName"].L.Select(av => av.S).ToList();

                PutItemRequest request = new PutItemRequest
                {
                    TableName = orderTable,
                    Item = item
                };
                await client.PutItemAsync(request);

                Dictionary<string, AttributeValue> key = new Dictionary<string, AttributeValue>
                    {
                        {"CustomerId",new AttributeValue{S=user.Id } }
                    };
                DeleteItemRequest delete_request = new DeleteItemRequest
                {
                    TableName = cartTable,
                    Key = key
                };
                await client.DeleteItemAsync(delete_request);
                var furnitureCount = furnitures.GroupBy(f => f.Name)
                                .Select(g => new { FurnitureName = g.Key, Quantity = g.Count() })
                                .ToList();
                foreach (var furniture_item in furnitureCount)
                {
                    var furniture = _repository.GetByName(furniture_item.FurnitureName);
                    furniture.Quantity -= furniture_item.Quantity;
                    _repository.Update(furniture);
                }
  
            }
            catch (AmazonDynamoDBException ex)
            {
                return "Error: " + ex.Message;
            }
            return uniqueId;
        }

        // DynamoDBClient
        private AmazonDynamoDBClient connectDynamoDb()
        {
            List<string> keys = getKeys();
            AmazonDynamoDBClient client = new AmazonDynamoDBClient(keys[0], keys[1], keys[2], RegionEndpoint.USEast1);
            return client;
        }

        private async Task<string> checkFurnitureQuantity()
        {
            var snsClient = connectSNS();
            string topicArn = "arn:aws:sns:us-east-1:165343445807:BetterFurnitureAdmin";
            List<Furniture> furnitures = _repository.GetAll();
            try
            {
                foreach (var furniture in furnitures)
                {
                    if (furniture.Quantity < 5)
                    {
                        string msg = "Quantity of " + furniture.Name + " is lesser than 5. Current Quantity is " + furniture.Quantity;
                        var publishRequest = new PublishRequest()
                        {
                            TopicArn = topicArn,
                            Message = msg
                        };
                        var publishResponse = await snsClient.PublishAsync(publishRequest);
                    }
                    
                }
            }catch(AmazonSimpleNotificationServiceException ex)
            {
                return "Error: " + ex.Message ;
            }catch(Exception ex)
            {
                return "Error: " + ex.Message;
            }
            
            return "Check successful.";
        }

        private AmazonSimpleNotificationServiceClient connectSNS()
        {
            List<string> keys = getKeys();
            AmazonSimpleNotificationServiceClient client = new AmazonSimpleNotificationServiceClient(keys[0], keys[1], keys[2], RegionEndpoint.USEast1);
            return client;
        }

        private List<string> getKeys()
        {
            List<string> keys = new List<string>();
            var builder = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory()).AddJsonFile("appsettings.json");
            IConfiguration config = builder.Build();

            keys.Add(config["AWS:id"]);
            keys.Add(config["AWS:key"]);
            keys.Add(config["AWS:token"]);
            return keys;
        }
    }
}
