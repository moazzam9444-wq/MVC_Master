namespace KicksMvc.Models.Entities;

public class OwnershipDocument
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string StorageUrl { get; set; } = string.Empty;
    public string MimeType { get; set; } = string.Empty;
    public int ByteSize { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public User User { get; set; } = null!;
}
