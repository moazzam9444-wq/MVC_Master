namespace KicksMvc.Models.Entities;

public class Setting
{
    public int Id { get; set; } = 1;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? Contact { get; set; }
    public string City { get; set; } = string.Empty;
    public decimal CommissionRate { get; set; } = 10m;
    public decimal ListingFee { get; set; } = 5m;
}
