using KicksMvc.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using KicksMvc.Models.Identity;
using KicksMvc.Models.Legacy;

namespace KicksMvc.Data;

// Mapped from the SQL design. No automatic database creation or migration.
// Unreferenced UNIQUE constraints use unique indexes; referenced composite candidate keys remain alternate keys.
// Navigation-free FKs alongside composite navigations intentionally retain all original SQL constraints.
// Literal defaults match CLR initializers and are always sent by EF, preserving explicit zero/false values.
// Database-generated timestamps and computed columns retain their generated-value configuration.
public class KicksDbContext(DbContextOptions<KicksDbContext> options) : IdentityDbContext<User, IdentityRole<int>, int>(options)
{
    // Version 2 is deliberate: standard password/role tables, without unused passkey schema.
    protected override Version SchemaVersion => IdentitySchemaVersions.Version2;
    public DbSet<LegacyMeeting> LegacyMeetings => Set<LegacyMeeting>();
    public DbSet<OwnershipDocument> OwnershipDocuments => Set<OwnershipDocument>();
    public DbSet<Field> Fields => Set<Field>();
    public DbSet<FieldImage> FieldImages => Set<FieldImage>();
    public DbSet<Amenity> Amenities => Set<Amenity>();
    public DbSet<FieldAmenity> FieldAmenities => Set<FieldAmenity>();
    public DbSet<Team> Teams => Set<Team>();
    public DbSet<TeamMember> TeamMembers => Set<TeamMember>();
    public DbSet<TeamRecruitingPosition> TeamRecruitingPositions => Set<TeamRecruitingPosition>();
    public DbSet<TeamRequest> TeamRequests => Set<TeamRequest>();
    public DbSet<Tournament> Tournaments => Set<Tournament>();
    public DbSet<TournamentRegistration> TournamentRegistrations => Set<TournamentRegistration>();
    public DbSet<RegistrationPlayer> RegistrationPlayers => Set<RegistrationPlayer>();
    public DbSet<TournamentDrawEntry> TournamentDrawEntries => Set<TournamentDrawEntry>();
    public DbSet<TournamentMatche> TournamentMatches => Set<TournamentMatche>();
    public DbSet<TournamentMatchSlot> TournamentMatchSlots => Set<TournamentMatchSlot>();
    public DbSet<OpenGame> OpenGames => Set<OpenGame>();
    public DbSet<GameParticipant> GameParticipants => Set<GameParticipant>();
    public DbSet<Challenge> Challenges => Set<Challenge>();
    public DbSet<ChallengeRequest> ChallengeRequests => Set<ChallengeRequest>();
    public DbSet<Booking> Bookings => Set<Booking>();
    public DbSet<BookingTeam> BookingTeams => Set<BookingTeam>();
    public DbSet<BookingShare> BookingShares => Set<BookingShare>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<GatewayPayment> GatewayPayments => Set<GatewayPayment>();
    public DbSet<StripeCheckoutAttempt> StripeCheckoutAttempts => Set<StripeCheckoutAttempt>();
    public DbSet<StripeWebhookEvent> StripeWebhookEvents => Set<StripeWebhookEvent>();
    public DbSet<Message> Messages => Set<Message>();
    public DbSet<Feedback> Feedback => Set<Feedback>();
    public DbSet<Setting> Settings => Set<Setting>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        GatewayPaymentMapping.Configure(modelBuilder);
        modelBuilder.UseCollation("Latin1_General_100_CI_AS_SC");
        modelBuilder.Entity<User>(e =>
        {
            e.ToTable("users", "dbo", table =>
            {
                table.HasCheckConstraint("CK_users_status_enum", "[status] IN (N'Pending', N'Active', N'Rejected', N'Inactive')");
                table.HasCheckConstraint("CK_users_requested_role_enum", "[requested_role] IN (N'player', N'owner')");
            });
            e.HasKey("Id").HasName("PK_users");
            e.Property(x => x.Id).HasColumnName("id").HasColumnType("int").IsRequired().UseIdentityColumn();
            e.Property(x => x.FullName).HasColumnName("full_name").HasColumnType("nvarchar(100)").IsRequired();
            e.Property(x => x.Email).HasColumnName("email").HasColumnType("nvarchar(254)").HasMaxLength(254).IsRequired();
            e.Property(x => x.PasswordHash).HasColumnName("password_hash").HasColumnType("nvarchar(max)");
            e.Property(x => x.UserName).HasColumnName("user_name");
            e.Property(x => x.NormalizedUserName).HasColumnName("normalized_user_name");
            e.Property(x => x.NormalizedEmail).HasColumnName("normalized_email").HasMaxLength(254);
            e.Property(x => x.EmailConfirmed).HasColumnName("email_confirmed");
            e.Property(x => x.SecurityStamp).HasColumnName("security_stamp");
            e.Property(x => x.ConcurrencyStamp).HasColumnName("concurrency_stamp");
            e.Property(x => x.PhoneNumberConfirmed).HasColumnName("phone_number_confirmed");
            e.Property(x => x.TwoFactorEnabled).HasColumnName("two_factor_enabled");
            e.Property(x => x.LockoutEnd).HasColumnName("lockout_end");
            e.Property(x => x.LockoutEnabled).HasColumnName("lockout_enabled");
            e.Property(x => x.AccessFailedCount).HasColumnName("access_failed_count");
            e.Property(x => x.Status).HasColumnName("status").HasColumnType("varchar(24)").IsRequired().HasDefaultValue("Pending").ValueGeneratedNever();
            e.Property(x => x.RequestedRole).HasColumnName("requested_role").HasColumnType("varchar(24)");
            e.Property(x => x.ReviewedBy).HasColumnName("reviewed_by").HasColumnType("int");
            e.Property(x => x.ReviewedAt).HasColumnName("reviewed_at").HasColumnType("datetimeoffset(0)");
            e.Property(x => x.City).HasColumnName("city").HasColumnType("nvarchar(100)");
            e.Property(x => x.PhoneNumber).HasColumnName("phone").HasColumnType("nvarchar(30)").HasMaxLength(30);
            e.Property(x => x.Address).HasColumnName("address").HasColumnType("nvarchar(300)");
            e.Property(x => x.AvatarUrl).HasColumnName("avatar_url").HasColumnType("nvarchar(max)");
            e.Property(x => x.CreatedAt).HasColumnName("created_at").HasColumnType("datetimeoffset(0)").IsRequired().HasDefaultValueSql("SYSUTCDATETIME()");
            e.HasOne<User>().WithMany().HasForeignKey("ReviewedBy").HasPrincipalKey("Id").OnDelete(DeleteBehavior.NoAction).HasConstraintName("FK_users_reviewed_by_0");
            e.HasIndex(new[] { "Email" }).HasDatabaseName("UQ_users_email").IsUnique();
            e.HasIndex(x => x.NormalizedEmail).HasDatabaseName("EmailIndex").IsUnique().HasFilter("[normalized_email] IS NOT NULL");
            e.HasIndex(new[] { "ReviewedBy" }).HasDatabaseName("IX_users_0");
        });
        modelBuilder.Entity<OwnershipDocument>(e =>
        {
            e.ToTable("ownership_documents", "dbo", table =>
            {
                table.HasCheckConstraint("CK_ownership_documents_byte_size", "[byte_size] BETWEEN 1 AND 1048576");
            });
            e.HasKey("Id").HasName("PK_ownership_documents");
            e.Property(x => x.Id).HasColumnName("id").HasColumnType("int").IsRequired().UseIdentityColumn();
            e.Property(x => x.UserId).HasColumnName("user_id").HasColumnType("int").IsRequired();
            e.Property(x => x.FileName).HasColumnName("file_name").HasColumnType("nvarchar(200)").IsRequired();
            e.Property(x => x.StorageUrl).HasColumnName("storage_url").HasColumnType("nvarchar(1000)").IsRequired();
            e.Property(x => x.MimeType).HasColumnName("mime_type").HasColumnType("varchar(80)").IsRequired();
            e.Property(x => x.ByteSize).HasColumnName("byte_size").HasColumnType("int").IsRequired();
            e.Property(x => x.CreatedAt).HasColumnName("created_at").HasColumnType("datetimeoffset(0)").IsRequired().HasDefaultValueSql("SYSUTCDATETIME()");
            e.HasOne<User>(x => x.User).WithMany(x => x.OwnershipDocuments).HasForeignKey("UserId").HasPrincipalKey("Id").OnDelete(DeleteBehavior.NoAction).HasConstraintName("FK_ownership_documents_user_id_0");
            e.HasIndex(new[] { "UserId" }).HasDatabaseName("IX_ownership_documents_0");
        });
        modelBuilder.Entity<Field>(e =>
        {
            e.ToTable("fields", "dbo", table =>
            {
                table.HasCheckConstraint("CK_fields_format_enum", "[format] IN (N'5v5', N'6v6', N'7v7')");
                table.HasCheckConstraint("CK_fields_hourly_price", "[hourly_price] >= 0");
                table.HasCheckConstraint("CK_fields_rating", "[rating] BETWEEN 0 AND 5");
                table.HasCheckConstraint("CK_fields_latitude", "[latitude] BETWEEN -90 AND 90");
                table.HasCheckConstraint("CK_fields_longitude", "[longitude] BETWEEN -180 AND 180");
                table.HasCheckConstraint("CK_fields_location_precision_enum", "[location_precision] IN (N'exact', N'area', N'city', N'unknown')");
            });
            e.HasKey("Id").HasName("PK_fields");
            e.Property(x => x.Id).HasColumnName("id").HasColumnType("int").IsRequired().UseIdentityColumn();
            e.Property(x => x.OwnerId).HasColumnName("owner_id").HasColumnType("int").IsRequired();
            e.Property(x => x.Name).HasColumnName("name").HasColumnType("nvarchar(120)").IsRequired();
            e.Property(x => x.City).HasColumnName("city").HasColumnType("nvarchar(100)").IsRequired();
            e.Property(x => x.Area).HasColumnName("area").HasColumnType("nvarchar(120)").IsRequired();
            e.Property(x => x.Description).HasColumnName("description").HasColumnType("nvarchar(max)").IsRequired();
            e.Property(x => x.Format).HasColumnName("format").HasColumnType("varchar(24)").IsRequired().HasDefaultValue("5v5").ValueGeneratedNever();
            e.Property(x => x.HourlyPrice).HasColumnName("hourly_price").HasColumnType("decimal(12,2)").IsRequired();
            e.Property(x => x.Rating).HasColumnName("rating").HasColumnType("decimal(3,2)").IsRequired().HasDefaultValue(0m).ValueGeneratedNever();
            e.Property(x => x.Latitude).HasColumnName("latitude").HasColumnType("decimal(9,6)");
            e.Property(x => x.Longitude).HasColumnName("longitude").HasColumnType("decimal(9,6)");
            e.Property(x => x.LocationPrecision).HasColumnName("location_precision").HasColumnType("varchar(24)").IsRequired().HasDefaultValue("unknown").ValueGeneratedNever();
            e.Property(x => x.IsArchived).HasColumnName("is_archived").HasColumnType("bit").IsRequired().HasDefaultValue(false).ValueGeneratedNever();
            e.Property(x => x.CreatedAt).HasColumnName("created_at").HasColumnType("datetimeoffset(0)").IsRequired().HasDefaultValueSql("SYSUTCDATETIME()");
            e.HasOne<User>(x => x.Owner).WithMany(x => x.OwnedFields).HasForeignKey("OwnerId").HasPrincipalKey("Id").OnDelete(DeleteBehavior.NoAction).HasConstraintName("FK_fields_owner_id_0");
            e.HasIndex(new[] { "OwnerId" }).HasDatabaseName("IX_fields_0");
        });
        modelBuilder.Entity<FieldImage>(e =>
        {
            e.ToTable("field_images", "dbo", table =>
            {
                table.HasCheckConstraint("CK_field_images_sort_order", "[sort_order] >= 0");
            });
            e.HasKey("Id").HasName("PK_field_images");
            e.Property(x => x.Id).HasColumnName("id").HasColumnType("int").IsRequired().UseIdentityColumn();
            e.Property(x => x.FieldId).HasColumnName("field_id").HasColumnType("int").IsRequired();
            e.Property(x => x.StorageUrl).HasColumnName("storage_url").HasColumnType("nvarchar(1000)").IsRequired();
            e.Property(x => x.SortOrder).HasColumnName("sort_order").HasColumnType("int").IsRequired().HasDefaultValue(0).ValueGeneratedNever();
            e.HasOne<Field>(x => x.Field).WithMany(x => x.Images).HasForeignKey("FieldId").HasPrincipalKey("Id").OnDelete(DeleteBehavior.NoAction).HasConstraintName("FK_field_images_field_id_0");
            e.HasIndex(new[] { "FieldId", "SortOrder" }).HasDatabaseName("UQ_field_images_field_id_sort_order").IsUnique();
        });
        modelBuilder.Entity<Amenity>(e =>
        {
            e.ToTable("amenities", "dbo", table =>
            {

            });
            e.HasKey("Id").HasName("PK_amenities");
            e.Property(x => x.Id).HasColumnName("id").HasColumnType("int").IsRequired().UseIdentityColumn();
            e.Property(x => x.Name).HasColumnName("name").HasColumnType("nvarchar(80)").IsRequired();

            e.HasIndex(new[] { "Name" }).HasDatabaseName("UQ_amenities_name").IsUnique();
        });
        modelBuilder.Entity<FieldAmenity>(e =>
        {
            e.ToTable("field_amenities", "dbo", table =>
            {

            });
            e.HasKey("FieldId", "AmenityId").HasName("PK_field_amenities");
            e.Property(x => x.FieldId).HasColumnName("field_id").HasColumnType("int").IsRequired().ValueGeneratedNever();
            e.Property(x => x.AmenityId).HasColumnName("amenity_id").HasColumnType("int").IsRequired().ValueGeneratedNever();
            e.HasOne<Field>(x => x.Field).WithMany(x => x.FieldAmenities).HasForeignKey("FieldId").HasPrincipalKey("Id").OnDelete(DeleteBehavior.NoAction).HasConstraintName("FK_field_amenities_field_id_0");
            e.HasOne<Amenity>(x => x.Amenity).WithMany(x => x.FieldAmenities).HasForeignKey("AmenityId").HasPrincipalKey("Id").OnDelete(DeleteBehavior.NoAction).HasConstraintName("FK_field_amenities_amenity_id_1");
            e.HasIndex(new[] { "AmenityId" }).HasDatabaseName("IX_field_amenities_1");
        });
        modelBuilder.Entity<Team>(e =>
        {
            e.ToTable("teams", "dbo", table =>
            {
                table.HasCheckConstraint("CK_teams_level_enum", "[level] IN (N'Casual', N'Intermediate', N'Competitive')");
                table.HasCheckConstraint("CK_teams_format_enum", "[format] IN (N'5v5', N'6v6', N'7v7')");
                table.HasCheckConstraint("CK_teams_capacity", "[capacity] BETWEEN 5 AND 30");
            });
            e.HasKey("Id").HasName("PK_teams");
            e.Property(x => x.Id).HasColumnName("id").HasColumnType("int").IsRequired().UseIdentityColumn();
            e.Property(x => x.CaptainId).HasColumnName("captain_id").HasColumnType("int").IsRequired();
            e.Property(x => x.Name).HasColumnName("name").HasColumnType("nvarchar(36)").IsRequired();
            e.Property(x => x.City).HasColumnName("city").HasColumnType("nvarchar(100)").IsRequired();
            e.Property(x => x.Area).HasColumnName("area").HasColumnType("nvarchar(120)").IsRequired();
            e.Property(x => x.Level).HasColumnName("level").HasColumnType("varchar(24)").IsRequired().HasDefaultValue("Intermediate").ValueGeneratedNever();
            e.Property(x => x.Format).HasColumnName("format").HasColumnType("varchar(24)").IsRequired().HasDefaultValue("5v5").ValueGeneratedNever();
            e.Property(x => x.Capacity).HasColumnName("capacity").HasColumnType("int").IsRequired().HasDefaultValue(12).ValueGeneratedNever();
            e.Property(x => x.Bio).HasColumnName("bio").HasColumnType("nvarchar(360)").IsRequired();
            e.Property(x => x.UsualMatchday).HasColumnName("usual_matchday").HasColumnType("nvarchar(100)").IsRequired();
            e.Property(x => x.Initials).HasColumnName("initials").HasColumnType("nvarchar(3)").IsRequired();
            e.Property(x => x.BadgeUrl).HasColumnName("badge_url").HasColumnType("nvarchar(1000)");
            e.Property(x => x.Color).HasColumnName("color").HasColumnType("varchar(20)").IsRequired().HasDefaultValue("forest").ValueGeneratedNever();
            e.Property(x => x.IsRecruiting).HasColumnName("is_recruiting").HasColumnType("bit").IsRequired().HasDefaultValue(true).ValueGeneratedNever();
            e.Property(x => x.CreatedAt).HasColumnName("created_at").HasColumnType("datetimeoffset(0)").IsRequired().HasDefaultValueSql("SYSUTCDATETIME()");
            e.HasOne<User>(x => x.Captain).WithOne(x => x.CaptainedTeam).HasForeignKey<Team>("CaptainId").HasPrincipalKey<User>("Id").OnDelete(DeleteBehavior.NoAction).HasConstraintName("FK_teams_captain_id_0");
            e.HasIndex(new[] { "CaptainId" }).HasDatabaseName("UQ_teams_captain_id").IsUnique();
            e.HasIndex(new[] { "Name" }).HasDatabaseName("UQ_teams_name").IsUnique();
        });
        modelBuilder.Entity<TeamMember>(e =>
        {
            e.ToTable("team_members", "dbo", table =>
            {
                table.HasCheckConstraint("CK_team_members_position_enum", "[position] IN (N'Goalkeeper', N'Defender', N'Midfielder', N'Forward')");
            });
            e.HasKey("UserId").HasName("PK_team_members");
            e.Property(x => x.UserId).HasColumnName("user_id").HasColumnType("int").IsRequired().ValueGeneratedNever();
            e.Property(x => x.TeamId).HasColumnName("team_id").HasColumnType("int").IsRequired();
            e.Property(x => x.Position).HasColumnName("position").HasColumnType("varchar(24)").IsRequired().HasDefaultValue("Midfielder").ValueGeneratedNever();
            e.Property(x => x.JoinedAt).HasColumnName("joined_at").HasColumnType("datetimeoffset(0)").IsRequired().HasDefaultValueSql("SYSUTCDATETIME()");
            e.HasOne<User>(x => x.User).WithOne(x => x.TeamMembership).HasForeignKey<TeamMember>("UserId").HasPrincipalKey<User>("Id").OnDelete(DeleteBehavior.NoAction).HasConstraintName("FK_team_members_user_id_0");
            e.HasOne<Team>(x => x.Team).WithMany(x => x.Members).HasForeignKey("TeamId").HasPrincipalKey("Id").OnDelete(DeleteBehavior.NoAction).HasConstraintName("FK_team_members_team_id_1");
            e.HasIndex(new[] { "TeamId" }).HasDatabaseName("IX_team_members_0");
        });
        modelBuilder.Entity<TeamRecruitingPosition>(e =>
        {
            e.ToTable("team_recruiting_positions", "dbo", table =>
            {
                table.HasCheckConstraint("CK_team_recruiting_positions_position_enum", "[position] IN (N'Goalkeeper', N'Defender', N'Midfielder', N'Forward')");
            });
            e.HasKey("TeamId", "Position").HasName("PK_team_recruiting_positions");
            e.Property(x => x.TeamId).HasColumnName("team_id").HasColumnType("int").IsRequired().ValueGeneratedNever();
            e.Property(x => x.Position).HasColumnName("position").HasColumnType("varchar(24)").IsRequired().ValueGeneratedNever();
            e.HasOne<Team>(x => x.Team).WithMany(x => x.RecruitingPositions).HasForeignKey("TeamId").HasPrincipalKey("Id").OnDelete(DeleteBehavior.NoAction).HasConstraintName("FK_team_recruiting_positions_team_id_0");

        });
        modelBuilder.Entity<TeamRequest>(e =>
        {
            e.ToTable("team_requests", "dbo", table =>
            {
                table.HasCheckConstraint("CK_team_requests_request_type_enum", "[request_type] IN (N'join', N'leave')");
                table.HasCheckConstraint("CK_team_requests_status_enum", "[status] IN (N'Pending', N'Accepted', N'Declined', N'Cancelled')");
                table.HasCheckConstraint("CK_team_requests_position_enum", "[position] IN (N'Goalkeeper', N'Defender', N'Midfielder', N'Forward')");
            });
            e.HasKey("Id").HasName("PK_team_requests");
            e.Property(x => x.Id).HasColumnName("id").HasColumnType("int").IsRequired().UseIdentityColumn();
            e.Property(x => x.TeamId).HasColumnName("team_id").HasColumnType("int").IsRequired();
            e.Property(x => x.UserId).HasColumnName("user_id").HasColumnType("int").IsRequired();
            e.Property(x => x.RequestType).HasColumnName("request_type").HasColumnType("varchar(24)").IsRequired().HasDefaultValue("join").ValueGeneratedNever();
            e.Property(x => x.Status).HasColumnName("status").HasColumnType("varchar(24)").IsRequired().HasDefaultValue("Pending").ValueGeneratedNever();
            e.Property(x => x.Position).HasColumnName("position").HasColumnType("varchar(24)").IsRequired().HasDefaultValue("Midfielder").ValueGeneratedNever();
            e.Property(x => x.Message).HasColumnName("message").HasColumnType("nvarchar(300)");
            e.Property(x => x.ReviewedBy).HasColumnName("reviewed_by").HasColumnType("int");
            e.Property(x => x.ReviewedAt).HasColumnName("reviewed_at").HasColumnType("datetimeoffset(0)");
            e.Property(x => x.CreatedAt).HasColumnName("created_at").HasColumnType("datetimeoffset(0)").IsRequired().HasDefaultValueSql("SYSUTCDATETIME()");
            e.HasOne<Team>(x => x.Team).WithMany(x => x.Requests).HasForeignKey("TeamId").HasPrincipalKey("Id").OnDelete(DeleteBehavior.NoAction).HasConstraintName("FK_team_requests_team_id_0");
            e.HasOne<User>().WithMany().HasForeignKey("UserId").HasPrincipalKey("Id").OnDelete(DeleteBehavior.NoAction).HasConstraintName("FK_team_requests_user_id_1");
            e.HasOne<User>().WithMany().HasForeignKey("ReviewedBy").HasPrincipalKey("Id").OnDelete(DeleteBehavior.NoAction).HasConstraintName("FK_team_requests_reviewed_by_2");
            e.HasIndex(new[] { "TeamId", "UserId", "RequestType" }).HasDatabaseName("IX_team_requests_0").IsUnique().HasFilter("[status] = 'Pending'");
            e.HasIndex(new[] { "UserId" }).HasDatabaseName("IX_team_requests_1");
            e.HasIndex(new[] { "ReviewedBy" }).HasDatabaseName("IX_team_requests_2");
        });
        modelBuilder.Entity<Tournament>(e =>
        {
            e.ToTable("tournaments", "dbo", table =>
            {
                table.HasCheckConstraint("CK_tournaments_duration_minutes", "[duration_minutes] > 0");
                table.HasCheckConstraint("CK_tournaments_phase6", "[match_duration_minutes] IS NULL OR ([match_duration_minutes] IN (60,120) AND [creation_key] IS NOT NULL AND [registration_opens_at] IS NOT NULL AND [registration_closes_at] IS NOT NULL AND [registration_opens_at] < [registration_closes_at] AND [registration_closes_at] <= [starts_at] AND DATEPART(TZOFFSET,[registration_opens_at])=180 AND DATEPART(TZOFFSET,[registration_closes_at])=180)");
                table.HasCheckConstraint("CK_tournaments_capacity", "[capacity] BETWEEN 2 AND 64");
                table.HasCheckConstraint("CK_tournaments_entry_fee", "[entry_fee] >= 0");
                table.HasCheckConstraint("CK_tournaments_system_enum", "[system] IN (N'Single elimination', N'Group stage + knockout')");
                table.HasCheckConstraint("CK_tournaments_status_enum", "[status] IN (N'Scheduled', N'Cancelled')");
                table.HasCheckConstraint("CK_tournaments_rule_1", "[status] <> 'Cancelled' OR ([cancelled_by] IS NOT NULL AND [cancelled_at] IS NOT NULL)");
            });
            e.HasKey("Id").HasName("PK_tournaments");
            e.Property(x=>x.Description).HasColumnName("description").HasColumnType("nvarchar(2000)");
            e.Property(x=>x.RegistrationOpensAt).HasColumnName("registration_opens_at").HasColumnType("datetimeoffset(0)");
            e.Property(x=>x.RegistrationClosesAt).HasColumnName("registration_closes_at").HasColumnType("datetimeoffset(0)");
            e.Property(x=>x.MatchDurationMinutes).HasColumnName("match_duration_minutes").HasColumnType("int");
            e.Property(x=>x.CreationKey).HasColumnName("creation_key").HasColumnType("uniqueidentifier");
            e.HasIndex(x=>x.CreationKey).HasDatabaseName("UQ_tournaments_creation_key").IsUnique().HasFilter("[creation_key] IS NOT NULL");
            e.Property(x => x.Id).HasColumnName("id").HasColumnType("int").IsRequired().UseIdentityColumn();
            e.Property(x => x.CreatedBy).HasColumnName("created_by").HasColumnType("int").IsRequired();
            e.Property(x => x.FieldId).HasColumnName("field_id").HasColumnType("int").IsRequired();
            e.Property(x => x.Name).HasColumnName("name").HasColumnType("nvarchar(120)").IsRequired();
            e.Property(x => x.StartsAt).HasColumnName("starts_at").HasColumnType("datetimeoffset(0)").IsRequired();
            e.Property(x => x.DurationMinutes).HasColumnName("duration_minutes").HasColumnType("int").IsRequired();
            e.Property(x => x.EndsAt).HasColumnName("ends_at").HasColumnType("datetimeoffset(0)").IsRequired().HasComputedColumnSql("DATEADD(MINUTE, [duration_minutes], [starts_at])", stored: true);
            e.Property(x => x.Capacity).HasColumnName("capacity").HasColumnType("int").IsRequired();
            e.Property(x => x.EntryFee).HasColumnName("entry_fee").HasColumnType("decimal(12,2)").IsRequired();
            e.Property(x => x.Prize).HasColumnName("prize").HasColumnType("nvarchar(200)").IsRequired();
            e.Property(x => x.System).HasColumnName("system").HasColumnType("varchar(24)").IsRequired().HasDefaultValue("Single elimination").ValueGeneratedNever();
            e.Property(x => x.Level).HasColumnName("level").HasColumnType("nvarchar(40)").IsRequired();
            e.Property(x => x.DrawStartedAt).HasColumnName("draw_started_at").HasColumnType("datetimeoffset(0)");
            e.Property(x => x.Status).HasColumnName("status").HasColumnType("varchar(24)").IsRequired().HasDefaultValue("Scheduled").ValueGeneratedNever();
            e.Property(x => x.CancelledBy).HasColumnName("cancelled_by").HasColumnType("int");
            e.Property(x => x.CancelledAt).HasColumnName("cancelled_at").HasColumnType("datetimeoffset(0)");
            e.Property(x => x.CreatedAt).HasColumnName("created_at").HasColumnType("datetimeoffset(0)").IsRequired().HasDefaultValueSql("SYSUTCDATETIME()");
            e.HasOne<User>().WithMany().HasForeignKey("CreatedBy").HasPrincipalKey("Id").OnDelete(DeleteBehavior.NoAction).HasConstraintName("FK_tournaments_created_by_0");
            e.HasOne<Field>(x => x.Field).WithMany(x => x.Tournaments).HasForeignKey("FieldId").HasPrincipalKey("Id").OnDelete(DeleteBehavior.NoAction).HasConstraintName("FK_tournaments_field_id_1");
            e.HasOne<User>().WithMany().HasForeignKey("CancelledBy").HasPrincipalKey("Id").OnDelete(DeleteBehavior.NoAction).HasConstraintName("FK_tournaments_cancelled_by_2");
            e.HasIndex(new[] { "CreatedBy" }).HasDatabaseName("IX_tournaments_0");
            e.HasIndex(new[] { "FieldId" }).HasDatabaseName("IX_tournaments_1");
            e.HasIndex(new[] { "CancelledBy" }).HasDatabaseName("IX_tournaments_2");
        });
        modelBuilder.Entity<TournamentRegistration>(e =>
        {
            e.ToTable("tournament_registrations", "dbo", table =>
            {
                table.HasCheckConstraint("CK_tournament_registrations_status_enum", "[status] IN (N'Registered', N'Withdrawn', N'Pending')");
            });
            e.HasKey("Id").HasName("PK_tournament_registrations");
            e.Property(x => x.Id).HasColumnName("id").HasColumnType("int").IsRequired().UseIdentityColumn();
            e.Property(x => x.TournamentId).HasColumnName("tournament_id").HasColumnType("int").IsRequired();
            e.Property(x => x.TeamId).HasColumnName("team_id").HasColumnType("int").IsRequired();
            e.Property(x => x.CaptainId).HasColumnName("captain_id").HasColumnType("int").IsRequired();
            e.Property(x => x.TeamNameSnapshot).HasColumnName("team_name_snapshot").HasColumnType("nvarchar(100)").IsRequired();
            e.Property(x => x.CaptainNameSnapshot).HasColumnName("captain_name_snapshot").HasColumnType("nvarchar(100)").IsRequired();
            e.Property(x => x.EmailSnapshot).HasColumnName("email_snapshot").HasColumnType("nvarchar(254)").IsRequired();
            e.Property(x => x.Status).HasColumnName("status").HasColumnType("varchar(24)").IsRequired().HasDefaultValue("Registered").ValueGeneratedNever();
            e.Property(x => x.WithdrawnAt).HasColumnName("withdrawn_at").HasColumnType("datetimeoffset(0)");
            e.Property(x => x.PaymentDeadline).HasColumnName("payment_deadline").HasColumnType("datetimeoffset(0)");
            e.Property(x => x.WithdrawnBy).HasColumnName("withdrawn_by").HasColumnType("int");
            e.Property(x => x.CreatedAt).HasColumnName("created_at").HasColumnType("datetimeoffset(0)").IsRequired().HasDefaultValueSql("SYSUTCDATETIME()");
            e.HasAlternateKey("TournamentId", "Id").HasName("UQ_tournament_registrations_tournament_id_id");
            e.HasOne<Tournament>(x => x.Tournament).WithMany(x => x.Registrations).HasForeignKey("TournamentId").HasPrincipalKey("Id").OnDelete(DeleteBehavior.NoAction).HasConstraintName("FK_tournament_registrations_tournament_id_0");
            e.HasOne<Team>(x => x.Team).WithMany(x => x.TournamentRegistrations).HasForeignKey("TeamId").HasPrincipalKey("Id").OnDelete(DeleteBehavior.NoAction).HasConstraintName("FK_tournament_registrations_team_id_1");
            e.HasOne<User>().WithMany().HasForeignKey("CaptainId").HasPrincipalKey("Id").OnDelete(DeleteBehavior.NoAction).HasConstraintName("FK_tournament_registrations_captain_id_2");
            e.HasOne<User>().WithMany().HasForeignKey("WithdrawnBy").HasPrincipalKey("Id").OnDelete(DeleteBehavior.NoAction).HasConstraintName("FK_tournament_registrations_withdrawn_by_3");
            e.HasIndex(new[] { "TournamentId", "TeamId" }).HasDatabaseName("UQ_tournament_registrations_tournament_id_team_id").IsUnique();
            e.HasIndex(new[] { "TournamentId", "CaptainId" }).HasDatabaseName("UQ_tournament_registrations_tournament_id_captain_id").IsUnique();
            e.HasIndex(new[] { "TeamId" }).HasDatabaseName("IX_tournament_registrations_1");
            e.HasIndex(new[] { "CaptainId" }).HasDatabaseName("IX_tournament_registrations_2");
            e.HasIndex(new[] { "WithdrawnBy" }).HasDatabaseName("IX_tournament_registrations_3");
        });
        modelBuilder.Entity<RegistrationPlayer>(e =>
        {
            e.ToTable("registration_players", "dbo", table =>
            {
                table.HasCheckConstraint("CK_registration_players_position_enum", "[position] IN (N'Goalkeeper', N'Defender', N'Midfielder', N'Forward')");
            });
            e.HasKey("RegistrationId", "UserId").HasName("PK_registration_players");
            e.Property(x => x.RegistrationId).HasColumnName("registration_id").HasColumnType("int").IsRequired().ValueGeneratedNever();
            e.Property(x => x.UserId).HasColumnName("user_id").HasColumnType("int").IsRequired().ValueGeneratedNever();
            e.Property(x => x.NameSnapshot).HasColumnName("name_snapshot").HasColumnType("nvarchar(100)").IsRequired();
            e.Property(x => x.Position).HasColumnName("position").HasColumnType("varchar(24)").IsRequired().HasDefaultValue("Midfielder").ValueGeneratedNever();
            e.HasOne<TournamentRegistration>(x => x.Registration).WithMany(x => x.Players).HasForeignKey("RegistrationId").HasPrincipalKey("Id").OnDelete(DeleteBehavior.NoAction).HasConstraintName("FK_registration_players_registration_id_0");
            e.HasOne<User>().WithMany().HasForeignKey("UserId").HasPrincipalKey("Id").OnDelete(DeleteBehavior.NoAction).HasConstraintName("FK_registration_players_user_id_1");
            e.HasIndex(new[] { "UserId" }).HasDatabaseName("IX_registration_players_1");
        });
        modelBuilder.Entity<TournamentDrawEntry>(e =>
        {
            e.ToTable("tournament_draw_entries", "dbo", table =>
            {
                table.HasCheckConstraint("CK_tournament_draw_entries_seed_position", "[seed_position] BETWEEN 1 AND 64");
            });
            e.HasKey("RegistrationId").HasName("PK_tournament_draw_entries");
            e.Property(x => x.RegistrationId).HasColumnName("registration_id").HasColumnType("int").IsRequired().ValueGeneratedNever();
            e.Property(x => x.TournamentId).HasColumnName("tournament_id").HasColumnType("int").IsRequired();
            e.Property(x => x.SeedPosition).HasColumnName("seed_position").HasColumnType("int").IsRequired();
            e.HasOne<TournamentRegistration>(x => x.Registration).WithOne(x => x.DrawEntry).HasForeignKey<TournamentDrawEntry>("RegistrationId").HasPrincipalKey<TournamentRegistration>("Id").OnDelete(DeleteBehavior.NoAction).HasConstraintName("FK_tournament_draw_entries_registration_id_0");
            e.HasOne<Tournament>(x => x.Tournament).WithMany(x => x.DrawEntries).HasForeignKey("TournamentId").HasPrincipalKey("Id").OnDelete(DeleteBehavior.NoAction).HasConstraintName("FK_tournament_draw_entries_tournament_id_1");
            e.HasOne<TournamentRegistration>().WithMany().HasForeignKey("TournamentId", "RegistrationId").HasPrincipalKey("TournamentId", "Id").OnDelete(DeleteBehavior.NoAction).HasConstraintName("FK_tournament_draw_entries_tournament_id_registration_id_2");
            e.HasIndex(new[] { "TournamentId", "SeedPosition" }).HasDatabaseName("UQ_tournament_draw_entries_tournament_id_seed_position").IsUnique();
        });
        modelBuilder.Entity<TournamentMatche>(e =>
        {
            e.ToTable("tournament_matches", "dbo", table =>
            {
                table.HasCheckConstraint("CK_tournament_matches_round_number", "[round_number] BETWEEN 1 AND 6");
                table.HasCheckConstraint("CK_tournament_matches_phase6_status", "[status] IS NULL OR [status] IN ('Draft','Scheduled','Completed','Cancelled')");
                table.HasCheckConstraint("CK_tournament_matches_phase6_duration", "[duration_minutes] IS NULL OR [duration_minutes] IN (60,120)");
                table.HasCheckConstraint("CK_tournament_matches_phase6_schedule", "[status] <> 'Scheduled' OR ([starts_at] IS NOT NULL AND [duration_minutes] IS NOT NULL AND DATEPART(TZOFFSET,[starts_at])=180 AND CONVERT(time(0),[starts_at])=TIMEFROMPARTS(DATEPART(HOUR,[starts_at]),0,0,0,0) AND DATEPART(HOUR,[starts_at])>=13 AND DATEPART(HOUR,[starts_at])+[duration_minutes]/60<=23)");
                table.HasCheckConstraint("CK_tournament_matches_phase6_scores", "([home_score] IS NULL OR [home_score] BETWEEN 0 AND 99) AND ([away_score] IS NULL OR [away_score] BETWEEN 0 AND 99) AND ([home_penalties] IS NULL OR [home_penalties] BETWEEN 0 AND 99) AND ([away_penalties] IS NULL OR [away_penalties] BETWEEN 0 AND 99)");
                table.HasCheckConstraint("CK_tournament_matches_phase6_result", "[status] <> 'Completed' OR ([winner_registration_id] IS NOT NULL AND ([is_bye]=1 OR ([starts_at] IS NOT NULL AND [duration_minutes] IS NOT NULL AND [home_score] IS NOT NULL AND [away_score] IS NOT NULL AND (([home_score]<>[away_score] AND [home_penalties] IS NULL AND [away_penalties] IS NULL) OR ([home_score]=[away_score] AND [home_penalties] IS NOT NULL AND [away_penalties] IS NOT NULL AND [home_penalties]<>[away_penalties])))))");
                table.HasCheckConstraint("CK_tournament_matches_match_number", "[match_number] BETWEEN 1 AND 32");
            });
            e.HasKey("Id").HasName("PK_tournament_matches");
            e.Property(x=>x.StartsAt).HasColumnName("starts_at").HasColumnType("datetimeoffset(0)");
            e.Property(x=>x.DurationMinutes).HasColumnName("duration_minutes").HasColumnType("int");
            e.Property(x=>x.EndsAt).HasColumnName("ends_at").HasColumnType("datetimeoffset(0)").HasComputedColumnSql("DATEADD(MINUTE, [duration_minutes], [starts_at])",stored:true);
            e.Property(x=>x.Status).HasColumnName("status").HasColumnType("varchar(24)");
            e.Property(x=>x.HomeScore).HasColumnName("home_score").HasColumnType("int");
            e.Property(x=>x.AwayScore).HasColumnName("away_score").HasColumnType("int");
            e.Property(x=>x.HomePenalties).HasColumnName("home_penalties").HasColumnType("int");
            e.Property(x=>x.AwayPenalties).HasColumnName("away_penalties").HasColumnType("int");
            e.HasIndex(x=>x.StartsAt).HasDatabaseName("IX_tournament_matches_schedule").HasFilter("[starts_at] IS NOT NULL");
            e.Property(x => x.Id).HasColumnName("id").HasColumnType("int").IsRequired().UseIdentityColumn();
            e.Property(x => x.TournamentId).HasColumnName("tournament_id").HasColumnType("int").IsRequired();
            e.Property(x => x.RoundNumber).HasColumnName("round_number").HasColumnType("int").IsRequired();
            e.Property(x => x.MatchNumber).HasColumnName("match_number").HasColumnType("int").IsRequired();
            e.Property(x => x.WinnerRegistrationId).HasColumnName("winner_registration_id").HasColumnType("int");
            e.Property(x => x.IsBye).HasColumnName("is_bye").HasColumnType("bit").IsRequired().HasDefaultValue(false).ValueGeneratedNever();
            e.Property(x => x.UpdatedBy).HasColumnName("updated_by").HasColumnType("int");
            e.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasColumnType("datetimeoffset(0)");
            e.HasAlternateKey("TournamentId", "Id").HasName("UQ_tournament_matches_tournament_id_id");
            e.HasOne<Tournament>(x => x.Tournament).WithMany(x => x.Matches).HasForeignKey("TournamentId").HasPrincipalKey("Id").OnDelete(DeleteBehavior.NoAction).HasConstraintName("FK_tournament_matches_tournament_id_0");
            e.HasOne<TournamentRegistration>().WithMany().HasForeignKey("WinnerRegistrationId").HasPrincipalKey("Id").OnDelete(DeleteBehavior.NoAction).HasConstraintName("FK_tournament_matches_winner_registration_id_1");
            e.HasOne<User>().WithMany().HasForeignKey("UpdatedBy").HasPrincipalKey("Id").OnDelete(DeleteBehavior.NoAction).HasConstraintName("FK_tournament_matches_updated_by_2");
            e.HasOne<TournamentRegistration>().WithMany().HasForeignKey("TournamentId", "WinnerRegistrationId").HasPrincipalKey("TournamentId", "Id").OnDelete(DeleteBehavior.NoAction).HasConstraintName("FK_tournament_matches_tournament_id_winner_registration_id_3");
            e.HasIndex(new[] { "TournamentId", "RoundNumber", "MatchNumber" }).HasDatabaseName("UQ_tournament_matches_tournament_id_round_number_match_number").IsUnique();
            e.HasIndex(new[] { "WinnerRegistrationId" }).HasDatabaseName("IX_tournament_matches_1");
            e.HasIndex(new[] { "UpdatedBy" }).HasDatabaseName("IX_tournament_matches_2");
        });
        modelBuilder.Entity<TournamentMatchSlot>(e =>
        {
            e.ToTable("tournament_match_slots", "dbo", table =>
            {
                table.HasCheckConstraint("CK_tournament_match_slots_slot_number", "[slot_number] IN (1,2)");
                table.HasCheckConstraint("CK_tournament_match_slots_rule_1", "[source_match_id] IS NULL OR [source_match_id] <> [match_id]");
            });
            e.HasKey("MatchId", "SlotNumber").HasName("PK_tournament_match_slots");
            e.Property(x => x.MatchId).HasColumnName("match_id").HasColumnType("int").IsRequired().ValueGeneratedNever();
            e.Property(x => x.SlotNumber).HasColumnName("slot_number").HasColumnType("int").IsRequired().ValueGeneratedNever();
            e.Property(x => x.TournamentId).HasColumnName("tournament_id").HasColumnType("int").IsRequired();
            e.Property(x => x.RegistrationId).HasColumnName("registration_id").HasColumnType("int");
            e.Property(x => x.SourceMatchId).HasColumnName("source_match_id").HasColumnType("int");
            e.HasOne<TournamentMatche>().WithMany().HasForeignKey("MatchId").HasPrincipalKey("Id").OnDelete(DeleteBehavior.NoAction).HasConstraintName("FK_tournament_match_slots_match_id_0");
            e.HasOne<Tournament>().WithMany().HasForeignKey("TournamentId").HasPrincipalKey("Id").OnDelete(DeleteBehavior.NoAction).HasConstraintName("FK_tournament_match_slots_tournament_id_1");
            e.HasOne<TournamentRegistration>().WithMany().HasForeignKey("RegistrationId").HasPrincipalKey("Id").OnDelete(DeleteBehavior.NoAction).HasConstraintName("FK_tournament_match_slots_registration_id_2");
            e.HasOne<TournamentMatche>().WithMany().HasForeignKey("SourceMatchId").HasPrincipalKey("Id").OnDelete(DeleteBehavior.NoAction).HasConstraintName("FK_tournament_match_slots_source_match_id_3");
            e.HasOne<TournamentMatche>(x => x.Match).WithMany(x => x.InputSlots).HasForeignKey("TournamentId", "MatchId").HasPrincipalKey("TournamentId", "Id").OnDelete(DeleteBehavior.NoAction).HasConstraintName("FK_tournament_match_slots_tournament_id_match_id_4");
            e.HasOne<TournamentRegistration>(x => x.Registration).WithMany(x => x.MatchSlots).HasForeignKey("TournamentId", "RegistrationId").HasPrincipalKey("TournamentId", "Id").OnDelete(DeleteBehavior.NoAction).HasConstraintName("FK_tournament_match_slots_tournament_id_registration_id_5");
            e.HasOne<TournamentMatche>(x => x.SourceMatch).WithMany(x => x.OutputSlots).HasForeignKey("TournamentId", "SourceMatchId").HasPrincipalKey("TournamentId", "Id").OnDelete(DeleteBehavior.NoAction).HasConstraintName("FK_tournament_match_slots_tournament_id_source_match_id_6");
            e.HasIndex(new[] { "RegistrationId" }).HasDatabaseName("IX_tournament_match_slots_2");
            e.HasIndex(new[] { "SourceMatchId" }).HasDatabaseName("IX_tournament_match_slots_3");
        });
        modelBuilder.Entity<OpenGame>(e =>
        {
            e.ToTable("open_games", "dbo", table =>
            {
                table.HasCheckConstraint("CK_open_games_duration_minutes", "[duration_minutes] > 0");
                table.HasCheckConstraint("CK_open_games_capacity", "[capacity] >= 2");
                table.HasCheckConstraint("CK_open_games_player_fee", "[player_fee] >= 0");
                table.HasCheckConstraint("CK_open_games_status_enum", "[status] IN (N'Scheduled', N'Cancelled')");
            });
            e.HasKey("Id").HasName("PK_open_games");
            e.Property(x => x.Id).HasColumnName("id").HasColumnType("int").IsRequired().UseIdentityColumn();
            e.Property(x => x.OrganizerId).HasColumnName("organizer_id").HasColumnType("int").IsRequired();
            e.Property(x => x.FieldId).HasColumnName("field_id").HasColumnType("int").IsRequired();
            e.Property(x => x.Title).HasColumnName("title").HasColumnType("nvarchar(120)").IsRequired();
            e.Property(x => x.StartsAt).HasColumnName("starts_at").HasColumnType("datetimeoffset(0)").IsRequired();
            e.Property(x => x.DurationMinutes).HasColumnName("duration_minutes").HasColumnType("int").IsRequired();
            e.Property(x => x.EndsAt).HasColumnName("ends_at").HasColumnType("datetimeoffset(0)").IsRequired().HasComputedColumnSql("DATEADD(MINUTE, [duration_minutes], [starts_at])", stored: true);
            e.Property(x => x.Capacity).HasColumnName("capacity").HasColumnType("int").IsRequired();
            e.Property(x => x.PlayerFee).HasColumnName("player_fee").HasColumnType("decimal(12,2)").IsRequired();
            e.Property(x => x.Level).HasColumnName("level").HasColumnType("nvarchar(40)").IsRequired();
            e.Property(x => x.Status).HasColumnName("status").HasColumnType("varchar(24)").IsRequired().HasDefaultValue("Scheduled").ValueGeneratedNever();
            e.Property(x => x.CreatedAt).HasColumnName("created_at").HasColumnType("datetimeoffset(0)").IsRequired().HasDefaultValueSql("SYSUTCDATETIME()");
            e.HasOne<User>().WithMany().HasForeignKey("OrganizerId").HasPrincipalKey("Id").OnDelete(DeleteBehavior.NoAction).HasConstraintName("FK_open_games_organizer_id_0");
            e.HasOne<Field>(x => x.Field).WithMany(x => x.OpenGames).HasForeignKey("FieldId").HasPrincipalKey("Id").OnDelete(DeleteBehavior.NoAction).HasConstraintName("FK_open_games_field_id_1");
            e.HasIndex(new[] { "OrganizerId" }).HasDatabaseName("IX_open_games_0");
            e.HasIndex(new[] { "FieldId" }).HasDatabaseName("IX_open_games_1");
        });
        modelBuilder.Entity<GameParticipant>(e =>
        {
            e.ToTable("game_participants", "dbo", table =>
            {
                table.HasCheckConstraint("CK_game_participants_position_enum", "[position] IN (N'Goalkeeper', N'Defender', N'Midfielder', N'Forward')");
                table.HasCheckConstraint("CK_game_participants_status_enum", "[status] IN (N'Joined', N'Left')");
            });
            e.HasKey("Id").HasName("PK_game_participants");
            e.Property(x => x.Id).HasColumnName("id").HasColumnType("int").IsRequired().UseIdentityColumn();
            e.Property(x => x.GameId).HasColumnName("game_id").HasColumnType("int").IsRequired();
            e.Property(x => x.UserId).HasColumnName("user_id").HasColumnType("int").IsRequired();
            e.Property(x => x.Position).HasColumnName("position").HasColumnType("varchar(24)").IsRequired().HasDefaultValue("Midfielder").ValueGeneratedNever();
            e.Property(x => x.Status).HasColumnName("status").HasColumnType("varchar(24)").IsRequired().HasDefaultValue("Joined").ValueGeneratedNever();
            e.Property(x => x.CreatedAt).HasColumnName("created_at").HasColumnType("datetimeoffset(0)").IsRequired().HasDefaultValueSql("SYSUTCDATETIME()");
            e.HasOne<OpenGame>(x => x.Game).WithMany(x => x.Participants).HasForeignKey("GameId").HasPrincipalKey("Id").OnDelete(DeleteBehavior.NoAction).HasConstraintName("FK_game_participants_game_id_0");
            e.HasOne<User>(x => x.User).WithMany().HasForeignKey("UserId").HasPrincipalKey("Id").OnDelete(DeleteBehavior.NoAction).HasConstraintName("FK_game_participants_user_id_1");
            e.HasIndex(new[] { "GameId", "UserId" }).HasDatabaseName("UQ_game_participants_game_id_user_id").IsUnique();
            e.HasIndex(new[] { "UserId" }).HasDatabaseName("IX_game_participants_1");
        });
        modelBuilder.Entity<Challenge>(e =>
        {
            e.ToTable("challenges", "dbo", table =>
            {
                table.HasCheckConstraint("CK_challenges_format_enum", "[format] IN (N'5v5', N'6v6', N'7v7')");
                table.HasCheckConstraint("CK_challenges_payment_mode_enum", "[payment_mode] IN (N'Split checkout', N'Legacy full')");
                table.HasCheckConstraint("CK_challenges_cost_agreement_enum", "[cost_agreement] IN (N'Half each', N'Host team pays', N'Visiting team pays')");
                table.HasCheckConstraint("CK_challenges_quoted_price", "[quoted_price] >= 0");
                table.HasCheckConstraint("CK_challenges_duration_minutes", "[duration_minutes] IN (60,120)");
                table.HasCheckConstraint("CK_challenges_status_enum", "[status] IN (N'Open', N'Agreed', N'Awaiting payment', N'Booked', N'Expired', N'Cancelled')");
                table.HasCheckConstraint("CK_challenges_rule_1", "[opponent_team_id] IS NULL OR [opponent_team_id] <> [host_team_id]");
                table.HasCheckConstraint("CK_challenges_rule_2", "[payment_mode] <> 'Split checkout' OR ([field_id] IS NOT NULL AND [quoted_price] IS NOT NULL AND [cost_agreement] = 'Half each')");
            });
            e.HasKey("Id").HasName("PK_challenges");
            e.Property(x => x.Id).HasColumnName("id").HasColumnType("int").IsRequired().UseIdentityColumn();
            e.Property(x => x.HostTeamId).HasColumnName("host_team_id").HasColumnType("int").IsRequired();
            e.Property(x => x.OpponentTeamId).HasColumnName("opponent_team_id").HasColumnType("int");
            e.Property(x => x.PostedBy).HasColumnName("posted_by").HasColumnType("int").IsRequired();
            e.Property(x => x.FieldId).HasColumnName("field_id").HasColumnType("int");
            e.Property(x => x.City).HasColumnName("city").HasColumnType("nvarchar(100)").IsRequired();
            e.Property(x => x.Format).HasColumnName("format").HasColumnType("varchar(24)").IsRequired().HasDefaultValue("5v5").ValueGeneratedNever();
            e.Property(x => x.Level).HasColumnName("level").HasColumnType("nvarchar(40)").IsRequired();
            e.Property(x => x.StartsAt).HasColumnName("starts_at").HasColumnType("datetimeoffset(0)").IsRequired();
            e.Property(x => x.PaymentMode).HasColumnName("payment_mode").HasColumnType("varchar(24)").IsRequired().HasDefaultValue("Split checkout").ValueGeneratedNever();
            e.Property(x => x.DurationMinutes).HasColumnName("duration_minutes").HasColumnType("int").IsRequired().HasDefaultValue(60).ValueGeneratedNever();
            e.Property(x => x.CostAgreement).HasColumnName("cost_agreement").HasColumnType("varchar(24)").IsRequired().HasDefaultValue("Half each").ValueGeneratedNever();
            e.Property(x => x.QuotedPrice).HasColumnName("quoted_price").HasColumnType("decimal(12,2)");
            e.Property(x => x.Note).HasColumnName("note").HasColumnType("nvarchar(300)");
            e.Property(x => x.Status).HasColumnName("status").HasColumnType("varchar(24)").IsRequired().HasDefaultValue("Open").ValueGeneratedNever();
            e.Property(x => x.CreatedAt).HasColumnName("created_at").HasColumnType("datetimeoffset(0)").IsRequired().HasDefaultValueSql("SYSUTCDATETIME()");
            e.HasOne<Team>(x => x.HostTeam).WithMany().HasForeignKey("HostTeamId").HasPrincipalKey("Id").OnDelete(DeleteBehavior.NoAction).HasConstraintName("FK_challenges_host_team_id_0");
            e.HasOne<Team>(x => x.OpponentTeam).WithMany().HasForeignKey("OpponentTeamId").HasPrincipalKey("Id").OnDelete(DeleteBehavior.NoAction).HasConstraintName("FK_challenges_opponent_team_id_1");
            e.HasOne<User>().WithMany().HasForeignKey("PostedBy").HasPrincipalKey("Id").OnDelete(DeleteBehavior.NoAction).HasConstraintName("FK_challenges_posted_by_2");
            e.HasOne<Field>(x => x.Field).WithMany().HasForeignKey("FieldId").HasPrincipalKey("Id").OnDelete(DeleteBehavior.NoAction).HasConstraintName("FK_challenges_field_id_3");
            e.HasIndex(new[] { "HostTeamId" }).HasDatabaseName("IX_challenges_0");
            e.HasIndex(new[] { "OpponentTeamId" }).HasDatabaseName("IX_challenges_1");
            e.HasIndex(new[] { "PostedBy" }).HasDatabaseName("IX_challenges_2");
            e.HasIndex(new[] { "FieldId" }).HasDatabaseName("IX_challenges_3");
        });
        modelBuilder.Entity<ChallengeRequest>(e =>
        {
            e.ToTable("challenge_requests", "dbo", table =>
            {
                table.HasCheckConstraint("CK_challenge_requests_status_enum", "[status] IN (N'Pending', N'Accepted', N'Declined', N'Withdrawn')");
            });
            e.HasKey("Id").HasName("PK_challenge_requests");
            e.Property(x => x.Id).HasColumnName("id").HasColumnType("int").IsRequired().UseIdentityColumn();
            e.Property(x => x.ChallengeId).HasColumnName("challenge_id").HasColumnType("int").IsRequired();
            e.Property(x => x.TeamId).HasColumnName("team_id").HasColumnType("int").IsRequired();
            e.Property(x => x.Status).HasColumnName("status").HasColumnType("varchar(24)").IsRequired().HasDefaultValue("Pending").ValueGeneratedNever();
            e.Property(x => x.Note).HasColumnName("note").HasColumnType("nvarchar(300)");
            e.Property(x => x.CreatedAt).HasColumnName("created_at").HasColumnType("datetimeoffset(0)").IsRequired().HasDefaultValueSql("SYSUTCDATETIME()");
            e.HasOne<Challenge>(x => x.Challenge).WithMany(x => x.Requests).HasForeignKey("ChallengeId").HasPrincipalKey("Id").OnDelete(DeleteBehavior.NoAction).HasConstraintName("FK_challenge_requests_challenge_id_0");
            e.HasOne<Team>(x => x.Team).WithMany().HasForeignKey("TeamId").HasPrincipalKey("Id").OnDelete(DeleteBehavior.NoAction).HasConstraintName("FK_challenge_requests_team_id_1");
            e.HasIndex(new[] { "ChallengeId", "TeamId" }).HasDatabaseName("IX_challenge_requests_0").IsUnique().HasFilter("[status] = 'Pending'");
            e.HasIndex(new[] { "ChallengeId" }).HasDatabaseName("IX_challenge_requests_1").IsUnique().HasFilter("[status] = 'Accepted'");
            e.HasIndex(new[] { "TeamId" }).HasDatabaseName("IX_challenge_requests_2");
        });
        modelBuilder.Entity<Booking>(e =>
        {
            e.ToTable("bookings", "dbo", table =>
            {
                table.HasCheckConstraint("CK_bookings_starts_at", "DATEPART(MINUTE,[starts_at]) = 0 AND DATEPART(SECOND,[starts_at]) = 0");
                table.HasCheckConstraint("CK_bookings_price", "[price] >= 0");
                table.HasCheckConstraint("CK_bookings_commission_rate", "[commission_rate] BETWEEN 0 AND 100");
                table.HasCheckConstraint("CK_bookings_commission", "[commission] >= 0");
                table.HasCheckConstraint("CK_bookings_owner_net", "[owner_net] >= 0");
                table.HasCheckConstraint("CK_bookings_status_enum", "[status] IN (N'Pending', N'Confirmed', N'Completed', N'Cancelled')");
                table.HasCheckConstraint("CK_bookings_cancellation_reason_enum", "[cancellation_reason] IN (N'Cancelled', N'Expired')");
                table.HasCheckConstraint("CK_bookings_rule_1", "[commission] + [owner_net] = [price]");
                table.HasCheckConstraint("CK_bookings_rule_2", "[is_split] = 0 OR ([challenge_id] IS NOT NULL AND [payment_deadline] IS NOT NULL)");
                table.HasCheckConstraint("CK_bookings_rule_3", "[payment_deadline] IS NULL OR [payment_deadline] <= [starts_at]");
                table.HasCheckConstraint("CK_bookings_rule_4", "DATEPART(TZOFFSET,[starts_at]) = 180 AND DATEPART(HOUR,[starts_at]) BETWEEN 13 AND 22");
                table.HasCheckConstraint("CK_bookings_duration_minutes", "[duration_minutes] IN (60,120)");
                table.HasCheckConstraint("CK_bookings_closing_time", "[ends_at] <= TODATETIMEOFFSET(DATEADD(HOUR,23,CONVERT(datetime2(0),CONVERT(date,[starts_at]))),180)");
            });
            e.HasKey("Id").HasName("PK_bookings");
            e.Property(x => x.Id).HasColumnName("id").HasColumnType("int").IsRequired().UseIdentityColumn();
            e.Property(x => x.FieldId).HasColumnName("field_id").HasColumnType("int").IsRequired();
            e.Property(x => x.UserId).HasColumnName("user_id").HasColumnType("int").IsRequired();
            e.Property(x => x.OwnerId).HasColumnName("owner_id").HasColumnType("int").IsRequired();
            e.Property(x => x.ChallengeId).HasColumnName("challenge_id").HasColumnType("int");
            e.Property(x => x.StartsAt).HasColumnName("starts_at").HasColumnType("datetimeoffset(0)").IsRequired();
            e.Property(x => x.DurationMinutes).HasColumnName("duration_minutes").HasColumnType("int").IsRequired().HasDefaultValue(60).ValueGeneratedNever();
            e.Property(x => x.EndsAt).HasColumnName("ends_at").HasColumnType("datetimeoffset(0)").IsRequired().HasComputedColumnSql("DATEADD(MINUTE, [duration_minutes], [starts_at])", stored: true);
            e.Property(x => x.Price).HasColumnName("price").HasColumnType("decimal(12,2)").IsRequired();
            e.Property(x => x.CommissionRate).HasColumnName("commission_rate").HasColumnType("decimal(5,2)").IsRequired();
            e.Property(x => x.Commission).HasColumnName("commission").HasColumnType("decimal(12,2)").IsRequired();
            e.Property(x => x.OwnerNet).HasColumnName("owner_net").HasColumnType("decimal(12,2)").IsRequired();
            e.Property(x => x.IsSplit).HasColumnName("is_split").HasColumnType("bit").IsRequired().HasDefaultValue(false).ValueGeneratedNever();
            e.Property(x => x.PaymentDeadline).HasColumnName("payment_deadline").HasColumnType("datetimeoffset(0)");
            e.Property(x => x.Status).HasColumnName("status").HasColumnType("varchar(24)").IsRequired().HasDefaultValue("Confirmed").ValueGeneratedNever();
            e.Property(x => x.CancellationReason).HasColumnName("cancellation_reason").HasColumnType("varchar(24)");
            e.Property(x => x.CancelledBy).HasColumnName("cancelled_by").HasColumnType("int");
            e.Property(x => x.CancelledAt).HasColumnName("cancelled_at").HasColumnType("datetimeoffset(0)");
            e.Property(x => x.CreatedAt).HasColumnName("created_at").HasColumnType("datetimeoffset(0)").IsRequired().HasDefaultValueSql("SYSUTCDATETIME()");
            e.HasOne<Field>(x => x.Field).WithMany(x => x.Bookings).HasForeignKey("FieldId").HasPrincipalKey("Id").OnDelete(DeleteBehavior.NoAction).HasConstraintName("FK_bookings_field_id_0");
            e.HasOne<User>(x => x.Player).WithMany().HasForeignKey("UserId").HasPrincipalKey("Id").OnDelete(DeleteBehavior.NoAction).HasConstraintName("FK_bookings_user_id_1");
            e.HasOne<User>(x => x.Owner).WithMany().HasForeignKey("OwnerId").HasPrincipalKey("Id").OnDelete(DeleteBehavior.NoAction).HasConstraintName("FK_bookings_owner_id_2");
            e.HasOne<Challenge>(x => x.Challenge).WithOne(x => x.Booking).HasForeignKey<Booking>("ChallengeId").HasPrincipalKey<Challenge>("Id").OnDelete(DeleteBehavior.NoAction).HasConstraintName("FK_bookings_challenge_id_2");
            e.HasOne<User>().WithMany().HasForeignKey("CancelledBy").HasPrincipalKey("Id").OnDelete(DeleteBehavior.NoAction).HasConstraintName("FK_bookings_cancelled_by_4");
            e.HasIndex(new[] { "FieldId", "StartsAt" }).HasDatabaseName("IX_bookings_0").IsUnique().HasFilter("[status] IN ('Pending','Confirmed')");
            e.HasIndex(new[] { "ChallengeId" }).HasDatabaseName("IX_bookings_1").IsUnique().HasFilter("[challenge_id] IS NOT NULL");
            e.HasIndex(new[] { "UserId" }).HasDatabaseName("IX_bookings_2");
            e.HasIndex(new[] { "OwnerId" }).HasDatabaseName("IX_bookings_3");
            e.HasIndex(new[] { "CancelledBy" }).HasDatabaseName("IX_bookings_4");
        });
        modelBuilder.Entity<BookingTeam>(e =>
        {
            e.ToTable("booking_teams", "dbo", table =>
            {

            });
            e.HasKey("BookingId", "TeamId").HasName("PK_booking_teams");
            e.Property(x => x.BookingId).HasColumnName("booking_id").HasColumnType("int").IsRequired().ValueGeneratedNever();
            e.Property(x => x.TeamId).HasColumnName("team_id").HasColumnType("int").IsRequired().ValueGeneratedNever();
            e.Property(x => x.CaptainId).HasColumnName("captain_id").HasColumnType("int").IsRequired();
            e.Property(x => x.TeamNameSnapshot).HasColumnName("team_name_snapshot").HasColumnType("nvarchar(100)").IsRequired();
            e.HasAlternateKey("BookingId", "TeamId", "CaptainId").HasName("UQ_booking_teams_booking_id_team_id_captain_id");
            e.HasOne<Booking>(x => x.Booking).WithMany(x => x.Teams).HasForeignKey("BookingId").HasPrincipalKey("Id").OnDelete(DeleteBehavior.NoAction).HasConstraintName("FK_booking_teams_booking_id_0");
            e.HasOne<Team>(x => x.Team).WithMany().HasForeignKey("TeamId").HasPrincipalKey("Id").OnDelete(DeleteBehavior.NoAction).HasConstraintName("FK_booking_teams_team_id_1");
            e.HasOne<User>(x => x.Captain).WithMany().HasForeignKey("CaptainId").HasPrincipalKey("Id").OnDelete(DeleteBehavior.NoAction).HasConstraintName("FK_booking_teams_captain_id_2");
            e.HasIndex(new[] { "TeamId" }).HasDatabaseName("IX_booking_teams_1");
            e.HasIndex(new[] { "CaptainId" }).HasDatabaseName("IX_booking_teams_2");
        });
        modelBuilder.Entity<BookingShare>(e =>
        {
            e.ToTable("booking_shares", "dbo", table =>
            {
                table.HasCheckConstraint("CK_booking_shares_amount", "[amount] >= 0");
                table.HasCheckConstraint("CK_booking_shares_commission", "[commission] >= 0");
                table.HasCheckConstraint("CK_booking_shares_payment_state_enum", "[payment_state] IN (N'Unpaid', N'Held', N'Paid', N'Refunded')");
                table.HasCheckConstraint("CK_booking_shares_rule_1", "[commission] <= [amount]");
            });
            e.HasKey("Id").HasName("PK_booking_shares");
            e.Property(x => x.Id).HasColumnName("id").HasColumnType("int").IsRequired().UseIdentityColumn();
            e.Property(x => x.BookingId).HasColumnName("booking_id").HasColumnType("int").IsRequired();
            e.Property(x => x.TeamId).HasColumnName("team_id").HasColumnType("int").IsRequired();
            e.Property(x => x.PayerId).HasColumnName("payer_id").HasColumnType("int").IsRequired();
            e.Property(x => x.Amount).HasColumnName("amount").HasColumnType("decimal(12,2)").IsRequired();
            e.Property(x => x.Commission).HasColumnName("commission").HasColumnType("decimal(12,2)").IsRequired();
            e.Property(x => x.PaymentState).HasColumnName("payment_state").HasColumnType("varchar(24)").IsRequired().HasDefaultValue("Unpaid").ValueGeneratedNever();
            e.Property(x => x.PaidAt).HasColumnName("paid_at").HasColumnType("datetimeoffset(0)");
            e.HasOne<Booking>(x => x.Booking).WithMany(x => x.Shares).HasForeignKey("BookingId").HasPrincipalKey("Id").OnDelete(DeleteBehavior.NoAction).HasConstraintName("FK_booking_shares_booking_id_0");
            e.HasOne<Team>().WithMany().HasForeignKey("TeamId").HasPrincipalKey("Id").OnDelete(DeleteBehavior.NoAction).HasConstraintName("FK_booking_shares_team_id_1");
            e.HasOne<User>(x => x.Payer).WithMany().HasForeignKey("PayerId").HasPrincipalKey("Id").OnDelete(DeleteBehavior.NoAction).HasConstraintName("FK_booking_shares_payer_id_2");
            e.HasOne<BookingTeam>(x => x.BookingTeam).WithOne(x => x.Share).HasForeignKey<BookingShare>("BookingId", "TeamId", "PayerId").HasPrincipalKey<BookingTeam>("BookingId", "TeamId", "CaptainId").OnDelete(DeleteBehavior.NoAction).HasConstraintName("FK_booking_shares_booking_id_team_id_payer_id_3");
            e.HasIndex(new[] { "BookingId", "TeamId" }).HasDatabaseName("UQ_booking_shares_booking_id_team_id").IsUnique();
            e.HasIndex(new[] { "BookingId", "PayerId" }).HasDatabaseName("UQ_booking_shares_booking_id_payer_id").IsUnique();
            e.HasIndex(new[] { "TeamId" }).HasDatabaseName("IX_booking_shares_1");
            e.HasIndex(new[] { "PayerId" }).HasDatabaseName("IX_booking_shares_2");
        });
        modelBuilder.Entity<Payment>(e =>
        {
            e.ToTable("payments", "dbo", table =>
            {
                table.HasCheckConstraint("CK_payments_amount", "[amount] >= 0");
                table.HasCheckConstraint("CK_payments_commission_rate", "[commission_rate] BETWEEN 0 AND 100");
                table.HasCheckConstraint("CK_payments_commission", "[commission] >= 0");
                table.HasCheckConstraint("CK_payments_owner_net", "[owner_net] >= 0");
                table.HasCheckConstraint("CK_payments_status_enum", "[status] IN (N'Held (demo)', N'Paid (demo)', N'Refunded (demo)')");
                table.HasCheckConstraint("CK_payments_rule_1", "[amount] > 0");
                table.HasCheckConstraint("CK_payments_rule_2", "[commission] + [owner_net] = [amount]");
                table.HasCheckConstraint("CK_payments_rule_3", "CASE WHEN [field_id] IS NULL THEN 0 ELSE 1 END + CASE WHEN [booking_id] IS NULL THEN 0 ELSE 1 END + CASE WHEN [share_id] IS NULL THEN 0 ELSE 1 END + CASE WHEN [game_participant_id] IS NULL THEN 0 ELSE 1 END + CASE WHEN [registration_id] IS NULL THEN 0 ELSE 1 END = 1");
                table.HasCheckConstraint("CK_payments_rule_4", "[registration_id] IS NULL OR ([commission_rate] = 100 AND [commission] = [amount] AND [owner_net] = 0 AND [owner_id] IS NULL)");
                table.HasCheckConstraint("CK_payments_rule_5", "[status] <> 'Held (demo)' OR [share_id] IS NOT NULL");
            });
            e.HasKey("Id").HasName("PK_payments");
            e.Property(x => x.Id).HasColumnName("id").HasColumnType("int").IsRequired().UseIdentityColumn();
            e.Property(x => x.PayerId).HasColumnName("payer_id").HasColumnType("int").IsRequired();
            e.Property(x => x.PlatformAdminId).HasColumnName("platform_admin_id").HasColumnType("int").IsRequired();
            e.Property(x => x.OwnerId).HasColumnName("owner_id").HasColumnType("int");
            e.Property(x => x.FieldId).HasColumnName("field_id").HasColumnType("int");
            e.Property(x => x.BookingId).HasColumnName("booking_id").HasColumnType("int");
            e.Property(x => x.ShareId).HasColumnName("share_id").HasColumnType("int");
            e.Property(x => x.GameParticipantId).HasColumnName("game_participant_id").HasColumnType("int");
            e.Property(x => x.RegistrationId).HasColumnName("registration_id").HasColumnType("int");
            e.Property(x => x.Amount).HasColumnName("amount").HasColumnType("decimal(12,2)").IsRequired();
            e.Property(x => x.CommissionRate).HasColumnName("commission_rate").HasColumnType("decimal(5,2)").IsRequired();
            e.Property(x => x.Commission).HasColumnName("commission").HasColumnType("decimal(12,2)").IsRequired();
            e.Property(x => x.OwnerNet).HasColumnName("owner_net").HasColumnType("decimal(12,2)").IsRequired();
            e.Property(x => x.Status).HasColumnName("status").HasColumnType("varchar(24)").IsRequired().HasDefaultValue("Paid (demo)").ValueGeneratedNever();
            e.Property(x => x.IsDemo).HasColumnName("is_demo").HasColumnType("bit").IsRequired().HasDefaultValue(true).ValueGeneratedNever();
            e.Property(x => x.RefundedAt).HasColumnName("refunded_at").HasColumnType("datetimeoffset(0)");
            e.Property(x => x.CreatedAt).HasColumnName("created_at").HasColumnType("datetimeoffset(0)").IsRequired().HasDefaultValueSql("SYSUTCDATETIME()");
            e.HasOne<User>().WithMany().HasForeignKey("PayerId").HasPrincipalKey("Id").OnDelete(DeleteBehavior.NoAction).HasConstraintName("FK_payments_payer_id_0");
            e.HasOne<User>().WithMany().HasForeignKey("PlatformAdminId").HasPrincipalKey("Id").OnDelete(DeleteBehavior.NoAction).HasConstraintName("FK_payments_platform_admin_id_1");
            e.HasOne<User>().WithMany().HasForeignKey("OwnerId").HasPrincipalKey("Id").OnDelete(DeleteBehavior.NoAction).HasConstraintName("FK_payments_owner_id_1");
            e.HasOne<Field>().WithMany().HasForeignKey("FieldId").HasPrincipalKey("Id").OnDelete(DeleteBehavior.NoAction).HasConstraintName("FK_payments_field_id_2");
            e.HasOne<Booking>(x => x.Booking).WithMany().HasForeignKey("BookingId").HasPrincipalKey("Id").OnDelete(DeleteBehavior.NoAction).HasConstraintName("FK_payments_booking_id_3");
            e.HasOne<BookingShare>(x => x.Share).WithOne(x => x.Payment).HasForeignKey<Payment>("ShareId").HasPrincipalKey<BookingShare>("Id").OnDelete(DeleteBehavior.NoAction).HasConstraintName("FK_payments_share_id_5");
            e.HasOne<GameParticipant>(x => x.GameParticipant).WithMany().HasForeignKey("GameParticipantId").HasPrincipalKey("Id").OnDelete(DeleteBehavior.NoAction).HasConstraintName("FK_payments_game_participant_id_4");
            e.HasOne<TournamentRegistration>(x => x.Registration).WithMany().HasForeignKey("RegistrationId").HasPrincipalKey("Id").OnDelete(DeleteBehavior.NoAction).HasConstraintName("FK_payments_registration_id_5");
            e.HasIndex(new[] { "ShareId" }).HasDatabaseName("IX_payments_0").IsUnique().HasFilter("[share_id] IS NOT NULL");
            e.HasIndex(new[] { "PayerId", "CreatedAt" }).HasDatabaseName("IX_payments_1");
            e.HasIndex(new[] { "PlatformAdminId", "Status", "CreatedAt" }).HasDatabaseName("IX_payments_2");
            e.HasIndex(new[] { "OwnerId" }).HasDatabaseName("IX_payments_3");
            e.HasIndex(new[] { "FieldId" }).HasDatabaseName("IX_payments_4");
            e.HasIndex(new[] { "BookingId" }).HasDatabaseName("IX_payments_5");
            e.HasIndex(new[] { "GameParticipantId" }).HasDatabaseName("IX_payments_6");
            e.HasIndex(new[] { "RegistrationId" }).HasDatabaseName("IX_payments_7");
        });
        modelBuilder.Entity<Message>(e =>
        {
            e.ToTable("messages", "dbo", table =>
            {
                table.HasCheckConstraint("CK_messages_rule_1", "[sender_id] <> [recipient_id]");
            });
            e.HasKey("Id").HasName("PK_messages");
            e.Property(x => x.Id).HasColumnName("id").HasColumnType("int").IsRequired().UseIdentityColumn();
            e.Property(x => x.SenderId).HasColumnName("sender_id").HasColumnType("int").IsRequired();
            e.Property(x => x.RecipientId).HasColumnName("recipient_id").HasColumnType("int").IsRequired();
            e.Property(x => x.Body).HasColumnName("body").HasColumnType("nvarchar(2000)").IsRequired();
            e.Property(x => x.ReadAt).HasColumnName("read_at").HasColumnType("datetimeoffset(0)");
            e.Property(x => x.CreatedAt).HasColumnName("created_at").HasColumnType("datetimeoffset(0)").IsRequired().HasDefaultValueSql("SYSUTCDATETIME()");
            e.HasOne<User>(x => x.Sender).WithMany().HasForeignKey("SenderId").HasPrincipalKey("Id").OnDelete(DeleteBehavior.NoAction).HasConstraintName("FK_messages_sender_id_0");
            e.HasOne<User>(x => x.Recipient).WithMany().HasForeignKey("RecipientId").HasPrincipalKey("Id").OnDelete(DeleteBehavior.NoAction).HasConstraintName("FK_messages_recipient_id_1");
            e.HasIndex(new[] { "RecipientId", "CreatedAt" }).HasDatabaseName("IX_messages_0");
            e.HasIndex(new[] { "SenderId" }).HasDatabaseName("IX_messages_1");
        });
        modelBuilder.Entity<Feedback>(e =>
        {
            e.ToTable("feedback", "dbo", table =>
            {
                table.HasCheckConstraint("CK_feedback_rating", "[rating] BETWEEN 1 AND 5");
            });
            e.HasKey("Id").HasName("PK_feedback");
            e.Property(x => x.Id).HasColumnName("id").HasColumnType("int").IsRequired().UseIdentityColumn();
            e.Property(x => x.Name).HasColumnName("name").HasColumnType("nvarchar(100)").IsRequired();
            e.Property(x => x.Email).HasColumnName("email").HasColumnType("nvarchar(254)").IsRequired();
            e.Property(x => x.Rating).HasColumnName("rating").HasColumnType("int").IsRequired();
            e.Property(x => x.Message).HasColumnName("message").HasColumnType("nvarchar(1500)").IsRequired();
            e.Property(x => x.CreatedAt).HasColumnName("created_at").HasColumnType("datetimeoffset(0)").IsRequired().HasDefaultValueSql("SYSUTCDATETIME()");

        });
        modelBuilder.Entity<Setting>(e =>
        {
            e.ToTable("settings", "dbo", table =>
            {
                table.HasCheckConstraint("CK_settings_id", "[id] = 1");
                table.HasCheckConstraint("CK_settings_commission_rate", "[commission_rate] BETWEEN 0 AND 100");
                table.HasCheckConstraint("CK_settings_listing_fee", "[listing_fee] >= 0");
            });
            e.HasKey("Id").HasName("PK_settings");
            e.Property(x => x.Id).HasColumnName("id").HasColumnType("int").IsRequired().HasDefaultValue(1).ValueGeneratedNever();
            e.Property(x => x.Title).HasColumnName("title").HasColumnType("nvarchar(200)").IsRequired();
            e.Property(x => x.Description).HasColumnName("description").HasColumnType("nvarchar(1000)").IsRequired();
            e.Property(x => x.Contact).HasColumnName("contact").HasColumnType("nvarchar(254)");
            e.Property(x => x.City).HasColumnName("city").HasColumnType("nvarchar(100)").IsRequired();
            e.Property(x => x.CommissionRate).HasColumnName("commission_rate").HasColumnType("decimal(5,2)").IsRequired().HasDefaultValue(10m).ValueGeneratedNever();
            e.Property(x => x.ListingFee).HasColumnName("listing_fee").HasColumnType("decimal(12,2)").IsRequired().HasDefaultValue(5m).ValueGeneratedNever();

        });
        // Keep the legacy meetings table and all its original constraints.
        modelBuilder.Entity<LegacyMeeting>(e => {
            e.ToTable("meetings","dbo", table => {
                table.HasCheckConstraint("CK_meetings_duration_minutes","([duration_minutes]=(120) OR [duration_minutes]=(90) OR [duration_minutes]=(60) OR [duration_minutes]=(45) OR [duration_minutes]=(30) OR [duration_minutes]=(15))");
                table.HasCheckConstraint("CK_meetings_status_enum","([status]=N'Cancelled' OR [status]=N'Completed' OR [status]=N'Scheduled')");
            });
            e.HasKey(x=>x.Id).HasName("PK_meetings");
            e.Property(x=>x.Id).HasColumnName("id").HasColumnType("int").IsRequired().UseIdentityColumn();
            e.Property(x=>x.TournamentId).HasColumnName("tournament_id").HasColumnType("int").IsRequired();
            e.Property(x=>x.OrganizerId).HasColumnName("organizer_id").HasColumnType("int").IsRequired();
            e.Property(x=>x.Title).HasColumnName("title").HasColumnType("nvarchar(80)").IsRequired();
            e.Property(x=>x.Agenda).HasColumnName("agenda").HasColumnType("nvarchar(600)");
            e.Property(x=>x.StartsAt).HasColumnName("starts_at").HasColumnType("datetimeoffset(0)").IsRequired();
            e.Property(x=>x.DurationMinutes).HasColumnName("duration_minutes").HasColumnType("int").IsRequired();
            e.Property(x=>x.RoomId).HasColumnName("room_id").HasColumnType("nvarchar(100)").IsRequired();
            e.Property(x=>x.Status).HasColumnName("status").HasColumnType("varchar(24)").IsRequired().HasDefaultValueSql("('Scheduled')");
            e.Property(x=>x.CreatedAt).HasColumnName("created_at").HasColumnType("datetimeoffset(0)").IsRequired().HasDefaultValueSql("(sysutcdatetime())");
            e.HasIndex(new[]{"TournamentId"}).HasDatabaseName("IX_meetings_0");
            e.HasIndex(new[]{"OrganizerId"}).HasDatabaseName("IX_meetings_1");
            e.HasOne<User>().WithMany().HasForeignKey("OrganizerId").HasPrincipalKey("Id").OnDelete(DeleteBehavior.NoAction).HasConstraintName("FK_meetings_organizer_id_1");
            e.HasOne<Tournament>().WithMany().HasForeignKey("TournamentId").HasPrincipalKey("Id").OnDelete(DeleteBehavior.NoAction).HasConstraintName("FK_meetings_tournament_id_0");
        });
        modelBuilder.Entity<IdentityRole<int>>(e =>
        {
            e.ToTable("AspNetRoles", "dbo", table =>
            {
                table.HasCheckConstraint("CK_AspNetRoles_name", "[Name] IN (N'player',N'owner',N'admin')");
            });
            e.Property(x => x.Name).IsRequired();
            e.Property(x => x.NormalizedName).IsRequired();
            e.HasData(
                new IdentityRole<int> { Id=1, Name=KicksRoles.Player, NormalizedName="PLAYER", ConcurrencyStamp="kicks-player-v1" },
                new IdentityRole<int> { Id=2, Name=KicksRoles.Owner, NormalizedName="OWNER", ConcurrencyStamp="kicks-owner-v1" },
                new IdentityRole<int> { Id=3, Name=KicksRoles.Admin, NormalizedName="ADMIN", ConcurrencyStamp="kicks-admin-v1" });
        });
        modelBuilder.Entity<IdentityUserRole<int>>(e =>
        {
            e.ToTable("AspNetUserRoles", "dbo");
            // Existing KICKS accounts have one global role, not multiple independent assignments.
            e.HasIndex(x => x.UserId).IsUnique().HasDatabaseName("UX_AspNetUserRoles_UserId");
        });
        modelBuilder.Entity<IdentityUserClaim<int>>().ToTable("AspNetUserClaims", "dbo");
        modelBuilder.Entity<IdentityUserLogin<int>>().ToTable("AspNetUserLogins", "dbo");
        modelBuilder.Entity<IdentityUserToken<int>>().ToTable("AspNetUserTokens", "dbo");
        modelBuilder.Entity<IdentityRoleClaim<int>>().ToTable("AspNetRoleClaims", "dbo");
        foreach (var foreignKey in modelBuilder.Model.GetEntityTypes().SelectMany(e => e.GetForeignKeys()))
            foreignKey.DeleteBehavior = DeleteBehavior.NoAction;
    }
}
