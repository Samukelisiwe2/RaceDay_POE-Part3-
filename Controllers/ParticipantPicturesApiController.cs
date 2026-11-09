
using Azure;
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
    [Route("api/profiles/participant/picture")]
    [Authorize(Roles = "Participant")]
    public class ParticipantPicturesApiController : ControllerBase
    {
        private const long MaxFileSize = 5 * 1024 * 1024;

        private readonly ApplicationDbContext _db;
        private readonly UserManager<IdentityUser> _users;
        private readonly IConfiguration _config;

        public ParticipantPicturesApiController(
            ApplicationDbContext db,
            UserManager<IdentityUser> users,
            IConfiguration config)
        {
            _db = db;
            _users = users;
            _config = config;
        }

        // POST: api/profiles/participant/picture
        [HttpPost]
        [Consumes("multipart/form-data")]
        [RequestSizeLimit(6 * 1024 * 1024)]
        public async Task<IActionResult> Upload(
            IFormFile file,
            CancellationToken cancellationToken)
        {
            var user = await _users.GetUserAsync(User);

            if (user?.Email == null)
                return Challenge();

            var participant = await _db.Participants
                .FirstOrDefaultAsync(
                    p => p.Email == user.Email,
                    cancellationToken);

            if (participant == null)
                return BadRequest(new
                {
                    message = "Complete your profile first."
                });

            if (file == null || file.Length == 0)
                return BadRequest(new
                {
                    message = "Please select an image."
                });

            if (file.Length > MaxFileSize)
                return BadRequest(new
                {
                    message = "Maximum file size is 5 MB."
                });

            await using var stream = new MemoryStream();

            await file.CopyToAsync(
                stream, cancellationToken);

            byte[] bytes = stream.ToArray();

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

            string contentType =
                isJpeg ? "image/jpeg" : "image/png";

            string extension =
                isJpeg ? ".jpg" : ".png";

            string? connectionString =
                _config["AzureBlob:ConnectionString"];

            if (string.IsNullOrWhiteSpace(connectionString))
                return Problem(
                    "Azure storage is not configured.",
                    statusCode: 503);

            string containerName =
                _config["AzureBlob:ContainerName"]
                ?? "event-banners";

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
                    $"participants/{participant.ParticipantId}/" +
                    $"{Guid.NewGuid():N}{extension}";

                var blob = container.GetBlobClient(blobName);

                stream.Position = 0;

                await blob.UploadAsync(
                    stream,
                    new BlobUploadOptions
                    {
                        HttpHeaders = new BlobHttpHeaders
                        {
                            ContentType = contentType
                        }
                    },
                    cancellationToken);

                participant.ProfilePictureBlobName = blobName;

                await _db.SaveChangesAsync(cancellationToken);

                return Ok(new
                {
                    message = "Profile picture uploaded successfully."
                });
            }
            catch (RequestFailedException)
            {
                return Problem(
                    "Azure storage could not complete the upload.",
                    statusCode: 502);
            }
        }

        // GET: api/profiles/participant/picture
        [HttpGet]
        public async Task<IActionResult> GetPicture(
            CancellationToken cancellationToken)
        {
            var user = await _users.GetUserAsync(User);

            if (user?.Email == null)
                return Challenge();

            var participant = await _db.Participants
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    p => p.Email == user.Email,
                    cancellationToken);

            if (participant == null ||
                string.IsNullOrWhiteSpace(
                    participant.ProfilePictureBlobName))
            {
                return NotFound();
            }

            string? connectionString =
                _config["AzureBlob:ConnectionString"];

            if (string.IsNullOrWhiteSpace(connectionString))
                return StatusCode(503);

            string containerName =
                _config["AzureBlob:ContainerName"]
                ?? "event-banners";

            try
            {
                var service = new BlobServiceClient(
                    connectionString);

                var blob = service
                    .GetBlobContainerClient(containerName)
                    .GetBlobClient(
                        participant.ProfilePictureBlobName);

                var download =
                    await blob.DownloadContentAsync(
                        cancellationToken);

                string contentType =
                    download.Value.Details.ContentType;

                if (contentType != "image/jpeg" &&
                    contentType != "image/png")
                {
                    return StatusCode(415);
                }

                Response.Headers.CacheControl =
                    "private, no-store";

                return File(
                    download.Value.Content.ToArray(),
                    contentType);
            }
            catch (RequestFailedException ex)
                when (ex.Status == 404)
            {
                return NotFound();
            }
            catch (RequestFailedException)
            {
                return StatusCode(502);
            }
        }
    }
}
