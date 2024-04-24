using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;

namespace proto.Controllers{
    [ApiController]
    [Route("api/storage")]
    public class ManageStorage : ControllerBase{
        readonly List<BlobItem> listFiles= [];
        private readonly BlobContainerClient containerClient;
        public ManageStorage()
        {
            string connectionString = Environment.GetEnvironmentVariable(variable: "AZURE_STORAGE_CONNECTION_STRING");
            var blobServiceClient = new BlobServiceClient(connectionString);
            containerClient = blobServiceClient.GetBlobContainerClient("quickstartblobs3505eaa5-04c5-4a41-8ea2-002df7ef53f8");
        }
        [HttpGet]
        public async Task<List<BlobItem>> GetFilesFromStorage(){
             await foreach (BlobItem blobItem in containerClient.GetBlobsAsync())
             {
                listFiles.Add(blobItem);
             }
            return listFiles;

        }

        [HttpDelete]
        public void DeleteFromStorage([FromBody] MessageRequest fileToDelete){
            // Create the container and return a container client object
            containerClient.DeleteBlobAsync(fileToDelete.Message);
        }

         [HttpPost("upload")]
            public async Task<IActionResult> UploadFile([FromForm] IFormFile file){
                BlobClient blobClient = containerClient.GetBlobClient(file.FileName);
                // Open the file and upload its data
                using (var stream = file.OpenReadStream())
                {
                    await blobClient.UploadAsync(stream, new BlobUploadOptions { HttpHeaders = new BlobHttpHeaders { ContentType = file.ContentType } });
                }

                return Ok();
            }


    }
}
// Create a local file in the ./data/ directory for uploading and downloading
//string localPath = "data";
//Directory.CreateDirectory(localPath);
//string fileName = "quickstart" + Guid.NewGuid().ToString() + ".txt";
//string localFilePath = Path.Combine(localPath, fileName);
// Write text to the file
//await System.IO.File.WriteAllTextAsync(localFilePath, "Hello, World!");
//// Get a reference to a blob
//BlobClient blobClient = containerClient.GetBlobClient(fileName);
//Console.WriteLine("Uploading to Blob storage as blob:\n\t {0}\n", blobClient.Uri);
//// Upload data from the local file, overwrite the blob if it already exists
//await blobClient.UploadAsync(localFilePath, true);
//await foreach (BlobItem blobItem in containerClient.GetBlobsAsync())
//{
//    Console.WriteLine("\t" + blobItem.Name);
//}