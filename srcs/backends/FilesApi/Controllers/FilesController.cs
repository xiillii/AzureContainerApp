using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shared.Models;
using System.Security.Claims;
using FilesApi.Data;
using FilesApi.Services;

namespace FilesApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class FilesController : ControllerBase
{
    private readonly FilesDbContext _context;
    private readonly IBlobStorageService _blobStorage;
    private readonly ILogger<FilesController> _logger;

    public FilesController(FilesDbContext context, IBlobStorageService blobStorage, ILogger<FilesController> logger)
    {
        _context = context;
        _blobStorage = blobStorage;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<FileMetadata>>> GetFiles()
    {
        var files = await _context.Files.ToListAsync();
        return Ok(files);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<FileMetadata>> GetFile(int id)
    {
        var file = await _context.Files.FindAsync(id);

        if (file == null)
        {
            return NotFound();
        }

        return Ok(file);
    }

    [HttpPost("upload")]
    public async Task<ActionResult<FileMetadata>> UploadFile(IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest("No file uploaded");
        }

        var username = User.FindFirst(ClaimTypes.Name)?.Value ?? "unknown";

        using var stream = file.OpenReadStream();
        var blobName = await _blobStorage.UploadFileAsync(stream, file.FileName, file.ContentType);

        var metadata = new FileMetadata
        {
            FileName = file.FileName,
            BlobName = blobName,
            FileSize = file.Length,
            ContentType = file.ContentType,
            UploadedAt = DateTime.UtcNow,
            UploadedBy = username
        };

        _context.Files.Add(metadata);
        await _context.SaveChangesAsync();

        _logger.LogInformation("File {FileName} uploaded by {Username}", file.FileName, username);

        return CreatedAtAction(nameof(GetFile), new { id = metadata.Id }, metadata);
    }

    [HttpGet("{id}/download")]
    public async Task<IActionResult> DownloadFile(int id)
    {
        var metadata = await _context.Files.FindAsync(id);
        
        if (metadata == null)
        {
            return NotFound();
        }

        try
        {
            var (stream, contentType) = await _blobStorage.DownloadFileAsync(metadata.BlobName);
            return File(stream, contentType, metadata.FileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading file {FileId}", id);
            return StatusCode(500, "Error downloading file");
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteFile(int id)
    {
        var metadata = await _context.Files.FindAsync(id);
        
        if (metadata == null)
        {
            return NotFound();
        }

        try
        {
            await _blobStorage.DeleteFileAsync(metadata.BlobName);
            _context.Files.Remove(metadata);
            await _context.SaveChangesAsync();

            _logger.LogInformation("File {FileId} deleted", id);

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting file {FileId}", id);
            return StatusCode(500, "Error deleting file");
        }
    }
}
