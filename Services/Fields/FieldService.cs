using KicksMvc.Data;
using KicksMvc.Models.Entities;
using KicksMvc.ViewModels;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
namespace KicksMvc.Services.Fields;

public sealed class FieldConflictException : Exception { }
public sealed class FieldService(KicksDbContext db, IDataProtectionProvider protection, FieldImageStore images, ILogger<FieldService> logger)
{
    private readonly IDataProtector protector = protection.CreateProtector("Kicks.Fields.AggregateVersion.v1");
    public IQueryable<Field> PublicFields => db.Fields.Where(f => !f.IsArchived && f.Owner.Status == "Active" && db.UserRoles.Any(ur => ur.UserId == f.OwnerId && db.Roles.Any(r => r.Id == ur.RoleId && r.Name == "owner")));
    private IQueryable<Field> Full(IQueryable<Field> query) => query.Include(f => f.Images).Include(f => f.FieldAmenities).ThenInclude(a => a.Amenity).AsSplitQuery();
    public Task<List<AmenityOption>> AmenitiesAsync() => db.Amenities.AsNoTracking().OrderBy(a => a.Name).Select(a => new AmenityOption(a.Id, a.Name)).ToListAsync();
    private string Fingerprint(Field field) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new
    {
        field.Id,
        field.OwnerId,
        field.Name,
        field.City,
        field.Area,
        field.Description,
        field.Format,
        field.HourlyPrice,
        field.Rating,
        field.Latitude,
        field.Longitude,
        field.LocationPrecision,
        field.IsArchived,
        Images = field.Images.OrderBy(i => i.SortOrder).ThenBy(i => i.Id).Select(i => new { i.Id, i.StorageUrl, i.SortOrder }),
        Amenities = field.FieldAmenities.Select(a => a.AmenityId).OrderBy(i => i)
    }))));
    private string Version(Field field) => protector.Protect(field.Id + ":" + Fingerprint(field));
    private void CheckVersion(Field field, string? version)
    {
        try { if (version is null || protector.Unprotect(version) != field.Id + ":" + Fingerprint(field)) throw new FieldConflictException(); }
        catch (CryptographicException) { throw new FieldConflictException(); }
    }
    public FieldDisplay Display(Field f, bool management = false) => new(f.Id, f.Name, f.City, f.Area, f.Description, f.Format, f.HourlyPrice, f.Rating, f.IsArchived,
        f.Images.OrderBy(i => i.SortOrder).Select(i => "/media/fields/" + i.StorageUrl).FirstOrDefault() ?? "/assets/images/field1.jpg",
        f.Images.OrderBy(i => i.SortOrder).Select(i => "/media/fields/" + i.StorageUrl).ToArray(),
        f.FieldAmenities.OrderBy(a => a.AmenityId).Select(a => new AmenityOption(a.AmenityId, a.Amenity.Name)).ToArray(), management ? Version(f) : null);
    public async Task<FieldPage> ListAsync(FieldQuery input, int? ownerId = null)
    {
        var query = ownerId is int owner ? db.Fields.Where(f => f.OwnerId == owner) : PublicFields;
        if (!string.IsNullOrWhiteSpace(input.Search)) { var search = input.Search.Trim(); query = query.Where(f => f.Name.Contains(search) || f.City.Contains(search) || f.Area.Contains(search)); }
        if (!string.IsNullOrWhiteSpace(input.City)) query = query.Where(f => f.City == input.City);
        if (input.Format is not null) query = query.Where(f => f.Format == input.Format);
        if (input.Min is decimal min) query = query.Where(f => f.HourlyPrice >= min);
        if (input.Max is decimal max) query = query.Where(f => f.HourlyPrice <= max);
        if (input.Rating is decimal rating) query = query.Where(f => f.Rating >= rating);
        var total = await query.CountAsync(); const int size = 12; var page = Math.Min(input.Page, Math.Max(1, (int)Math.Ceiling(total / (double)size)));
        query = input.Sort switch { "price-low" => query.OrderBy(f => f.HourlyPrice).ThenBy(f => f.Id), "price-high" => query.OrderByDescending(f => f.HourlyPrice).ThenBy(f => f.Id), "rating" => query.OrderByDescending(f => f.Rating).ThenBy(f => f.Id), _ => query.OrderByDescending(f => f.Id) };
        var rows = await Full(query.AsNoTracking().Skip((page - 1) * size).Take(size)).ToListAsync(); return new(rows.Select(f => Display(f, ownerId != null)).ToList(), total, page, size);
    }
    public async Task<FieldDisplay?> GetAsync(int id, int? owner = null)
    {
        var query = owner is int ownerId ? db.Fields.Where(f => f.OwnerId == ownerId) : PublicFields;
        var field = await Full(query.AsNoTracking()).SingleOrDefaultAsync(f => f.Id == id); return field is null ? null : Display(field, owner != null);
    }
    public async Task<FieldDisplay?> SaveAsync(int ownerId, int? id, FieldInputViewModel input)
    {
        var selected = input.AmenityIds.Distinct().ToArray();
        if (selected.Length > 40 || await db.Amenities.CountAsync(a => selected.Contains(a.Id)) != selected.Length) throw new InvalidDataException("Select valid amenities from the available list.");
        if (input.Photo is not null && input.RemovePhoto) throw new InvalidDataException("Choose replacement or removal, not both.");
        (byte[] Bytes, string Extension)? upload = input.Photo is null ? null : await images.ValidateAsync(input.Photo);
        string? newKey = null; var obsolete = new List<string>(); int savedId;
        await using var transaction = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
        try
        {
            Field field;
            if (id is int fieldId)
            {
                var locked = await Full(db.Fields.FromSqlInterpolated($"SELECT * FROM dbo.fields WITH (UPDLOCK,HOLDLOCK) WHERE id={fieldId} AND owner_id={ownerId}")).SingleOrDefaultAsync();
                if (locked is null) return null; field = locked; CheckVersion(field, input.Version);
                if (field.IsArchived) throw new InvalidDataException("Restore this archived field before editing it.");
            }
            else { field = new Field { OwnerId = ownerId }; db.Fields.Add(field); }
            if (field.City != input.City.Trim() || field.Area != input.Area.Trim()) { field.Latitude = null; field.Longitude = null; field.LocationPrecision = "unknown"; }
            field.Name = input.Name.Trim(); field.City = input.City.Trim(); field.Area = input.Area.Trim(); field.Description = input.Description.Trim(); field.Format = input.Format; field.HourlyPrice = input.HourlyPrice;
            var removed = field.FieldAmenities.Where(a => !selected.Contains(a.AmenityId)).ToList(); db.FieldAmenities.RemoveRange(removed); foreach (var a in removed) field.FieldAmenities.Remove(a);
            foreach (var aid in selected.Except(field.FieldAmenities.Select(a => a.AmenityId))) field.FieldAmenities.Add(new FieldAmenity { AmenityId = aid });
            if (upload is not null || input.RemovePhoto)
            {
                obsolete = field.Images.Select(i => i.StorageUrl).ToList(); db.FieldImages.RemoveRange(field.Images); field.Images.Clear();
                // Flush removals before replacing unique (field_id,sort_order), inside the same transaction.
                if (id != null) await db.SaveChangesAsync();
                if (upload is { } file) { newKey = await images.SaveAsync(file.Bytes, file.Extension); field.Images.Add(new FieldImage { StorageUrl = newKey, SortOrder = 0 }); }
            }
            await db.SaveChangesAsync(); savedId = field.Id; await transaction.CommitAsync();
        }
        catch { if (newKey != null) images.Delete(newKey); throw; }
        foreach (var old in obsolete) try { images.Delete(old); } catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidDataException) { logger.LogWarning("A retired field image could not be removed. Run private file maintenance."); }
        db.ChangeTracker.Clear(); return await GetAsync(savedId, ownerId);
    }
    public async Task<bool> SetArchivedAsync(int ownerId, int id, string version, bool archived)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
        var field = await Full(db.Fields.FromSqlInterpolated($"SELECT * FROM dbo.fields WITH (UPDLOCK,HOLDLOCK) WHERE id={id} AND owner_id={ownerId}")).SingleOrDefaultAsync();
        if (field is null) return false; CheckVersion(field, version); field.IsArchived = archived; await db.SaveChangesAsync(); await transaction.CommitAsync(); return true;
    }
}
