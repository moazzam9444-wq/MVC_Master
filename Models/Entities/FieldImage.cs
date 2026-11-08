namespace KicksMvc.Models.Entities;

public class FieldImage
{
    public int Id { get; set; }
    public int FieldId { get; set; }
    public string StorageUrl { get; set; } = string.Empty;
    public int SortOrder { get; set; } = 0;

    public Field Field { get; set; } = null!;
}
