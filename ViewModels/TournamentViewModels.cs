using System.ComponentModel.DataAnnotations;
namespace KicksMvc.ViewModels;

public class TournamentInput
{
    [Required,StringLength(120)] public string Name {get;set;}="";
    [Range(1,int.MaxValue)] public int FieldId {get;set;}
    [StringLength(2000)] public string? Description {get;set;}
    [Required] public DateTime? RegistrationOpensLocal {get;set;}
    [Required] public DateTime? RegistrationClosesLocal {get;set;}
    [Required] public DateTime? StartsLocal {get;set;}
    [Required] public DateTime? EndsLocal {get;set;}
    [AllowedValues(60,120)] public int MatchDurationMinutes {get;set;}=60;
    [Range(2,64)] public int Capacity {get;set;}=8;
    [Range(typeof(decimal),"0","9999999999.99")] public decimal EntryFee {get;set;}
    [StringLength(200)] public string? Prize {get;set;}
    [StringLength(40)] public string? Level {get;set;}="All levels";
    [Required,AllowedValues("Single elimination")] public string System {get;set;}="Single elimination";
}
public sealed class TournamentEditInput:TournamentInput
{
    [Required,StringLength(4000)] public string Version {get;set;}="";
}
public sealed class TournamentScheduleInput:ActivitySlotInput
{
    [Required,StringLength(4000)] public string Version {get;set;}="";
}
public sealed class TournamentResultInput
{
    [Required,Range(0,99)] public int? HomeScore {get;set;}
    [Required,Range(0,99)] public int? AwayScore {get;set;}
    [Range(0,99)] public int? HomePenalties {get;set;}
    [Range(0,99)] public int? AwayPenalties {get;set;}
    [Required,StringLength(4000)] public string Version {get;set;}="";
}
public sealed class TournamentListInput
{
    [Range(1,100000)] public int Page {get;set;}=1;
    [StringLength(120)] public string? Search {get;set;}
    [RegularExpression("^(browse|mine|registered)$")] public string Mode {get;set;}="browse";
}
