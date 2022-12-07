using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Files.Application;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Files.Infrastructure
{
    public class BlobStorage : IBlobStorage
    {
        #region Dependency Injection / Constructor
        private readonly string? _storageConnectionString;
        private readonly string? _storageContainerName;
        private readonly ILogger<BlobStorage> _logger;
        private readonly BlobContainerClient _blobContainerClient;
        public IConfiguration? Configuration { get; }

        public BlobStorage(IConfiguration configuration, ILogger<BlobStorage> logger)
        {
            Configuration = configuration;
            _storageConnectionString = Configuration["BlobConnectionString"];
            _storageContainerName = Configuration["BlobContainerName"];
            BlobContainerClient blobContainerClient = new(_storageConnectionString, _storageContainerName);

            _blobContainerClient = blobContainerClient;
            _logger = logger;
        }
        #endregion

        /// <summary>
        /// A method that retrieve all the files from the blob.
        /// </summary>
        /// <returns> List of BlobDto </returns>
        public async Task<List<BlobDto>> ListAsync()
        {
            List<BlobDto> files = new();

            await foreach (BlobItem file in _blobContainerClient.GetBlobsAsync())
            {
                string uri = _blobContainerClient.Uri.ToString();
                var name = file.Name;
                var fullUri = $"{uri}/{name}";

                files.Add(new BlobDto
                {
                    Uri = fullUri,
                    Name = name,
                    ContentType = file.Properties.ContentType
                });
            }
            return files;
        }
        public async Task<BlobResponseDto> UploadAsync(IFormFile blob)
        {
            BlobResponseDto blobResponse = new();
            try
            {
                BlobClient client = _blobContainerClient.GetBlobClient(blob.FileName);

                // Open a stream for the file we want to upload
                await using (Stream? data = blob.OpenReadStream())
                {
                    // Upload the file async
                    await client.UploadAsync(data);
                }
                // Everything is OK and file got uploaded
                blobResponse.Status = $"File {blob.FileName} Uploaded Successfully";
                blobResponse.Error = false;
                blobResponse.Blob.Uri = client.Uri.AbsoluteUri;
                blobResponse.Blob.Name = client.Name;

            }
            // If the file already exists, we catch the exception and do not upload it
            catch (RequestFailedException ex)
               when (ex.ErrorCode == BlobErrorCode.BlobAlreadyExists)
            {
                _logger.LogError($"File with name {blob.FileName} already exists in container. Set another name to store the file in the container: '{_storageContainerName}.'");
                blobResponse.Status = $"File with name {blob.FileName} already exists. Please use another name to store your file.";
                blobResponse.Error = true;
                return blobResponse;
            }
            // If we get an unexpected error, we catch it here and return the error message
            catch (RequestFailedException ex)
            {
                // Log error to console and create a new response we can return to the requesting method
                _logger.LogError($"Unhandled Exception. ID: {ex.StackTrace} - Message: {ex.Message}");
                blobResponse.Status = $"Unexpected error: {ex.StackTrace}. Check log with StackTrace ID.";
                blobResponse.Error = true;
                return blobResponse;
            }

            // Return the BlobUploadResponse object
            return blobResponse;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="blobFilename"></param>
        /// <returns></returns>
        public async Task<BlobDto?> DownloadAsync(string blobFilename)
        {
            try
            {
                // Get a reference to the blob uploaded earlier from the API in the container from configuration settings
                BlobClient file = _blobContainerClient.GetBlobClient(blobFilename);

                // Check if the file exists in the container
                if (await file.ExistsAsync())
                {
                    var data = await file.OpenReadAsync();
                    Stream blobContent = data;

                    // Download the file details async
                    var content = await file.DownloadContentAsync();

                    // Add data to variables in order to return a BlobDto
                    string name = blobFilename;
                    string contentType = content.Value.Details.ContentType;

                    // Create new BlobDto with blob data from variables
                    return new BlobDto { Content = blobContent, Name = name, ContentType = contentType };
                }
            }
            catch (RequestFailedException ex)
                when (ex.ErrorCode == BlobErrorCode.BlobNotFound)
            {
                // Log error to console
                _logger.LogError($"File {blobFilename} was not found.");
            }

            // File does not exist, return null and handle that in requesting method
            return null;
        }
        public async Task<BlobResponseDto> DeleteAsync(string blobFilename)
        {
            BlobClient file = _blobContainerClient.GetBlobClient(blobFilename);

            try
            {
                // Delete the file
                await file.DeleteAsync();
            }
            catch (RequestFailedException ex)
                when (ex.ErrorCode == BlobErrorCode.BlobNotFound)
            {
                // File did not exist, log to console and return new response to requesting method
                _logger.LogError($"File {blobFilename} was not found.");
                return new BlobResponseDto { Error = true, Status = $"File with name {blobFilename} not found." };
            }

            // Return a new BlobResponseDto to the requesting method
            return new BlobResponseDto { Error = false, Status = $"File: {blobFilename} has been successfully deleted." };
        }
    }
}
