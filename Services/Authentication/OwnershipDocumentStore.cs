using System.Text;

namespace KicksMvc.Services.Authentication;

// Private files are never served by static-file middleware. SQL holds opaque storage keys only.
public sealed class OwnershipDocumentStore(IWebHostEnvironment environment)
{
    private string Root => Path.Combine(environment.ContentRootPath, "App_Data", "ownership");

    public async Task<(byte[] Bytes, string Name, string Mime)> ValidateAsync(IFormFile file)
    {
        if (file.Length is <= 0 or > 1048576) throw new InvalidDataException("Upload a document up to 1 MB.");
        var name = Path.GetFileName(file.FileName.Replace('\\', '/')).Trim();
        if (name.Length is < 1 or > 200 || name.Any(char.IsControl))
            throw new InvalidDataException("Use a document filename of 1–200 characters.");
        using var stream = file.OpenReadStream();
        using var buffer = new MemoryStream();
        var chunk = new byte[8192];
        int count;
        while ((count = await stream.ReadAsync(chunk)) != 0)
        {
            if (buffer.Length + count > 1048576) throw new InvalidDataException("Upload a document up to 1 MB.");
            buffer.Write(chunk, 0, count);
        }
        var bytes = buffer.ToArray();
        var extension = Path.GetExtension(name).ToLowerInvariant();
        var mime = extension switch
        {
            ".pdf" when bytes.AsSpan().StartsWith("%PDF-"u8) => "application/pdf",
            ".png" when bytes.AsSpan().StartsWith(new byte[] {137,80,78,71,13,10,26,10}) => "image/png",
            ".jpg" or ".jpeg" when bytes.AsSpan().StartsWith(new byte[] {255,216,255}) => "image/jpeg",
            ".webp" when bytes.Length >= 12 && Encoding.ASCII.GetString(bytes,0,4) == "RIFF" && Encoding.ASCII.GetString(bytes,8,4) == "WEBP" => "image/webp",
            _ => throw new InvalidDataException("Upload a PDF, JPG, PNG or WebP document with matching file contents.")
        };
        if (file.ContentType != mime) throw new InvalidDataException("The document type does not match its contents.");
        return (bytes, name, mime);
    }

    public async Task<string> SaveAsync(byte[] bytes)
    {
        Directory.CreateDirectory(Root);
        var key = Guid.NewGuid().ToString("N") + ".bin";
        try { await File.WriteAllBytesAsync(PathFor(key), bytes); }
        catch { Delete(key); throw; }
        return key;
    }
    public string PathFor(string key)
    {
        if (key.Length != 36 || !key.EndsWith(".bin", StringComparison.Ordinal) ||
            !Guid.TryParseExact(key[..32], "N", out _)) throw new InvalidDataException("Invalid private document key.");
        return Path.Combine(Root, key);
    }
    public void Delete(string key) => File.Delete(PathFor(key));
}
