using KicksMvc.Services.Fields;
namespace KicksMvc.Services.Teams;
public sealed class TeamLogoStore(IWebHostEnvironment environment,FieldImageStore validator)
{
    string Root=>Path.Combine(environment.ContentRootPath,"App_Data","team-logos");
    public string PathFor(string key){if(!System.Text.RegularExpressions.Regex.IsMatch(key,@"^[a-f0-9]{32}\.(png|jpg|webp)$"))throw new InvalidDataException("Invalid logo key.");return Path.Combine(Root,key);}
    public async Task<string> Save(IFormFile file){if(file.Length is <=0 or >1048576)throw new InvalidDataException("Choose a PNG, JPG or WebP logo up to 1 MB.");var(bytes,ext)=await validator.ValidateAsync(file);if(bytes.Length>1048576)throw new InvalidDataException("Logo exceeds 1 MB.");Directory.CreateDirectory(Root);var key=Guid.NewGuid().ToString("N")+ext;try{await File.WriteAllBytesAsync(PathFor(key),bytes);return key;}catch{Delete(key);throw;}}
    public void Delete(string key){try{File.Delete(PathFor(key));}catch(IOException){}catch(UnauthorizedAccessException){}}
}
