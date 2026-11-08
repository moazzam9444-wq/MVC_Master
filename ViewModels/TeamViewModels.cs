using System.ComponentModel.DataAnnotations;
namespace KicksMvc.ViewModels;
public sealed class TeamInput
{
    [Required, StringLength(36,MinimumLength=3)] public string Name {get;set;}="";
    [Required,StringLength(100)] public string City {get;set;}="";
    [StringLength(120)] public string? Area {get;set;}="";
    [RegularExpression("^(Casual|Intermediate|Competitive)$")] public string Level {get;set;}="Intermediate";
    [RegularExpression("^(5v5|6v6|7v7)$")] public string Format {get;set;}="5v5";
    [Range(5,30)] public int Capacity {get;set;}=12;
    [StringLength(360)] public string? Bio {get;set;}="";
    [StringLength(100)] public string? Schedule {get;set;}="";
    [Required,RegularExpression("^[A-Za-z0-9]{2,3}$")] public string Initials {get;set;}="FC";
    [RegularExpression("^(forest|navy|clay|purple|gold|teal)$")] public string Color {get;set;}="forest";
    public bool Open {get;set;}=true;
    [MaxLength(4)] public string[] Positions {get;set;}=[];
    [StringLength(4000)] public string? Version {get;set;}="";
}
public sealed class TeamJoinInput
{
    [Required,RegularExpression("^(Goalkeeper|Defender|Midfielder|Forward)$")] public string Position {get;set;}="Midfielder";
    [StringLength(300)] public string? Message {get;set;}
}
public sealed class TeamVersionInput { [Required,StringLength(4000)] public string Version {get;set;}=""; }
public sealed class TeamListInput
{
    [Range(1,100000)] public int Page {get;set;}=1;
    [StringLength(120)] public string? Search {get;set;}
    [StringLength(100)] public string? City {get;set;}
    [RegularExpression("^(Casual|Intermediate|Competitive)?$")] public string? Level {get;set;}
    [RegularExpression("^(5v5|6v6|7v7)?$")] public string? Format {get;set;}
    [RegularExpression("^(discover|my|requests)$")] public string Tab {get;set;}="discover";
    [RegularExpression("^(recommended|name|newest)$")] public string Sort {get;set;}="recommended";
    public bool Recruiting {get;set;}
}
