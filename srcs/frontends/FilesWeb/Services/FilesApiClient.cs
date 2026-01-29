using System.Net.Http.Json;
using Shared.Models;

namespace FilesWeb.Services;

public class FilesApiClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<FilesApiClient> _logger;

    public FilesApiClient(HttpClient httpClient, ILogger<FilesApiClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<List<FileMetadata>?> GetFilesAsync()
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<List<FileMetadata>>("/api/files");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching files");
            return null;
        }
    }

    public async Task<bool> UploadFileAsync(Stream fileStream, string fileName)
    {
        try
        {
            using var content = new MultipartFormDataContent();
            var streamContent = new StreamContent(fileStream);
            content.Add(streamContent, "file", fileName);

            var response = await _httpClient.PostAsync("/api/files/upload", content);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading file {FileName}", fileName);
            return false;
        }
    }

    public async Task<Stream?> DownloadFileAsync(int id)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/api/files/{id}/download");
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadAsStreamAsync();
            }
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading file {FileId}", id);
            return null;
        }
    }

    public async Task<bool> DeleteFileAsync(int id)
    {
        try
        {
            var response = await _httpClient.DeleteAsync($"/api/files/{id}");
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting file {FileId}", id);
            return false;
        }
    }
}
