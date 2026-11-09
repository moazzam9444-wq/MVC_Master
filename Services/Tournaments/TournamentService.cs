using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using System.Text.Json;
using KicksMvc.Data;
using KicksMvc.Models.Entities;
using KicksMvc.Services.Activities;
using KicksMvc.Services.Bookings;
using KicksMvc.Services.Fields;
using KicksMvc.ViewModels;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
namespace KicksMvc.Services.Tournaments;

public sealed partial class TournamentService(KicksDbContext db,FieldService fields,ActivityRules rules,IDataProtectionProvider protection)
{
    readonly IDataProtector quotes=protection.CreateProtector("Kicks.Tournaments.Creation.v1");
    sealed record Quote(int Actor,Guid Key,TournamentInput Input,DateTimeOffset Expiry);
    sealed record Window(DateTimeOffset Open,DateTimeOffset Close,DateTimeOffset Start,DateTimeOffset End,int Minutes);
    static DateTimeOffset Amman(DateTime? value)
    {
        if(value is null||value.Value.Kind!=DateTimeKind.Unspecified)throw new ActivityException("Enter Amman local dates and times without a timezone suffix.",400);
        try{return new(value.Value,TimeSpan.FromHours(3));}catch(ArgumentOutOfRangeException){throw new ActivityException("The submitted Amman date is outside the supported range.",400);}
    }
    Window Validate(TournamentInput input,bool future)
    {
        if(!Validator.TryValidateObject(input,new ValidationContext(input),[],true)||string.IsNullOrWhiteSpace(input.Name)||decimal.Round(input.EntryFee,2)!=input.EntryFee)throw new ActivityException("Check tournament text, capacity, format, fee and dates.",400);
        var open=Amman(input.RegistrationOpensLocal);var close=Amman(input.RegistrationClosesLocal);var start=Amman(input.StartsLocal);var end=Amman(input.EndsLocal);
        if(open.TimeOfDay.Ticks%TimeSpan.TicksPerMinute!=0||close.TimeOfDay.Ticks%TimeSpan.TicksPerMinute!=0||open>=close||close>start||end<=start||!rules.Valid(start,input.MatchDurationMinutes)||end.Hour is <14 or >23||end.TimeOfDay.Ticks%TimeSpan.TicksPerHour!=0||end<start.AddMinutes(input.MatchDurationMinutes)||(end-start).TotalMinutes>int.MaxValue||(future&&(start<=rules.Now||close<=rules.Now)))throw new ActivityException("Use an ordered registration window ending by tournament start, with a future 13:00–23:00 Amman event window and 60/120-minute matches.",400);
        return new(open,close,start,end,checked((int)(end-start).TotalMinutes));
    }
    async Task<Field> Eligible(int fieldId)=>await fields.PublicFields.AsNoTracking().SingleOrDefaultAsync(f=>f.Id==fieldId)??throw new ActivityException("The field or its owner is unavailable.",404);
    static void Owner(Tournament t,Field f,int actor){if(t.CreatedBy!=actor||f.OwnerId!=actor)throw new ActivityException("Only the tournament's field owner may manage it.",403);}
    static void Modern(Tournament t){if(t.MatchDurationMinutes is null)throw new ActivityException("Legacy tournament history is read-only; create a new tournament to use fixture scheduling.");}
    async Task<(Tournament Tournament,Field Field)> Gate(int id)
    {
        // All tournament mutations take the shared field gate BEFORE the tournament row.
        var fieldId=await db.Tournaments.AsNoTracking().Where(t=>t.Id==id).Select(t=>(int?)t.FieldId).SingleOrDefaultAsync()??throw new ActivityException("Tournament not found.",404);
        var field=await FieldOccupancy.Lock(db,fieldId)??throw new ActivityException("Field not found.",404);
        var t=await db.Tournaments.FromSqlInterpolated($"SELECT * FROM dbo.tournaments WITH(UPDLOCK,HOLDLOCK) WHERE id={id}").SingleAsync();
        if(t.FieldId!=fieldId)throw new ActivityException("The tournament changed. Refresh.");return(t,field);
    }
    async Task<string> Version(Tournament t)
    {
        var entries=await db.TournamentRegistrations.AsNoTracking().Where(r=>r.TournamentId==t.Id).OrderBy(r=>r.Id).Select(r=>new{r.Id,r.Status}).ToListAsync();
        var matches=await db.TournamentMatches.AsNoTracking().Where(m=>m.TournamentId==t.Id).OrderBy(m=>m.Id).Select(m=>new{m.Id,m.Status,m.StartsAt,m.UpdatedAt,m.WinnerRegistrationId}).ToListAsync();
        return rules.Version(new{Kind="Tournament",t.Id,t.CreatedBy,t.FieldId,t.Name,t.Description,t.StartsAt,t.DurationMinutes,t.Capacity,t.EntryFee,t.Prize,t.System,t.Level,t.RegistrationOpensAt,t.RegistrationClosesAt,t.MatchDurationMinutes,t.Status,t.DrawStartedAt,entries,matches});
    }
    async Task<bool> Completed(int id)=>await db.TournamentMatches.AnyAsync(m=>m.TournamentId==id&&m.RoundNumber==db.TournamentMatches.Where(x=>x.TournamentId==id).Max(x=>x.RoundNumber)&&m.Status=="Completed"&&m.WinnerRegistrationId!=null);
    async Task Editable(Tournament t){Modern(t);if(t.Status=="Cancelled"||await Completed(t.Id))throw new ActivityException("Cancelled and completed tournaments retain read-only history.");}
    public async Task<object> QuoteCreation(int actor,TournamentInput input)
    {
        var w=Validate(input,true);await using var tx=await db.Database.BeginTransactionAsync();var field=await FieldOccupancy.Lock(db,input.FieldId)??throw new ActivityException("Field not found.",404);await Eligible(field.Id);if(field.OwnerId!=actor)throw new ActivityException("Choose your own field.",403);
        var q=new Quote(actor,Guid.NewGuid(),input,rules.Now.AddMinutes(15));await tx.CommitAsync();return new{Token=quotes.Protect(JsonSerializer.Serialize(q)),StartsAt=w.Start,EndsAt=w.End,input.MatchDurationMinutes,input.Capacity,input.EntryFee,Notice="Only explicitly scheduled matches reserve the field. No payment is processed."};
    }
    public async Task<(int Id,bool Replay)> Create(int actor,string token)
    {
        Quote q;try{q=JsonSerializer.Deserialize<Quote>(quotes.Unprotect(token))??throw new CryptographicException();}catch(Exception e)when(e is CryptographicException or FormatException or ArgumentException or JsonException){throw new ActivityException("Invalid tournament creation quote.",400);}
        if(q.Actor!=actor)throw new ActivityException("This creation quote belongs to another owner.",403);
        await using var tx=await db.Database.BeginTransactionAsync();var field=await FieldOccupancy.Lock(db,q.Input.FieldId)??throw new ActivityException("Field unavailable.",404);if(field.OwnerId!=actor)throw new ActivityException("Choose your own field.",403);
        var old=await db.Tournaments.SingleOrDefaultAsync(t=>t.CreationKey==q.Key);if(old is not null){await tx.CommitAsync();return(old.Id,true);}
        if(q.Expiry<rules.Now)throw new ActivityException("The creation quote expired. Review the tournament again.");var w=Validate(q.Input,true);await Eligible(field.Id);
        var t=new Tournament{CreatedBy=actor,FieldId=field.Id,Name=q.Input.Name.Trim(),Description=q.Input.Description?.Trim(),RegistrationOpensAt=w.Open,RegistrationClosesAt=w.Close,StartsAt=w.Start,DurationMinutes=w.Minutes,MatchDurationMinutes=q.Input.MatchDurationMinutes,Capacity=q.Input.Capacity,EntryFee=q.Input.EntryFee,Prize=q.Input.Prize?.Trim()??"",Level=q.Input.Level?.Trim()??"All levels",System=q.Input.System,CreationKey=q.Key};
        db.Tournaments.Add(t);await db.SaveChangesAsync();await tx.CommitAsync();return(t.Id,false);
    }
    public async Task Edit(int actor,int id,TournamentEditInput input)
    {
        await using var tx=await db.Database.BeginTransactionAsync();var(t,field)=await Gate(id);Owner(t,field,actor);await Editable(t);rules.Check(input.Version,await Version(t));var w=Validate(input,false);
        if(input.FieldId!=field.Id)throw new ActivityException("A tournament stays on its original field. Cancel and create another to change fields.");
        var core=t.StartsAt!=w.Start||t.DurationMinutes!=w.Minutes||t.Capacity!=input.Capacity||t.EntryFee!=input.EntryFee||t.MatchDurationMinutes!=input.MatchDurationMinutes||t.RegistrationOpensAt!=w.Open||t.RegistrationClosesAt!=w.Close||t.System!=input.System;
        if(core){if(t.DrawStartedAt!=null||await db.TournamentRegistrations.AnyAsync(r=>r.TournamentId==id))throw new ActivityException("Registration dates, fee, capacity and event schedule are fixed after the first entry.");Validate(input,true);await Eligible(field.Id);t.StartsAt=w.Start;t.DurationMinutes=w.Minutes;t.RegistrationOpensAt=w.Open;t.RegistrationClosesAt=w.Close;t.Capacity=input.Capacity;t.EntryFee=input.EntryFee;t.MatchDurationMinutes=input.MatchDurationMinutes;t.System=input.System;}
        t.Name=input.Name.Trim();t.Description=input.Description?.Trim();t.Prize=input.Prize?.Trim()??"";t.Level=input.Level?.Trim()??"All levels";await db.SaveChangesAsync();await tx.CommitAsync();
    }
    public async Task Cancel(int actor,int id,string version)
    {
        await using var tx=await db.Database.BeginTransactionAsync();var(t,field)=await Gate(id);Owner(t,field,actor);await Editable(t);rules.Check(version,await Version(t));
        if(await db.TournamentMatches.AnyAsync(m=>m.TournamentId==id&&m.Status=="Scheduled"&&m.StartsAt<=rules.Now&&m.EndsAt>rules.Now))throw new ActivityException("An ongoing match must finish before cancelling the remaining tournament.");
        var future=await db.TournamentMatches.Where(m=>m.TournamentId==id&&(m.Status=="Draft"||m.Status=="Scheduled"&&m.StartsAt>rules.Now)).ToListAsync();foreach(var m in future){m.Status="Cancelled";m.UpdatedBy=actor;m.UpdatedAt=rules.Now;}
        t.Status="Cancelled";t.CancelledBy=actor;t.CancelledAt=rules.Now;await db.SaveChangesAsync();await tx.CommitAsync();
    }
}
