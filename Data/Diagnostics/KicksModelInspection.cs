using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using KicksMvc.Models.Entities;

namespace KicksMvc.Data.Diagnostics;

// Offline metadata inspection only. Never opens a connection, saves changes, or generates a migration.
internal static class KicksModelInspection
{
    public static string Describe(KicksDbContext db, bool verifyBehavior = false)
    {
        var entities = db.GetService<IDesignTimeModel>().Model.GetEntityTypes().OrderBy(e => e.GetTableName()).ToList();
        return JsonSerializer.Serialize(new
        {
            Tables = entities.Count,
            Columns = entities.Sum(e => e.GetProperties().Count()),
            Relationships = entities.Sum(e => e.GetForeignKeys().Count()),
            BehaviorChecks = verifyBehavior ? VerifyBehavior(db) : Array.Empty<string>(),
            DbSets = typeof(KicksDbContext).GetProperties()
                .Where(p => p.PropertyType.IsGenericType && p.PropertyType.GetGenericTypeDefinition() == typeof(DbSet<>))
                .Where(p => entities.Any(e => e.ClrType == p.PropertyType.GenericTypeArguments[0]))
                .Select(p => p.PropertyType.GenericTypeArguments[0].Name).Order().ToArray(),
            Entities = entities.Select(e =>
            {
                var table = StoreObjectIdentifier.Table(e.GetTableName()!, e.GetSchema());
                var instance = Activator.CreateInstance(e.ClrType)!;
                return new
                {
                    Entity = e.ClrType.Name,
                    Table = e.GetTableName(),
                    Schema = e.GetSchema(),
                    Properties = e.GetProperties().Select(p => new
                    {
                        p.Name,
                        Column = p.GetColumnName(table),
                        Type = p.GetColumnType(),
                        ClrType = p.ClrType.FullName,
                        p.IsNullable,
                        p.IsConcurrencyToken,
                        Shadow = p.IsShadowProperty(),
                        Generated = p.ValueGenerated.ToString(),
                        Strategy = p.GetValueGenerationStrategy().ToString(),
                        DefaultSql = p.GetDefaultValueSql(),
                        HasDefault = p.TryGetDefaultValue(out _),
                        DefaultValue = p.TryGetDefaultValue(out var value) ? value : null,
                        InitialValue = p.PropertyInfo?.GetValue(instance),
                        p.Sentinel,
                        ComputedSql = p.GetComputedColumnSql(),
                        Stored = p.GetIsStored()
                    }),
                    Keys = e.GetKeys().Select(k => new
                    {
                        Name = k.GetName(),
                        Primary = k.IsPrimaryKey(),
                        Columns = k.Properties.Select(p => p.GetColumnName(table))
                    }),
                    ForeignKeys = e.GetForeignKeys().Select(f => new
                    {
                        Name = f.GetConstraintName(),
                        Columns = f.Properties.Select(p => p.GetColumnName(table)),
                        PrincipalTable = f.PrincipalEntityType.GetTableName(),
                        PrincipalColumns = f.PrincipalKey.Properties.Select(p => p.GetColumnName()),
                        DeleteBehavior = f.DeleteBehavior.ToString(),
                        f.IsUnique,
                        f.IsRequired,
                        Navigation = f.DependentToPrincipal?.Name,
                        Inverse = f.PrincipalToDependent?.Name
                    }),
                    Indexes = e.GetIndexes().Select(i => new
                    {
                        Name = i.GetDatabaseName(),
                        Columns = i.Properties.Select(p => p.GetColumnName(table)),
                        i.IsUnique,
                        Filter = i.GetFilter()
                    }),
                    Checks = e.GetCheckConstraints().Select(c => new { c.Name, c.Sql }),
                    Navigations = e.GetNavigations().Select(n => new
                    {
                        n.Name,
                        Target = n.TargetEntityType.ClrType.Name,
                        n.IsCollection,
                        Inverse = n.Inverse?.Name
                    })
                };
            })
        });
    }

    private static string[] VerifyBehavior(KicksDbContext db)
    {
        var passed = new List<string>();
        void Check(bool condition, string name)
        {
            if (!condition) throw new InvalidOperationException($"Offline model check failed: {name}");
            passed.Add(name);
        }

        // These are isolated, in-memory change-tracker instances, never persisted or seeded.
        var editable = new (object Entity, string Property, object Value)[]
        {
            (new User { Id = 1, Email = "before@example.invalid" }, nameof(User.Email), "after@example.invalid"),
            (new FieldImage { Id = 1, FieldId = 1 }, nameof(FieldImage.SortOrder), 2),
            (new Amenity { Id = 1, Name = "Before" }, nameof(Amenity.Name), "After"),
            (new Team { Id = 1, CaptainId = 1, Name = "Before" }, nameof(Team.Name), "After"),
            (new Team { Id = 1, CaptainId = 1 }, nameof(Team.CaptainId), 2),
            (new TournamentRegistration { Id = 1, TournamentId = 1, TeamId = 1, CaptainId = 1 }, nameof(TournamentRegistration.TeamId), 2),
            (new TournamentRegistration { Id = 1, TournamentId = 1, TeamId = 1, CaptainId = 1 }, nameof(TournamentRegistration.CaptainId), 2),
            (new TournamentDrawEntry { RegistrationId = 1, TournamentId = 1, SeedPosition = 1 }, nameof(TournamentDrawEntry.SeedPosition), 2),
            (new TournamentMatche { Id = 1, TournamentId = 1, RoundNumber = 1, MatchNumber = 1 }, nameof(TournamentMatche.MatchNumber), 2),
            (new GameParticipant { Id = 1, GameId = 1, UserId = 1 }, nameof(GameParticipant.UserId), 2),
            (new BookingShare { Id = 1, BookingId = 1, TeamId = 1, PayerId = 1 }, nameof(BookingShare.TeamId), 2),
            (new BookingShare { Id = 1, BookingId = 1, TeamId = 1, PayerId = 1 }, nameof(BookingShare.PayerId), 2)
        };
        foreach (var (entity, property, value) in editable)
        {
            db.ChangeTracker.Clear();
            db.Attach(entity);
            db.Entry(entity).Property(property).CurrentValue = value;
            db.ChangeTracker.DetectChanges();
            Check(db.Entry(entity).Property(property).IsModified, $"Editable uniqueness: {entity.GetType().Name}.{property}");
        }

        db.ChangeTracker.Clear();
        var owner = new User { Id = 1 };
        var field = new Field { Id = 2, OwnerId = 1 };
        db.Attach(owner);
        db.Attach(field);
        Check(ReferenceEquals(field.Owner, owner) && owner.OwnedFields.Contains(field), "Field/owner inverse fixup");

        db.ChangeTracker.Clear();
        var team = new Team { Id = 1, CaptainId = 2 };
        var member = new TeamMember { UserId = 2, TeamId = 1 };
        var player = new User { Id = 2 };
        db.Attach(player);
        db.Attach(team);
        db.Attach(member);
        Check(ReferenceEquals(team.Captain, player) && ReferenceEquals(player.CaptainedTeam, team), "Captain one-to-one fixup");
        Check(ReferenceEquals(member.Team, team) && team.Members.Contains(member) && ReferenceEquals(player.TeamMembership, member), "Roster inverse fixup");

        db.ChangeTracker.Clear();
        var frozenTeam = new BookingTeam { BookingId = 1, TeamId = 2, CaptainId = 3 };
        var share = new BookingShare { Id = 4, BookingId = 1, TeamId = 2, PayerId = 3 };
        var payment = new Payment { Id = 5, PayerId = 3, PlatformAdminId = 6, ShareId = 4 };
        db.Attach(frozenTeam);
        db.Attach(share);
        db.Attach(payment);
        Check(ReferenceEquals(share.BookingTeam, frozenTeam) && ReferenceEquals(frozenTeam.Share, share), "Frozen captain composite fixup");
        Check(ReferenceEquals(payment.Share, share) && ReferenceEquals(share.Payment, payment), "Share/receipt one-to-one fixup");

        db.ChangeTracker.Clear();
        var match = new TournamentMatche { Id = 1, TournamentId = 2 };
        var feeder = new TournamentMatche { Id = 3, TournamentId = 2 };
        var slot = new TournamentMatchSlot { MatchId = 1, SlotNumber = 1, TournamentId = 2, SourceMatchId = 3 };
        db.Attach(match);
        db.Attach(feeder);
        db.Attach(slot);
        Check(ReferenceEquals(slot.Match, match) && match.InputSlots.Contains(slot)
            && ReferenceEquals(slot.SourceMatch, feeder) && feeder.OutputSlots.Contains(slot), "Tournament composite match/feed fixup");

        foreach (var entity in new object[] { new Setting { ListingFee = 0m, CommissionRate = 0m }, new Team { Id = 7, CaptainId = 8, Capacity = 5, IsRecruiting = false }, new Payment { Id = 9, IsDemo = false } })
        {
            db.ChangeTracker.Clear();
            db.Add(entity);
            foreach (var name in entity is Setting ? new[] { nameof(Setting.ListingFee), nameof(Setting.CommissionRate) }
                : entity is Team ? new[] { nameof(Team.IsRecruiting) } : new[] { nameof(Payment.IsDemo) })
                Check(db.Entry(entity).Property(name).Metadata.ValueGenerated == ValueGenerated.Never,
                    $"Explicit zero/false retained: {entity.GetType().Name}.{name}");
        }
        db.ChangeTracker.Clear();
        return passed.ToArray();
    }
}
