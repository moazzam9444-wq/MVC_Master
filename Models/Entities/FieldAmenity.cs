namespace KicksMvc.Models.Entities;

public class FieldAmenity
{
    public int FieldId { get; set; }
    public int AmenityId { get; set; }

    public Field Field { get; set; } = null!;
    public Amenity Amenity { get; set; } = null!;
}
