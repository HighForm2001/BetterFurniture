using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amazon;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.DynamoDBv2.DocumentModel;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using Microsoft.Extensions.Configuration;
using System.IO;
using BetterFurniture.Models;
using Microsoft.AspNetCore.Authorization;
using BetterFurniture.Areas.Identity.Data;
using Microsoft.AspNetCore.Identity;

namespace BetterFurniture.Controllers
{
    [Authorize]
    public class OrderManagementController : Controller
    {
        private const string tableName = "BetterFurnitureOrder";
        private readonly UserManager<BetterFurnitureUser> _userManager;

        public OrderManagementController(UserManager<BetterFurnitureUser> userManager)
        {
            _userManager = userManager;
        }

        private AmazonDynamoDBClient connectDynamoDb()
        {
            List<string> keys = getKeys();
            AmazonDynamoDBClient client = new AmazonDynamoDBClient(keys[0], keys[1], keys[2],RegionEndpoint.USEast1);
            return client;
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

        // order page
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> OrderOverview()
        {
            if (TempData["msg"] != null)
            {
                string msg = (string)TempData["msg"];
                if (msg.Contains("Succesfully"))
                    ViewBag.color = "chartreuse";
                else
                    ViewBag.color = "red";
                ViewBag.Msg = msg;
            }
            List<Order> orders = await getOrders();
            if (orders == null)
            {
                return View();
            }
            return View(orders);

        }
        public async Task<List<Order>> getOrders()
        {
            var client = connectDynamoDb();
            List<Order> orders = new List<Order>();
            try
            {
                ScanRequest request = new ScanRequest
                {
                    TableName = tableName
                };

                ScanResponse response = await client.ScanAsync(request);

                foreach (var item in response.Items)
                {
                    Order order = new Order();
                    order.CustomerName = item["CustomerName"].S;
                    order.OrderID = item["OrderID"].S;
                    order.ShippingAddress = item["ShippingAddress"].S;
                    order.CustomerEmail = item["CustomerEmail"].S;
                    order.CustomerPhone = item["CustomerPhone"].S;
                    order.ItemName = item["ItemName"].L.Select(av => av.S).ToList();
                    order.Status = item["Status"].S;
                    order.TotalPrice = decimal.Parse(item["TotalPrice"].N);
                    orders.Add(order);
                }
            }
            catch (AmazonDynamoDBException ex)
            {
                return null;
            }
            return orders;
        }

        // edit page
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> EditOrder(string id)
        {
            List<Order> orders =await getOrders();
            if (orders != null)
            {
                Order order = orders.Find(x => x.OrderID == id);
                if (order != null)
                    return View(order);
            }
            return View();
        }


        public async Task<IActionResult> processEditOrder(Order order)
        {
            Console.WriteLine(order.ItemName);
            string msg = await update_dynamodb(order);
            TempData["msg"] = msg;
            return RedirectToAction("OrderOverview", "OrderManagement");
        }

        public async Task<IActionResult> OrderStatus()
        {
            BetterFurnitureUser user = await _userManager.GetUserAsync(HttpContext.User);
            string userName = user.CustomerFullName;
            List<Order> selected_orders = new List<Order>();
            List<Order> all_orders = await getOrders();
            foreach(var order in all_orders)
            {
                if (order.CustomerName.Equals(userName))
                    selected_orders.Add(order);
            }
            return View(selected_orders);
        }

        public async Task<IActionResult> Delete(string id)
        {
            var client = connectDynamoDb();
            try
            {
                Dictionary<string, AttributeValue> key = new Dictionary<string, AttributeValue>
            {
                {"OrderID",new AttributeValue{S=id } }
            };
                DeleteItemRequest request = new DeleteItemRequest
                {
                    TableName = tableName,
                    Key = key
                };
                await client.DeleteItemAsync(request);
            }catch(AmazonDynamoDBException ex)
            {
                TempData["msg"] = "Error: " + ex.Message;
                return RedirectToAction("OrderOverview", "OrderManagement");
            }catch(Exception ex)
            {
                TempData["msg"] = "Error: " + ex.Message;
                return RedirectToAction("OrderOverview", "OrderManagement");
            }
            
            return RedirectToAction("OrderOverview", "OrderManagement");
        }

        [HttpPost]
        public async Task<string> update_dynamodb(Order order)
        {
            var client = connectDynamoDb();
            try
            {
                Dictionary<string, AttributeValue> item = new Dictionary<string, AttributeValue>
                {
                    { "OrderID",new AttributeValue{S=order.OrderID } },
                    {"CustomerName",new AttributeValue{S=order.CustomerName } },
                    { "ShippingAddress",new AttributeValue{S=order.ShippingAddress } },
                    { "CustomerEmail",new AttributeValue{S=order.CustomerEmail } },
                    { "CustomerPhone",new AttributeValue{S=order.CustomerPhone } },
                    { "Status",new AttributeValue{S=order.Status } },
                    { "ItemName",new AttributeValue{L=order.ItemName.Select(x=>new AttributeValue{S=x }).ToList() } },
                    {"TotalPrice",new AttributeValue{N=order.TotalPrice.ToString()} }
                };
               // List<string> test = item["ItemName"].L.Select(av => av.S).ToList();
                
                PutItemRequest request = new PutItemRequest
                {
                    TableName = tableName,
                    Item = item
                };
                await client.PutItemAsync(request);
            }catch(AmazonDynamoDBException ex)
            {
                return "Error: " + ex.Message;
            }
            return "Updated Succesfully";
        }
    }
}
