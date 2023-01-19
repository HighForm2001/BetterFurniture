using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using BetterFurniture.Areas.Identity.Data;
using BetterFurniture.Models;
using BetterFurniture.Models.Repositories;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace BetterFurniture.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly FurnitureRepository _repository;
        private UserManager<BetterFurnitureUser> _userManager;

        public HomeController(ILogger<HomeController> logger, FurnitureRepository repository, UserManager<BetterFurnitureUser> userManager)
        {
            _logger = logger;
            _repository = repository;
            _userManager = userManager;
        }

        // views
        public async Task<IActionResult> Index(List<Furniture>? searched_furniture)
        {
            if (User.Identity.IsAuthenticated && User.IsInRole("Admin"))
            {
                BetterFurnitureUser user = await _userManager.GetUserAsync(HttpContext.User);
                string msg = await checkSubscription(user);
                Console.WriteLine(msg);
            }
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
        
        // functions
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

        private List<string> getValues()
        {
            List<string> values = new List<string>();

            // link appsettings.json and get values
            var builder = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory()).AddJsonFile("appsettings.json");
            IConfigurationRoot config = builder.Build(); // build the json file

            // read info from json using config instance
            values.Add(config["AWS:id"]);
            values.Add(config["AWS:key"]);
            values.Add(config["AWS:token"]);

            return values;
        }

        private async Task<string> checkSubscription(BetterFurnitureUser user)
        {
            // retrieve an existing 
            try
            {
                var client = connectSNS();
                string topicARN = "arn:aws:sns:us-east-1:165343445807:BetterFurnitureAdmin";
                string email = user.Email;

                // check if the user is subscribed to the topic
                var listSubscriptionsByTopicRequest = new ListSubscriptionsByTopicRequest
                {
                    TopicArn = topicARN
                };
                bool isSubscribed = false;
                ListSubscriptionsByTopicResponse listSubscriptionsByTopicResponse;
                do
                {
                    listSubscriptionsByTopicResponse = await client.ListSubscriptionsByTopicAsync(listSubscriptionsByTopicRequest);

                    foreach (var subscription in listSubscriptionsByTopicResponse.Subscriptions)
                    {
                        if (subscription.Protocol == "email" && subscription.Endpoint == email)
                        {
                            isSubscribed = true;
                            break;
                        }
                    }

                    listSubscriptionsByTopicRequest.NextToken = listSubscriptionsByTopicResponse.NextToken;
                } while (listSubscriptionsByTopicResponse.NextToken != null);
                if (!isSubscribed)
                {
                    var subscribeRequest = new SubscribeRequest
                    {
                        TopicArn = topicARN,
                        Protocol = "email",
                        Endpoint = email,
                    };
                    /*subscribeRequest.Attributes.Add("Email", "true"); // got error*/
                    var subscribeResponse = await client.SubscribeAsync(subscribeRequest);
                    return subscribeResponse.ToString();
                }
                return "User already subscribed to the SNS";

            }
            catch (AmazonSimpleNotificationServiceException ex)
            {
                return ex.Message;
            }
            catch (Exception ex)
            {
                return ex.Message;
            }


        }

        private AmazonSimpleNotificationServiceClient connectSNS()
        {
            List<string> keys = getValues();
            AmazonSimpleNotificationServiceClient client = new AmazonSimpleNotificationServiceClient(keys[0], keys[1], keys[2], RegionEndpoint.USEast1);
            return client;
        }
    }
}
