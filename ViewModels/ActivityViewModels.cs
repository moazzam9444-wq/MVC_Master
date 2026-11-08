using System.ComponentModel.DataAnnotations;
namespace KicksMvc.ViewModels;
public class ActivitySlotInput
{
    [Required] public DateOnly? Date {get;set;}
    [Range(13,22)] public int Hour {get;set;}
    [AllowedValues(60,120)] public int DurationMinutes {get;set;}=60;
}
public sealed class GameInput:ActivitySlotInput
{
    [Range(1,int.MaxValue)] public int FieldId {get;set;}
    [StringLength(120)] public string? Title {get;set;}
    [Range(2,30)] public int Capacity {get;set;}=10;
    [StringLength(40)] public string? Level {get;set;}="All levels";
}
public sealed class GameEditInput
{
    [Required,StringLength(120)] public string Title {get;set;}="";
    [Range(2,30)] public int Capacity {get;set;}=10;
    [StringLength(40)] public string? Level {get;set;}
    [Required,StringLength(4000)] public string Version {get;set;}="";
}
public sealed class ActivityTokenInput {[Required,StringLength(8000)] public string Token {get;set;}="";}
public sealed class ActivityVersionInput {[Required,StringLength(4000)] public string Version {get;set;}="";}
public sealed class GameJoinInput {[RegularExpression("^(Goalkeeper|Defender|Midfielder|Forward)$")] public string Position {get;set;}="Midfielder";}
public sealed class ChallengeInput:ActivitySlotInput
{
    [Range(1,int.MaxValue)] public int TargetTeamId {get;set;}
    [Range(1,int.MaxValue)] public int? FieldId {get;set;}
    [StringLength(300)] public string? Note {get;set;}
}
public sealed class ActivityListInput
{
    [Range(1,100000)] public int Page {get;set;}=1;
    [StringLength(120)] public string? Search {get;set;}
    [RegularExpression("^(browse|mine|joined|sent|received)$")] public string Mode {get;set;}="browse";
}
