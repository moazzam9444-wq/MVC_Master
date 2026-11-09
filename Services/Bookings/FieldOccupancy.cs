using KicksMvc.Data;
using KicksMvc.Models.Entities;
using Microsoft.EntityFrameworkCore;
namespace KicksMvc.Services.Bookings;
// Every field occupancy writer uses this SQL field-row gate, then this same interval predicate.
public static class FieldOccupancy
{
    public sealed record Interval(DateTimeOffset StartsAt,DateTimeOffset EndsAt);
    public static async Task<Field?> Lock(KicksDbContext db,int id)=>await db.Fields.FromSqlInterpolated($"SELECT * FROM dbo.fields WITH(UPDLOCK,HOLDLOCK) WHERE id={id}").SingleOrDefaultAsync();
    public static async Task<List<Interval>> Blocks(KicksDbContext db,int fieldId,DateTimeOffset start,DateTimeOffset end,int? excludeGame=null,int? excludeTournamentMatch=null)
    {
        var blocks=await db.Bookings.AsNoTracking().Where(b=>b.FieldId==fieldId&&(b.Status=="Pending"||b.Status=="Confirmed")&&b.StartsAt<end&&b.EndsAt>start).Select(b=>new Interval(b.StartsAt,b.EndsAt)).ToListAsync();
        blocks.AddRange(await db.OpenGames.AsNoTracking().Where(g=>g.FieldId==fieldId&&g.Id!=excludeGame&&g.Status!="Cancelled"&&g.StartsAt<end&&g.EndsAt>start).Select(g=>new Interval(g.StartsAt,g.EndsAt)).ToListAsync());
        // Old containers have no per-match schedule. Preserve their legacy occupancy conservatively.
        // Every Phase 6 tournament has a match duration; its event window never reserves the field.
        blocks.AddRange(await db.Tournaments.AsNoTracking().Where(t=>t.MatchDurationMinutes==null&&t.FieldId==fieldId&&t.Status!="Cancelled"&&t.StartsAt<end&&t.EndsAt>start).Select(t=>new Interval(t.StartsAt,t.EndsAt)).ToListAsync());
        blocks.AddRange(await db.TournamentMatches.AsNoTracking().Where(m=>m.Id!=excludeTournamentMatch&&m.Tournament.FieldId==fieldId&&(m.Status=="Scheduled"||m.Status=="Completed")&&m.StartsAt<end&&m.EndsAt>start).Select(m=>new Interval(m.StartsAt!.Value,m.EndsAt!.Value)).ToListAsync());return blocks;
    }
    public static async Task<bool> Occupied(KicksDbContext db,int fieldId,DateTimeOffset start,DateTimeOffset end,int? excludeGame=null,int? excludeTournamentMatch=null)=>(await Blocks(db,fieldId,start,end,excludeGame,excludeTournamentMatch)).Count!=0;
}
