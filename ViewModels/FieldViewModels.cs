using System.ComponentModel.DataAnnotations;
namespace KicksMvc.ViewModels;

public sealed class FieldInputViewModel
{
    [Required, StringLength(120, MinimumLength = 2)] public string Name { get; set; } = "";
    [Required, StringLength(100)] public string City { get; set; } = "";
    [Required, StringLength(120)] public string Area { get; set; } = "";
    [Required, StringLength(5000, MinimumLength = 10)] public string Description { get; set; } = "";
    [Required, RegularExpression("^(5v5|6v6|7v7)$")] public string Format { get; set; } = "5v5";
    [Range(typeof(decimal), "0", "9999999999.99")] public decimal HourlyPrice { get; set; }
    public List<int> AmenityIds { get; set; } = [];
    public IFormFile? Photo { get; set; }
    public bool RemovePhoto { get; set; }
    [StringLength(2000)] public string? Version { get; set; }
}
public sealed class FieldVersionInput
{
    [Required, StringLength(2000)] public string Version { get; set; } = "";
}
public sealed class FieldQuery
{
    [StringLength(120)] public string? Search { get; set; }
    [StringLength(100)] public string? City { get; set; }
    [RegularExpression("^(5v5|6v6|7v7)$")] public string? Format { get; set; }
    [Range(typeof(decimal), "0", "9999999999.99")] public decimal? Min { get; set; }
    [Range(typeof(decimal), "0", "9999999999.99")] public decimal? Max { get; set; }
    [Range(typeof(decimal), "0", "5")] public decimal? Rating { get; set; }
    [RegularExpression("^(newest|price-low|price-high|rating)$")] public string Sort { get; set; } = "newest";
    [Range(1, 100000)] public int Page { get; set; } = 1;
}
public sealed record AmenityOption(int Id, string Name);
public sealed record FieldDisplay(int Id, string Name, string City, string Area, string Description, string Type,
    decimal Price, decimal Rating, bool IsArchived, string Photo, string[] Images, AmenityOption[] Amenities, string? Version);
public sealed record FieldPage(IReadOnlyList<FieldDisplay> Items, int Total, int Page, int PageSize);
