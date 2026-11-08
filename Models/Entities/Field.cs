namespace KicksMvc.Models.Entities;

public class Field
{
    public int Id { get; set; }
    public int OwnerId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string Area { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Format { get; set; } = "5v5";
    public decimal HourlyPrice { get; set; }
    public decimal Rating { get; set; } = 0m;
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public string LocationPrecision { get; set; } = "unknown";
    public bool IsArchived { get; set; } = false;
    public DateTimeOffset CreatedAt { get; set; }

    public User Owner { get; set; } = null!;
    public ICollection<FieldImage> Images { get; set; } = new List<FieldImage>();
    public ICollection<FieldAmenity> FieldAmenities { get; set; } = new List<FieldAmenity>();
    public ICollection<Tournament> Tournaments { get; set; } = new List<Tournament>();
    public ICollection<OpenGame> OpenGames { get; set; } = new List<OpenGame>();
    public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
}
