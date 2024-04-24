using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Azure.AI.TextAnalytics;
using Azure;
using Azure.AI.OpenAI;
using System.IO;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs;
using iTextSharp.text.pdf;
using iTextSharp.text.pdf.parser;

namespace proto.Controllers
{
    public class MessageRequest //corps de la requête
    {
        public string? chatContent { get; set; }
        public string? Message { get; set; }
    }
    
    [ApiController]
    [Route("api/chat")] 
    public class ChatController : ControllerBase
    {
        private readonly IConfiguration _config;
        private readonly HttpClient _httpClient;
        private readonly BlobContainerClient containerClient;
        readonly List<BlobItem> listFiles= [];

        public ChatController(IConfiguration config, IHttpClientFactory httpClientFactory)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _httpClient = httpClientFactory?.CreateClient() ?? throw new ArgumentNullException(nameof(httpClientFactory));
            string connectionString = Environment.GetEnvironmentVariable(variable: "AZURE_STORAGE_CONNECTION_STRING");
            var blobServiceClient = new BlobServiceClient(connectionString);
            containerClient = blobServiceClient.GetBlobContainerClient("quickstartblobs3505eaa5-04c5-4a41-8ea2-002df7ef53f8");
        }

        [HttpPost]
        public async Task<IActionResult> SendMessage([FromBody] MessageRequest request)
        {
            try
            {   
                string chatContent = request.chatContent!;
                string userInput = request.Message!;
                string finale="This is what you need to know, if i ask you someting not related to the files and texts say that you don't know the answer : ";
                List<BlobItem> liste = await GetFilesFromStorage();

                var tasks = liste.Select(async item =>{
                    string contentType = item.Properties.ContentType; // Assuming item is a BlobItem
                    if (contentType.Equals("application/pdf", StringComparison.OrdinalIgnoreCase))
                    {
                        Console.WriteLine("pdf");
                        return await PdfText(item);
                    }
                    else if (contentType.Equals("text/plain", StringComparison.OrdinalIgnoreCase))
                    {
                        Console.WriteLine("txt");
                        return await GetContentFromFileAsync(item);
                    }
                    else
                    {
                        throw new InvalidOperationException($"Unknown content type: {contentType}");
                    }
                    
                } );
                //replace the foreach cause foreach doesn't wait for async task to finsh
                await Task.WhenAll(tasks);
                foreach (var task in tasks)
                    finale += await task + "\n\n\n";
                
                

                string azureModelEndpoint = _config["AzureOAIEndpoint"] ?? throw new ArgumentNullException("AzureOAIEndpoint");
                string azureModelKey = _config["AzureOAIKey"] ?? throw new ArgumentNullException("AzureOAIKey");

                OpenAIClient client = new(new Uri(azureModelEndpoint), new AzureKeyCredential(azureModelKey));

                ChatCompletionsOptions chatCompletionsOptions = new()
                {
                   Messages = {
                        new ChatRequestSystemMessage(finale),
                        new ChatRequestSystemMessage(chatContent),
                        new ChatRequestUserMessage(userInput),
                    },
                    Temperature = 0.7f,
                    DeploymentName = "testopenai",
                };

                //chatCompletionsOptions.Prompts.Add(request.Message);

                Console.WriteLine(finale);
                Response<ChatCompletions> response = await client.GetChatCompletionsAsync(chatCompletionsOptions);
                string completion = response.Value.Choices[0].Message.Content;
                return Ok(completion);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        private async Task<string> GetContentFromFileAsync(BlobItem blobItem){
            using Stream stream = await containerClient
            .GetBlobClient(blobItem.Name)
            .OpenReadAsync();
            using StreamReader reader = new(stream);
            // Read the content directly from the stream
            string content = await reader.ReadToEndAsync();
            return content;
        }

        public async Task<List<BlobItem>> GetFilesFromStorage(){
             await foreach (BlobItem blobItem in containerClient.GetBlobsAsync())
             {
                listFiles.Add(blobItem);
             }
            return listFiles;
        }

           private async Task<string> PdfText(BlobItem blobItem)
            {
                var blobClient = containerClient.GetBlobClient(blobItem.Name);
                var response = await blobClient.DownloadAsync();
                using var streamReader = new StreamReader(response.Value.Content);
                using var pdfReader = new PdfReader(streamReader.BaseStream);
                string text = string.Empty;
                for(int page = 1; page <= pdfReader.NumberOfPages; page++)
                {
                    text += iTextSharp.text.pdf.parser.PdfTextExtractor.GetTextFromPage(pdfReader, page);
                }
                    return text;
            }  
    }
}