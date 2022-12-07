using Files.Application;
using Files.Application.BlobStorage.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace Files.Api;

[Route("api/Files")]
[ApiController]
public class BlobStorageController : ControllerBase
{
    private readonly IBlobStorage _blobStorage;
    public BlobStorageController(IBlobStorage blobStorage) 
    { 
        _blobStorage = blobStorage ?? throw new ArgumentNullException(nameof(blobStorage));
    }

    [HttpGet("")]
    public async Task<IActionResult> Get()
    {
        List<BlobDto>? files = await _blobStorage.ListAsync();

        if (files == null) return NotFound();

        return StatusCode(StatusCodes.Status200OK, files);
    }

    [HttpPost(nameof(Upload))]
    public async Task<IActionResult> Upload(IFormFile file) 
    {
        BlobResponseDto? response = await _blobStorage.UploadAsync(file);

        // Check if we got an error
        if (response.Error == true)
        {
            // We got an error during upload, return an error with details to the client
            return StatusCode(StatusCodes.Status500InternalServerError, response.Status);
        }
        else
        {
            // Return a success message to the client about successfull upload
            return StatusCode(StatusCodes.Status200OK, response);
        }
    }
}
