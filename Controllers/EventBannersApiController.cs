
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Data;

namespace RaceDay.Controllers
{
    [ApiController]
    [Route("api/events/{eventId:int}/banner")]
    [Authorize(Roles = "Organiser")]
    public class EventBannersApiController : ControllerBase
    {
        private const long MaxFileSize = 5 * 1024 * 1024;

        private readonly ApplicationDbContext _db;
        private readonly UserManager<IdentityUser> _users;
        private readonly IConfiguration _configuration;

        public EventBannersApiController(
            ApplicationDbContext db,
            UserManager<IdentityUser> users,
            IConfiguration configuration)
        {
            _db = db;
            _users = users;
            _configuration = configuration;
        }


        [HttpPost]
        [Consumes("multipart/form-data")]
        [RequestSizeLimit(6 * 1024 * 1024)]
        public async Task<IActionResult> UploadBanner(
            int eventId,
            IFormFile file,
            CancellationToken cancellationToken)

        {
            if (eventId <= 0)
                return BadRequest(new
                {
                    message = "Invalid event ID."
                });

            var user = await _users.GetUserAsync(User);

            if (user?.Email == null)
                return Challenge();

            bool eventExists = await _db.Events
                .AnyAsync(
                    e => e.EventId == eventId,
                    cancellationToken);

            if (!eventExists)
                return NotFound(new
                {
                    message = "Event not found."
                });

            bool ownsEvent = await _db.Events
                .AnyAsync(
                    e => e.EventId == eventId &&
                         e.Organiser.Email == user.Email,
                    cancellationToken);

            if (!ownsEvent)
                return Forbid();

            if (file == null || file.Length == 0)
                return BadRequest(new
                {
                    message = "Select an image to upload."
                });

            if (file.Length > MaxFileSize)
                return BadRequest(new
                {
                    message = "Maximum file size is 5 MB."
                });

            // Read a bounded upload into memory so its
            // actual file signature can be validated.
            await using var imageData = new MemoryStream();

            await file.CopyToAsync(
                imageData,
                cancellationToken);

            byte[] bytes = imageData.ToArray();

            bool isJpeg =
                bytes.Length >= 3 &&
                bytes[0] == 0xFF &&
                bytes[1] == 0xD8 &&
                bytes[2] == 0xFF;

            bool isPng =
                bytes.Length >= 8 &&
                bytes[0] == 0x89 &&
                bytes[1] == 0x50 &&
                bytes[2] == 0x4E &&
                bytes[3] == 0x47 &&
                bytes[4] == 0x0D &&
                bytes[5] == 0x0A &&
                bytes[6] == 0x1A &&
                bytes[7] == 0x0A;

            if (!isJpeg && !isPng)
                return BadRequest(new
                {
                    message = "Only JPEG and PNG images are allowed."
                });

            string contentType = isJpeg
                ? "image/jpeg"
                : "image/png";

            string extension = isJpeg
                ? ".jpg"
                : ".png";

            string? connectionString =
                _configuration["AzureBlob:ConnectionString"];

            string containerName =
                _configuration["AzureBlob:ContainerName"]
                ?? "event-banners";

            if (string.IsNullOrWhiteSpace(connectionString))
            {
                return Problem(
                    "Azure Blob Storage is not configured.",
                    statusCode: 503);
            }

            try
            {
                var service = new BlobServiceClient(
                    connectionString);

                var container =
                    service.GetBlobContainerClient(
                        containerName);

                await container.CreateIfNotExistsAsync(
                    PublicAccessType.None,
                    cancellationToken: cancellationToken);

                string blobName =
                    $"events/{eventId}/" +
                    $"{Guid.NewGuid():N}{extension}";

                var blob = container.GetBlobClient(
                    blobName);

                imageData.Position = 0;

                await blob.UploadAsync(
                    imageData,
                    new BlobUploadOptions
                    {
                        HttpHeaders = new BlobHttpHeaders
                        {
                            ContentType = contentType
                        }
                    },
                    cancellationToken);

                var eventToUpdate = await _db.Events
                    .FirstAsync(
                        e => e.EventId == eventId,
                        cancellationToken);

                eventToUpdate.BannerBlobName = blobName;

                await _db.SaveChangesAsync(cancellationToken);


                return Created(
                    $"/api/events/{eventId}/banner",
                    new
                    {
                        message = "Event banner uploaded successfully.",
                        eventId,
                        blobName,
                        container = containerName,
                        contentType,
                        sizeBytes = file.Length
                    });
            }
            catch (Azure.RequestFailedException)
            {
                // Do not expose credentials or Azure
                // storage diagnostics to the browser.
                return Problem(
                    "Azure Storage could not complete the upload.",
                    statusCode: 502);
            }
        }
        // =====================================
        // GET EVENT BANNER FROM AZURE
        // =====================================

        [HttpGet]
        [Authorize(Roles = "Organiser,Participant")]
        public async Task<IActionResult> GetBanner(
            int eventId,
            CancellationToken cancellationToken)
        {
            if (eventId <= 0)
                return NotFound();

            var raceEvent = await _db.Events
                .AsNoTracking()
                .Where(e => e.EventId == eventId)
                .Select(e => new { e.BannerBlobName })
                .FirstOrDefaultAsync(cancellationToken);

            if (raceEvent == null ||
                string.IsNullOrWhiteSpace(raceEvent.BannerBlobName))
            {
                return NotFound();
            }

            string? connectionString =
                _configuration["AzureBlob:ConnectionString"];

            if (string.IsNullOrWhiteSpace(connectionString))
                return Problem(
                    "Azure Storage is not configured.",
                    statusCode: 503);

            string containerName =
                _configuration["AzureBlob:ContainerName"]
                ?? "event-banners";

            try
            {
                var service = new BlobServiceClient(connectionString);

                var container =
                    service.GetBlobContainerClient(containerName);

                var blob =
                    container.GetBlobClient(raceEvent.BannerBlobName);

                var download = await blob.DownloadContentAsync(
                    cancellationToken);

                string contentType =
                    download.Value.Details.ContentType;

                if (contentType != "image/jpeg" &&
                    contentType != "image/png")
                {
                    return StatusCode(415);
                }

                Response.Headers.CacheControl = "private, no-store";

                return File(
                    download.Value.Content.ToArray(),
                    contentType);
            }
            catch (Azure.RequestFailedException ex)
                when (ex.Status == 404)
            {
                return NotFound();
            }
            catch (Azure.RequestFailedException)
            {
                return Problem(
                    "Unable to retrieve the Azure banner.",
                    statusCode: 502);
            }
        }

    }
}
