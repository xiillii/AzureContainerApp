namespace Shared.Models;

public class FileMetadata
{
    public int Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string BlobName { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string ContentType { get; set; } = string.Empty;
    public DateTime UploadedAt { get; set; }
    public string UploadedBy { get; set; } = string.Empty;
}
