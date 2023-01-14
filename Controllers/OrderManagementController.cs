using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amazon;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.DynamoDBv2.DocumentModel;
using Microsoft.Extensions.Configuration;
using System.IO;
using BetterFurniture.Models;

namespace BetterFurniture.Controllers
{
    public class OrderManagementController : Controller
    {
        private const string tableName = "BetterFurnitureOrder";
        
        private AmazonDynamoDBClient connect()
        {
            List<string> keys = new List<string>();
            var builder = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory()).AddJsonFile("appsettings.json");
            IConfiguration config = builder.Build();

            keys.Add(config["AWS:id"]);
            keys.Add(config["AWS:key"]);
            keys.Add(config["AWS:token"]);
            AmazonDynamoDBClient client = new AmazonDynamoDBClient(keys[0], keys[1], keys[2],RegionEndpoint.USEast1);
            return client;
        }

        // order page
        public async Task<IActionResult> Index()
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
            var client = connect();
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
            return RedirectToAction("Index", "OrderManagement");
        }

        public async Task<IActionResult> Delete(string id)
        {
            var client = connect();
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
                return RedirectToAction("Index", "OrderManagement");
            }catch(Exception ex)
            {
                TempData["msg"] = "Error: " + ex.Message;
                return RedirectToAction("Index", "OrderManagement");
            }
            
            return RedirectToAction("Index", "OrderManagement");
        }

        [HttpPost]
        public async Task<string> update_dynamodb(Order order)
        {
            var client = connect();
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
                    { "ItemName",new AttributeValue{L=order.ItemName.Select(x=>new AttributeValue{S=x }).ToList() } }
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
