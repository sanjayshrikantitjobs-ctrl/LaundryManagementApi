using LaundryMgmt.Application.Common.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LaundryMgmt.API.Controllers;

/// <summary>
/// Local-disk image upload for garment/service catalog photos, served back out via
/// app.UseStaticFiles() under /uploads. Fine for local dev/single-instance deployment;
/// swap the body of UploadImage for an Azure Blob Storage (or S3) write if you move to
/// a multi-instance/production hosting setup, since local disk storage won't survive
/// a redeploy or be shared across instances.
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[Authorize(Roles = AppRoles.ImageUploadRoles)]
public class UploadsController : ControllerBase
{
    private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png", ".webp", ".gif" };
    private const long MaxFileSizeBytes = 5 * 1024 * 1024; // 5 MB

    private readonly IWebHostEnvironment _env;

    public UploadsController(IWebHostEnvironment env) => _env = env;

    /// <summary>Uploads an image (garment/service photo) and returns its public URL.</summary>
    [HttpPost("images")]
    [RequestSizeLimit(MaxFileSizeBytes)]
    [ProducesResponseType(typeof(UploadedImageDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<UploadedImageDto>> UploadImage(IFormFile file, CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
            return BadRequest("No file uploaded.");

        if (file.Length > MaxFileSizeBytes)
            return BadRequest("File is too large (max 5 MB).");

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!AllowedExtensions.Contains(extension))
            return BadRequest($"Unsupported file type. Allowed: {string.Join(", ", AllowedExtensions)}");

        var webRoot = _env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot");
        var uploadsFolder = Path.Combine(webRoot, "uploads");
        Directory.CreateDirectory(uploadsFolder);

        var fileName = $"{Guid.NewGuid()}{extension}";
        var filePath = Path.Combine(uploadsFolder, fileName);

        await using (var stream = System.IO.File.Create(filePath))
            await file.CopyToAsync(stream, cancellationToken);

        var url = $"{Request.Scheme}://{Request.Host}/uploads/{fileName}";
        return Ok(new UploadedImageDto(url));
    }
}

public record UploadedImageDto(string Url);
