namespace KicksMvc.Models.Entities;

public class Amenity
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;

    public ICollection<FieldAmenity> FieldAmenities { get; set; } = new List<FieldAmenity>();
}
