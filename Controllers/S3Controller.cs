using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amazon; // connect AWS account
using Amazon.S3; // bucket library
using Amazon.S3.Model; 
using Microsoft.Extensions.Configuration; // for appsettings.json
using System.IO; // input and output
using Microsoft.AspNetCore.Http;


namespace BetterFurniture.Controllers
{
    public class S3Controller : Controller
    {
        private const string s3name = "better-furniture-s3";

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
            return RedirectToAction("Display", "S3");
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
            return RedirectToAction("Display", "S3");
        }

        public IActionResult Upload()
        {
            return View("Upload");
        }
    }
}
