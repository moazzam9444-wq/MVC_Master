using System.Text;
namespace KicksMvc.Services.Fields;

public sealed class FieldImageStore(IWebHostEnvironment environment)
{
    private string Root => Path.Combine(environment.ContentRootPath, "App_Data", "field-images");
    public async Task<(byte[] Bytes, string Extension)> ValidateAsync(IFormFile file)
    {
        if (file.Length is <= 0 or > 5242880) throw new InvalidDataException("Choose a JPG, PNG or WebP image up to 5 MB.");
        await using var input = file.OpenReadStream(); using var output = new MemoryStream();
        var block = new byte[8192]; int count;
        while ((count = await input.ReadAsync(block)) > 0) { if (output.Length + count > 5242880) throw new InvalidDataException("Image exceeds 5 MB."); output.Write(block, 0, count); }
        var bytes = output.ToArray(); var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        var kind = ext switch
        {
            ".jpg" or ".jpeg" when bytes.Length >= 4 && bytes.AsSpan().StartsWith(new byte[] { 255, 216, 255 }) && bytes[^2] == 255 && bytes[^1] == 217 => (".jpg", "image/jpeg"),
            ".png" when bytes.Length >= 24 && bytes.AsSpan().StartsWith(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }) && Encoding.ASCII.GetString(bytes, 12, 4) == "IHDR" && Encoding.ASCII.GetString(bytes, bytes.Length - 8, 4) == "IEND" => (".png", "image/png"),
            ".webp" when bytes.Length >= 20 && Encoding.ASCII.GetString(bytes, 0, 4) == "RIFF" && Encoding.ASCII.GetString(bytes, 8, 4) == "WEBP" && BitConverter.ToUInt32(bytes, 4) == bytes.Length - 8 => (".webp", "image/webp"),
            _ => throw new InvalidDataException("Image contents must match a JPG, PNG or WebP file.")
        };
        if (file.ContentType != kind.Item2) throw new InvalidDataException("Image MIME type does not match its contents.");
        return (bytes, kind.Item1);
    }
    public async Task<string> SaveAsync(byte[] bytes, string extension)
    {
        Directory.CreateDirectory(Root); var key = Guid.NewGuid().ToString("N") + extension;
        try { await File.WriteAllBytesAsync(PathFor(key), bytes); } catch { Delete(key); throw; }
        return key;
    }
    public string PathFor(string key)
    {
        if (!System.Text.RegularExpressions.Regex.IsMatch(key, "^[a-f0-9]{32}\\.(jpg|png|webp)$")) throw new InvalidDataException("Invalid image key.");
        return Path.Combine(Root, key);
    }
    public void Delete(string key) => File.Delete(PathFor(key));
}
