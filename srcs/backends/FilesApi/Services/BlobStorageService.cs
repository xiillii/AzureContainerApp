using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace FilesApi.Services;

public interface IBlobStorageService
{
    Task<string> UploadFileAsync(Stream fileStream, string fileName, string contentType);
    Task<(Stream Stream, string ContentType)> DownloadFileAsync(string blobName);
    Task DeleteFileAsync(string blobName);
    Task<IEnumerable<string>> ListFilesAsync();
}

public class BlobStorageService : IBlobStorageService
{
    private readonly BlobContainerClient _containerClient;
    private readonly ILogger<BlobStorageService> _logger;

    public BlobStorageService(IConfiguration configuration, ILogger<BlobStorageService> logger)
    {
        _logger = logger;
        var connectionString = configuration["AzureStorage:ConnectionString"];
        var containerName = configuration["AzureStorage:ContainerName"] ?? "files";

        if (string.IsNullOrEmpty(connectionString))
        {
            // For local development, use Azurite
            connectionString = "UseDevelopmentStorage=true";
        }

        var blobServiceClient = new BlobServiceClient(connectionString);
        _containerClient = blobServiceClient.GetBlobContainerClient(containerName);
        _containerClient.CreateIfNotExists();
    }

    public async Task<string> UploadFileAsync(Stream fileStream, string fileName, string contentType)
    {
        var blobName = $"{Guid.NewGuid()}_{fileName}";
        var blobClient = _containerClient.GetBlobClient(blobName);

        await blobClient.UploadAsync(fileStream, new BlobHttpHeaders { ContentType = contentType });
        
        _logger.LogInformation("File uploaded: {BlobName}", blobName);
        return blobName;
    }

    public async Task<(Stream Stream, string ContentType)> DownloadFileAsync(string blobName)
    {
        var blobClient = _containerClient.GetBlobClient(blobName);
        var download = await blobClient.DownloadAsync();
        
        var contentType = download.Value.Details.ContentType;
        _logger.LogInformation("File downloaded: {BlobName}", blobName);
        
        return (download.Value.Content, contentType);
    }

    public async Task DeleteFileAsync(string blobName)
    {
        var blobClient = _containerClient.GetBlobClient(blobName);
        await blobClient.DeleteIfExistsAsync();
        
        _logger.LogInformation("File deleted: {BlobName}", blobName);
    }

    public async Task<IEnumerable<string>> ListFilesAsync()
    {
        var blobs = new List<string>();
        
        await foreach (var blobItem in _containerClient.GetBlobsAsync())
        {
            blobs.Add(blobItem.Name);
        }
        
        return blobs;
    }
}
