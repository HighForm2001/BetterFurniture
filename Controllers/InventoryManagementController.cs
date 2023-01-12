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


namespace BetterFurniture.Controllers
{
    public class InventoryManagementController : Controller
    {
        private const string s3name = "better-furniture-s3";
        private readonly Models.Repositories.FurnitureRepository _repository;

        // inject connection to the database
        public InventoryManagementController(Models.Repositories.FurnitureRepository repository)
        {
            _repository = repository;
        }

        public IActionResult Index()
        {
            var furniture = _repository.GetAll();
            return View(furniture);
        }
        public IActionResult CreateView()
        {
            return View();
        }
        public IActionResult EditView(int id)
        {
            Models.Furniture furniture_to_edit = _repository.GetByID(id);
            return View(furniture_to_edit);
        }
        // create product - need further improvement, like
        // need id, name, description, quantity, images
        [HttpPost]
        public async Task<IActionResult> Create(Models.Furniture furniture, List<IFormFile> imageFile)
        {
            var s3client = connect();
            if (_repository.GetByName(furniture.Name) != null)
            {
                string msg = "Existing furniture name: " + furniture.Name;
                return RedirectToAction("CreateView", "InventoryManagement", new { Msg = msg});
            }
            foreach (var img in imageFile)
            {
                if (img.Length <= 0)
                {
                    return BadRequest("Empty file. Failed to upload!");
                }
                else if (!img.ContentType.ToLower().StartsWith("image/")) // check file type
                {
                    return BadRequest("Not a image type. Failed to upload!" + "Image type: " + img.ContentType.ToLower());
                }

                // upload img to S3 and get URL
                try
                {

                    // upload to s3
                    PutObjectRequest request = new PutObjectRequest // generate request
                    {
                        InputStream = img.OpenReadStream(),
                        BucketName = s3name + "/images",
                        Key = img.FileName,
                        CannedACL = S3CannedACL.PublicRead
                    };

                    // send request
                    await s3client.PutObjectAsync(request);
                    furniture.ImageUrls = furniture.ImageUrls + "https://" + s3name + ".s3.amazonaws.com/images/" + img.FileName + ",";
                }
                catch (AmazonS3Exception ex)
                {
                    return BadRequest("Failed to upload due to technical issue. Error message: " + ex.Message);
                }
                catch (Exception ex)
                {
                    return BadRequest("Failed to upload due to technical issue. Error message: " + ex.Message);
                }

            }
            furniture.ImageUrls = furniture.ImageUrls.Substring(0,furniture.ImageUrls.Length-1);
            _repository.Add(furniture);
            Console.WriteLine("Added successfully");
            return RedirectToAction("Index", "InventoryManagement");
        }

        // edit product
        [HttpPost]
        public IActionResult Edit(Models.Furniture furniture)
        {
            _repository.Update(furniture);
            return RedirectToAction("Index", "InventoryManagement");
        }

        // delete product
        public IActionResult Delete(int id)
        {
            _repository.Delete(id);
            return RedirectToAction("Index", "InventoryManagement");
        }


        // step 1: get string for connection to AWS account
        private List<string> getValues()
        {
            List<string> values = new List<string>();

            // link appsettings.json and get values
            var builder = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory()).AddJsonFile("appsettings.json");
            IConfigurationRoot config = builder.Build(); // build the json file

            // read info from json using config instance
            values.Add(config["S3:id"]);
            values.Add(config["S3:key"]);
            values.Add(config["S3:token"]);

            return values;
        }

        private AmazonS3Client connect()
        {
            List<string> values = getValues();
            var s3Client = new AmazonS3Client(values[0], values[1], values[2], RegionEndpoint.USEast1);
            //var s3Client = new AmazonS3Client(RegionEndpoint.USEast1);
            return s3Client;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        // step 2: upload image to s3 and generate the url to store in DB
        public async Task<IActionResult> ProcessUploadImage(List<IFormFile> imageFile)
        {
            // get credential
            var s3client = connect();

            // read each image and store to s3
            if (imageFile == null)
            {
                return BadRequest("You did not submit any file.");
            }
            foreach (var img in imageFile)
            {
                if (img.Length <= 0)
                {
                    return BadRequest("Empty file. Failed to upload!");
                }
                else if (!img.ContentType.ToLower().StartsWith("image/")) // check file type
                {
                    return BadRequest("Not a image type. Failed to upload!" + "Image type: " + img.ContentType.ToLower());
                }

                // upload img to S3 and get URL
                try
                {
                    
                    
                    // upload to s3
                    PutObjectRequest request = new PutObjectRequest // generate request
                    {
                        InputStream = img.OpenReadStream(),
                        BucketName = s3name + "/images",
                        Key = img.FileName,
                        CannedACL = S3CannedACL.PublicRead
                    };

                    // send request
                     await s3client.PutObjectAsync(request);
                }
                catch (AmazonS3Exception ex)
                {
                    return BadRequest("Failed to upload due to technical issue. Error message: " + ex.Message);
                }
                catch (Exception ex)
                {
                    return BadRequest("Failed to upload due to technical issue. Error message: " + ex.Message);
                }
            }

            // return to upload page
            return RedirectToAction("Display", "InventoryManagement");
        }

        // step 3: display image from s3 as gallery
        public async Task<IActionResult> Display()
        {
            // get credential
            var s3client = connect();
            List<S3Object> images = new List<S3Object>();
            try
            {
                // s3 token, to tell if image still in S3
                string token = null;

                do
                {
                    //create list object request to S3
                    ListObjectsRequest request = new ListObjectsRequest
                    {
                        BucketName = s3name
                    };

                    // get response of image from s3
                    ListObjectsResponse response = await s3client.ListObjectsAsync(request).ConfigureAwait(false);
                    images.AddRange(response.S3Objects);
                    token = response.NextMarker;
                } while (token != null);
            }catch(AmazonS3Exception ex)
            {
                return BadRequest("Error: " + ex.Message);
            }
            return View(images);
        }

        // function 4: delete image
        public async Task<IActionResult> DeleteImage(string imgName)
        {
            // add credential
            var s3client = connect();
            try
            {
                // create a delete request
                DeleteObjectRequest request = new DeleteObjectRequest
                {
                    BucketName = s3name,
                    Key = imgName
                };
                await s3client.DeleteObjectAsync(request);
            }catch(AmazonS3Exception ex)
            {
                return BadRequest("Error message: " + ex.Message);
            }catch(Exception ex)
            {
                return BadRequest("Error message: " + ex.Message);
            }
            return RedirectToAction("Display", "InventoryManagement");
        }

        public IActionResult Upload()
        {
            return View("Upload");
        }
    }
}
