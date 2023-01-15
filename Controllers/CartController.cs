using Amazon;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using BetterFurniture.Areas.Identity.Data;
using BetterFurniture.Models;
using BetterFurniture.Models.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace BetterFurniture.Controllers
{
    [Authorize]
    public class CartController : Controller
    {
        private const string tableName = "BetterFurnitureCart";
        private readonly FurnitureRepository _repository;
        private readonly UserManager<BetterFurnitureUser> _userManager;

        public CartController(FurnitureRepository repository, UserManager<BetterFurnitureUser> userManager)
        {
            _repository = repository;
            _userManager = userManager;
        }

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
            var user = await _userManager.GetUserAsync(HttpContext.User);
            string customerName = user.CustomerFullName;
            List<Cart> carts = await getCarts();
            Cart cart = carts.Find(x => x.CustomerName.Equals(customerName));
            if (cart == null)
                ViewBag.Msg = "Your cart is empty. Add some product into your cart now!";
            else
            {
                cart = update_cart_total_price(cart);
                await update_cart(cart);
            }
            if (TempData["msg"] != null)
            {
                string modifyMsg = (string)TempData["msg"];
                if (modifyMsg.Contains("Succesfully"))
                    ViewBag.color = "green";
                else
                    ViewBag.color = "red";
                ViewBag.modifyMsg = modifyMsg;
            }
            var cart_view = new CartViewModel
            {
                Cart = cart,
                Furniture = _repository.GetAll()
            };
            return View(cart_view);
        }
        public async Task<IActionResult> RemoveFromCart(string itemName, string cart_passed)
        {
            Cart cart = JsonConvert.DeserializeObject<Cart>(cart_passed);
            Console.WriteLine(cart.ItemName);
            
            cart.ItemName.Remove(itemName);
            if (cart.ItemName.Count == 0)
            {
                await Delete(cart);
            }
            else
            {
                // manage here so it deduct the total price correctly
                cart = update_cart_total_price(cart);
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

        public Cart update_cart_total_price(Cart cart)
        {
            cart.total_price = 0;
            foreach(var item in cart.ItemName)
            {
                cart.total_price += _repository.GetByName(item).Price;
            }
            return cart;
        }

        public async Task<string> Delete(Cart cart)
        {
            var client = connect();
            string id = cart.CustomerName;
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
