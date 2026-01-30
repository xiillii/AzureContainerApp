using Microsoft.AspNetCore.Mvc;
using FilesWeb.Services;

namespace FilesWeb.Controllers;

public class FilesController : Controller
{
    private readonly FilesApiClient _filesApi;
    private readonly AuthService _authService;
    private readonly ILogger<FilesController> _logger;

    public FilesController(FilesApiClient filesApi, AuthService authService, ILogger<FilesController> logger)
    {
        _filesApi = filesApi;
        _authService = authService;
        _logger = logger;
    }

    public async Task<IActionResult> Index()
    {
        if (!_authService.IsAuthenticated)
        {
            return RedirectToAction("Login", "Account");
        }

        try
        {
            var files = await _filesApi.GetFilesAsync();
            return View(files ?? new List<Shared.Models.FileMetadata>());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load files");
            ViewBag.ErrorMessage = "Failed to load files. Please try again.";
            return View(new List<Shared.Models.FileMetadata>());
        }
    }

    [HttpPost]
    public async Task<IActionResult> Upload(IFormFile file)
    {
        if (!_authService.IsAuthenticated)
        {
            return RedirectToAction("Login", "Account");
        }

        if (file == null || file.Length == 0)
        {
            TempData["ErrorMessage"] = "Please select a file to upload";
            return RedirectToAction("Index");
        }

        try
        {
            using var stream = file.OpenReadStream();
            await _filesApi.UploadFileAsync(stream, file.FileName);
            TempData["SuccessMessage"] = $"File '{file.FileName}' uploaded successfully";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upload file");
            TempData["ErrorMessage"] = $"Failed to upload file: {ex.Message}";
        }

        return RedirectToAction("Index");
    }

    [HttpGet]
    public async Task<IActionResult> Download(int id, string fileName)
    {
        if (!_authService.IsAuthenticated)
        {
            return RedirectToAction("Login", "Account");
        }

        try
        {
            var fileData = await _filesApi.DownloadFileAsync(id);
            
            if (fileData == null)
            {
                TempData["ErrorMessage"] = "File not found";
                return RedirectToAction("Index");
            }

            return File(fileData, "application/octet-stream", fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to download file");
            TempData["ErrorMessage"] = $"Failed to download file: {ex.Message}";
            return RedirectToAction("Index");
        }
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        if (!_authService.IsAuthenticated)
        {
            return RedirectToAction("Login", "Account");
        }

        try
        {
            await _filesApi.DeleteFileAsync(id);
            TempData["SuccessMessage"] = "File deleted successfully";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete file");
            TempData["ErrorMessage"] = $"Failed to delete file: {ex.Message}";
        }

        return RedirectToAction("Index");
    }
}
