using Amazon;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using BetterFurniture.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace BetterFurniture.Controllers
{
    public class CartController : Controller
    {
        private const string tableName = "BetterFurnitureCart";

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

        public async Task<List<Cart>> getCarts()
        {
            var client = connect();
            List<Cart> carts = new List<Cart>();
            try
            {
                ScanRequest request = new ScanRequest
                {
                    TableName = tableName
                };

                ScanResponse response = await client.ScanAsync(request);

                foreach (var item in response.Items)
                {
                    Cart cart = new Cart();
                    cart.CustomerName = item["CustomerName"].S;
                    cart.ItemName = item["ItemName"].L.Select(av => av.S).ToList();
                    cart.total_price = decimal.Parse(item["TotalPrice"].N);
                    carts.Add(cart);
                }
            }
            catch (AmazonDynamoDBException ex)
            {
                return null;
            }
            return carts;
        }

        public async Task<IActionResult> CartPage()
        {
            string customerName = "Chin";
            List<Cart> carts = await getCarts();
            Cart cart = carts.Find(x => x.CustomerName.Equals(customerName));
            if (cart == null)
                ViewBag.Msg = "Your cart is empty. Add some product into your cart now!";
            if (TempData["msg"] != null)
            {
                string modifyMsg = (string)TempData["msg"];
                if (modifyMsg.Contains("Succesfully"))
                    ViewBag.color = "green";
                else
                    ViewBag.color = "red";
                ViewBag.modifyMsg = modifyMsg;
            }
            return View(cart);
        }
        public async Task<IActionResult> RemoveFromCart(string itemName, string customerName)
        {
            List<Cart> carts = await getCarts();
            Console.WriteLine(customerName);
            Console.WriteLine(itemName);
            Cart cart = carts.Find(x => x.CustomerName.Equals(customerName));
            Console.WriteLine(cart.ItemName);
            
            cart.ItemName.Remove(itemName);
            if (cart.ItemName.Count == 0)
            {
                await Delete(customerName);
            }
            else
            {
                // manage here so it deduct the total price correctly
                cart.total_price -= 10;
                string msg = await update_cart(cart);
                TempData["msg"] = msg;
                
            }
            return RedirectToAction("CartPage", "Cart");
        }
        [HttpPost]
        public async Task<string> update_cart(Cart cart)
        {
            var client = connect();
            try
            {
                Dictionary<string, AttributeValue> item = new Dictionary<string, AttributeValue>
                {
                    {"CustomerName",new AttributeValue{S=cart.CustomerName } },
                    { "TotalPrice",new AttributeValue{N=cart.total_price.ToString() } },
                    { "ItemName",new AttributeValue{L=cart.ItemName.Select(x=>new AttributeValue{S=x }).ToList() } }
                };

                PutItemRequest request = new PutItemRequest
                {
                    TableName = tableName,
                    Item = item
                };
                await client.PutItemAsync(request);
            }
            catch (AmazonDynamoDBException ex)
            {
                return "Error: " + ex.Message;
            }
            return "Updated Cart Succesfully";
        }

        public async Task<string> Delete(string id)
        {
            var client = connect();
            try
            {
                Dictionary<string, AttributeValue> key = new Dictionary<string, AttributeValue>
                    {
                        {"CustomerName",new AttributeValue{S=id } }
                    };
                DeleteItemRequest request = new DeleteItemRequest
                    {
                        TableName = tableName,
                        Key = key
                    };
                await client.DeleteItemAsync(request);
            }
            catch (AmazonDynamoDBException ex)
            {
                return "Error: " + ex.Message;
            }
            catch (Exception ex)
            {
                return "Error: " + ex.Message;
            }

            return "Deleted Successfully";
        }
    }
}
