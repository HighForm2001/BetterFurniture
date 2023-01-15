using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amazon; // connect AWS account
using Amazon.S3; // bucket library
using Amazon.S3.Model;
using Amazon.S3.Transfer;
using Microsoft.Extensions.Configuration; // for appsettings.json
using System.IO; // input and output
using Microsoft.AspNetCore.Http;
using BetterFurniture.Models.Repositories;
using BetterFurniture.Models;
using Microsoft.AspNetCore.Authorization;

namespace BetterFurniture.Controllers
{
/*    [Authorize(Roles ="Admin")]*/
    public class InventoryManagementController : Controller
    {
        private const string s3name = "better-furniture-s3";
        private readonly FurnitureRepository _repository;

        // inject connection to the database
        public InventoryManagementController(FurnitureRepository repository)
        {
            _repository = repository;
        }

        public IActionResult InventoryOverview()
        {
            var furniture = _repository.GetAll();
            return View(furniture);
        }
        public IActionResult CreateView(string? msg)
        {
            if (msg != null)
            {
                ViewBag.Msg = msg;
            }
            return View();
        }
        public async Task<IActionResult> EditView(int id)
        {
            ViewBag.Msg = "";
            if (TempData["msg"] != null){
                ViewBag.Msg = TempData["msg"] as string;
            }
            Console.WriteLine("ViewBag.msg = " + ViewBag.Msg);
            Models.Furniture furniture_to_edit = await _repository.GetByID(id);
            return View(furniture_to_edit);
        }
        // create product 
        [HttpPost]
        public async Task<IActionResult> Create(Furniture furniture, List<IFormFile> imageFile)
        {
            if (_repository.GetByName(furniture.Name) != null)
            {
                string error = "Existing furniture name: " + furniture.Name;
                return RedirectToAction("CreateView","InventoryManagement", new { Msg = error});
            }
            if (imageFile.Count == 0)
            {
                string error = "Lack of information. Please fill in all information needed";
                return RedirectToAction("CreateView", "InventoryManagement", new { Msg = error });
            }
            furniture.ImageUrls = await update_images(imageFile, furniture.Name, "");
            if (ModelState.IsValid)
            {
                _repository.Add(furniture);
                return RedirectToAction("InventoryOverview", "InventoryManagement");
            }
            string msg = "Lack of information. Please fill in all information needed";
            return RedirectToAction("CreateView", "InventoryManagement", new { Msg = msg });
        }

        // edit product
        [HttpPost]
        public async Task<IActionResult> Edit(Furniture furniture, List<IFormFile> imageFile)
        {
            Console.WriteLine(imageFile.Count);
            if (imageFile.Count > 0)
            {
                Console.WriteLine("furniture.ImageUrls" + furniture.Name);
                string urls = await update_images(imageFile, furniture.Name,furniture.ImageUrls);
                furniture.ImageUrls += ","+urls;
                if (furniture.ImageUrls.EndsWith(','))
                {
                    Console.WriteLine("Remove the last ','");
                    furniture.ImageUrls = furniture.ImageUrls[0..^1];
                }
                if (furniture.ImageUrls.StartsWith(","))
                {
                    furniture.ImageUrls = furniture.ImageUrls[1..];
                }
            }
            if((furniture.ImageUrls == null) && (imageFile.Count == 0))
            {
                TempData["msg"] = "You need an image to display the product";
                return RedirectToAction("EditView", "InventoryManagement", new { id = furniture.ID });
            }
            if ((furniture.ImageUrls!= null) &&(furniture.ImageUrls.Contains("Error")))
            {
                TempData["msg"] = furniture.ImageUrls;
                Console.WriteLine("TempData[msg] = " + TempData["msg"]);
                return RedirectToAction("EditView", "InventoryManagement", new { id = furniture.ID });
            }
            if (ModelState.IsValid)
            {
                _repository.Update(furniture);
                return RedirectToAction("InventoryOverview", "InventoryManagement");
            }
            TempData["msg"] = "Please fill in all the information";
            Console.WriteLine("TempData[msg] = " + TempData["msg"]);
            return RedirectToAction("EditView","InventoryManagement",new { id = furniture.ID });
        }

        // delete product
        public async Task<IActionResult> Delete(int id)
        {
            var furniture =await _repository.GetByID(id);
            if (furniture.ImageUrls != null)
            {
                foreach (var url in furniture.ImageUrls.Split(","))
                {
                    await DeleteImage(url, furniture.Name);
                }
            }
            
            _repository.Delete(id);
            return RedirectToAction("InventoryOverview", "InventoryManagement");
        }

        // delete image url
        [HttpPost]
        public async Task<IActionResult> DeleteSingleImage(string imgUrl, int id)
        {
            // Delete the path from the ImageUrls property
            Console.WriteLine("Delete Imae URL is called");
            var furniture = await _repository.GetByID(id);
            
            if (furniture.ImageUrls.Length > 0){
                furniture.ImageUrls = furniture.ImageUrls.Replace(imgUrl, "");
                await DeleteImage(imgUrl,furniture.Name);
                furniture.ImageUrls = furniture.ImageUrls.Replace(",,", ",");
                if (furniture.ImageUrls.StartsWith(','))
                {
                    furniture.ImageUrls = furniture.ImageUrls[1..];
                }
                if (furniture.ImageUrls.EndsWith(','))
                {
                    furniture.ImageUrls = furniture.ImageUrls[0..^1];
                }
            }
            
            _repository.Update(furniture);
            return RedirectToAction("EditView", "InventoryManagement", new { id });
        }

        // update image
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<string> update_images(List<IFormFile> imageFile, string furniture_name, string furnitureUrl)
        {
            string url = "";

            var s3client = connect();
            foreach (var img in imageFile)
            {
                if (img.Length <= 0)
                {
                    return "Error: No image is submitted.";
                }
                else if (!img.ContentType.ToLower().StartsWith("image/")) // check file type
                {
                    return "Error: Not a image type. Failed to upload!" + "Image type: " + img.ContentType.ToLower();
                }
                // upload img to S3 and get URL
                try
                {

                    // upload to s3
                    PutObjectRequest request = new PutObjectRequest // generate request
                    {
                        InputStream = img.OpenReadStream(),
                        BucketName = s3name + "/images/" + furniture_name,
                        Key = img.FileName,
                        CannedACL = S3CannedACL.PublicRead
                    };

                    // send request
                    await s3client.PutObjectAsync(request);
                    string url_to_add = "https://" + s3name + ".s3.amazonaws.com/images/" + furniture_name + "/" + img.FileName;
     /*               Console.WriteLine(furnitureUrl.Length > 0);*/

                    if ( (furnitureUrl == null)|| (furnitureUrl.Length == 0))
                    {
                        Console.WriteLine("furnitureUrl is null");
                        url +=  url_to_add + ",";
                    }else if ((furnitureUrl != null) && (!furnitureUrl.Contains(url_to_add)))
                    {
                        url += url_to_add + ",";
                    }
                }
                catch (AmazonS3Exception ex)
                {
                    return "Error: Failed to upload due to S3 issue. Error message: " + ex.Message;
                }
                

            }
            if (url.Length>0)
                url = url[0..^1];
            return url;
        }
        // step 1: get string for connection to AWS account
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

        private AmazonS3Client connect()
        {
            List<string> values = getValues();
            var s3Client = new AmazonS3Client(values[0], values[1], values[2], RegionEndpoint.USEast1);
            return s3Client;
        }

        
        
        public async Task<string> DeleteImage(string imgUrl, string name)
        {
            // add credential
            var s3client = connect();
            string imgName = imgUrl.Split("/").Last();
            string folder = "/images/" + name + "";
            try
            {
                // create a delete request
                DeleteObjectRequest request = new DeleteObjectRequest
                {
                    BucketName = s3name + folder,
                    Key = imgName
                };
                await s3client.DeleteObjectAsync(request);
            }catch(AmazonS3Exception ex)
            {
                return "Error message: " + ex.Message;
            }catch(Exception ex)
            {
                return "Error message: " + ex.Message;
            }
            return "Successful";
        }

       
    }
}
