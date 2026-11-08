using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KicksMvc.Data.Migrations
{
    /// <inheritdoc />
    public partial class BaselineExistingKicks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "dbo");

            migrationBuilder.CreateTable(
                name: "amenities",
                schema: "dbo",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    name = table.Column<string>(type: "nvarchar(80)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_amenities", x => x.id);
                    table.UniqueConstraint("UQ_amenities_name", x => x.name);
                });

            migrationBuilder.CreateTable(
                name: "feedback",
                schema: "dbo",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    created_at = table.Column<DateTimeOffset>(type: "datetimeoffset(0)", nullable: false, defaultValueSql: "(sysutcdatetime())"),
                    email = table.Column<string>(type: "nvarchar(254)", nullable: false),
                    message = table.Column<string>(type: "nvarchar(1500)", nullable: false),
                    name = table.Column<string>(type: "nvarchar(100)", nullable: false),
                    rating = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_feedback", x => x.id);
                    table.CheckConstraint("CK_feedback_rating", "([rating]>=(1) AND [rating]<=(5))");
                });

            migrationBuilder.CreateTable(
                name: "settings",
                schema: "dbo",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false, defaultValueSql: "((1))"),
                    city = table.Column<string>(type: "nvarchar(100)", nullable: false),
                    commission_rate = table.Column<decimal>(type: "decimal(5,2)", nullable: false, defaultValueSql: "((10))"),
                    contact = table.Column<string>(type: "nvarchar(254)", nullable: true),
                    description = table.Column<string>(type: "nvarchar(1000)", nullable: false),
                    listing_fee = table.Column<decimal>(type: "decimal(12,2)", nullable: false, defaultValueSql: "((5))"),
                    title = table.Column<string>(type: "nvarchar(200)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_settings", x => x.id);
                    table.CheckConstraint("CK_settings_commission_rate", "([commission_rate]>=(0) AND [commission_rate]<=(100))");
                    table.CheckConstraint("CK_settings_id", "([id]=(1))");
                    table.CheckConstraint("CK_settings_listing_fee", "([listing_fee]>=(0))");
                });

            migrationBuilder.CreateTable(
                name: "users",
                schema: "dbo",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    avatar_url = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    city = table.Column<string>(type: "nvarchar(100)", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "datetimeoffset(0)", nullable: false, defaultValueSql: "(sysutcdatetime())"),
                    email = table.Column<string>(type: "nvarchar(254)", nullable: false),
                    full_name = table.Column<string>(type: "nvarchar(100)", nullable: false),
                    password_algorithm = table.Column<string>(type: "varchar(40)", nullable: false, defaultValueSql: "('PBKDF2-SHA256')"),
                    password_hash = table.Column<string>(type: "varchar(512)", nullable: false),
                    password_iterations = table.Column<int>(type: "int", nullable: false, defaultValueSql: "((100000))"),
                    password_salt = table.Column<string>(type: "varchar(128)", nullable: false),
                    phone = table.Column<string>(type: "nvarchar(30)", nullable: true),
                    requested_role = table.Column<string>(type: "varchar(24)", nullable: true),
                    reviewed_at = table.Column<DateTimeOffset>(type: "datetimeoffset(0)", nullable: true),
                    reviewed_by = table.Column<int>(type: "int", nullable: true),
                    role = table.Column<string>(type: "varchar(24)", nullable: false, defaultValueSql: "('player')"),
                    status = table.Column<string>(type: "varchar(24)", nullable: false, defaultValueSql: "('Pending')")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users", x => x.id);
                    table.UniqueConstraint("UQ_users_email", x => x.email);
                    table.CheckConstraint("CK_users_password_iterations", "([password_iterations]>=(100000))");
                    table.CheckConstraint("CK_users_requested_role_enum", "([requested_role]=N'owner' OR [requested_role]=N'player')");
                    table.CheckConstraint("CK_users_role_enum", "([role]=N'admin' OR [role]=N'owner' OR [role]=N'player')");
                    table.CheckConstraint("CK_users_status_enum", "([status]=N'Inactive' OR [status]=N'Rejected' OR [status]=N'Active' OR [status]=N'Pending')");
                    table.ForeignKey(
                        name: "FK_users_reviewed_by_0",
                        column: x => x.reviewed_by,
                        principalSchema: "dbo",
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "fields",
                schema: "dbo",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    area = table.Column<string>(type: "nvarchar(120)", nullable: false),
                    city = table.Column<string>(type: "nvarchar(100)", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "datetimeoffset(0)", nullable: false, defaultValueSql: "(sysutcdatetime())"),
                    description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    format = table.Column<string>(type: "varchar(24)", nullable: false, defaultValueSql: "('5v5')"),
                    hourly_price = table.Column<decimal>(type: "decimal(12,2)", nullable: false),
                    is_archived = table.Column<bool>(type: "bit", nullable: false, defaultValueSql: "((0))"),
                    latitude = table.Column<decimal>(type: "decimal(9,6)", nullable: true),
                    location_precision = table.Column<string>(type: "varchar(24)", nullable: false, defaultValueSql: "('unknown')"),
                    longitude = table.Column<decimal>(type: "decimal(9,6)", nullable: true),
                    name = table.Column<string>(type: "nvarchar(120)", nullable: false),
                    owner_id = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_fields", x => x.id);
                    table.CheckConstraint("CK_fields_format_enum", "([format]=N'7v7' OR [format]=N'6v6' OR [format]=N'5v5')");
                    table.CheckConstraint("CK_fields_hourly_price", "([hourly_price]>=(0))");
                    table.CheckConstraint("CK_fields_latitude", "([latitude]>=(-90) AND [latitude]<=(90))");
                    table.CheckConstraint("CK_fields_location_precision_enum", "([location_precision]=N'unknown' OR [location_precision]=N'city' OR [location_precision]=N'area' OR [location_precision]=N'exact')");
                    table.CheckConstraint("CK_fields_longitude", "([longitude]>=(-180) AND [longitude]<=(180))");
                    table.ForeignKey(
                        name: "FK_fields_owner_id_0",
                        column: x => x.owner_id,
                        principalSchema: "dbo",
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "messages",
                schema: "dbo",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    body = table.Column<string>(type: "nvarchar(2000)", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "datetimeoffset(0)", nullable: false, defaultValueSql: "(sysutcdatetime())"),
                    read_at = table.Column<DateTimeOffset>(type: "datetimeoffset(0)", nullable: true),
                    recipient_id = table.Column<int>(type: "int", nullable: false),
                    sender_id = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_messages", x => x.id);
                    table.CheckConstraint("CK_messages_rule_1", "([sender_id]<>[recipient_id])");
                    table.ForeignKey(
                        name: "FK_messages_recipient_id_1",
                        column: x => x.recipient_id,
                        principalSchema: "dbo",
                        principalTable: "users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_messages_sender_id_0",
                        column: x => x.sender_id,
                        principalSchema: "dbo",
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "ownership_documents",
                schema: "dbo",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    byte_size = table.Column<int>(type: "int", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "datetimeoffset(0)", nullable: false, defaultValueSql: "(sysutcdatetime())"),
                    file_name = table.Column<string>(type: "nvarchar(200)", nullable: false),
                    mime_type = table.Column<string>(type: "varchar(80)", nullable: false),
                    storage_url = table.Column<string>(type: "nvarchar(1000)", nullable: false),
                    user_id = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ownership_documents", x => x.id);
                    table.CheckConstraint("CK_ownership_documents_byte_size", "([byte_size]>=(1) AND [byte_size]<=(1048576))");
                    table.ForeignKey(
                        name: "FK_ownership_documents_user_id_0",
                        column: x => x.user_id,
                        principalSchema: "dbo",
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "teams",
                schema: "dbo",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    area = table.Column<string>(type: "nvarchar(120)", nullable: false),
                    badge_url = table.Column<string>(type: "nvarchar(1000)", nullable: true),
                    bio = table.Column<string>(type: "nvarchar(360)", nullable: false),
                    capacity = table.Column<int>(type: "int", nullable: false, defaultValueSql: "((12))"),
                    captain_id = table.Column<int>(type: "int", nullable: false),
                    city = table.Column<string>(type: "nvarchar(100)", nullable: false),
                    color = table.Column<string>(type: "varchar(20)", nullable: false, defaultValueSql: "('forest')"),
                    created_at = table.Column<DateTimeOffset>(type: "datetimeoffset(0)", nullable: false, defaultValueSql: "(sysutcdatetime())"),
                    format = table.Column<string>(type: "varchar(24)", nullable: false, defaultValueSql: "('5v5')"),
                    initials = table.Column<string>(type: "nvarchar(3)", nullable: false),
                    is_recruiting = table.Column<bool>(type: "bit", nullable: false, defaultValueSql: "((1))"),
                    level = table.Column<string>(type: "varchar(24)", nullable: false, defaultValueSql: "('Intermediate')"),
                    name = table.Column<string>(type: "nvarchar(36)", nullable: false),
                    usual_matchday = table.Column<string>(type: "nvarchar(100)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_teams", x => x.id);
                    table.UniqueConstraint("UQ_teams_captain_id", x => x.captain_id);
                    table.UniqueConstraint("UQ_teams_name", x => x.name);
                    table.CheckConstraint("CK_teams_capacity", "([capacity]>=(5) AND [capacity]<=(30))");
                    table.CheckConstraint("CK_teams_format_enum", "([format]=N'7v7' OR [format]=N'6v6' OR [format]=N'5v5')");
                    table.CheckConstraint("CK_teams_level_enum", "([level]=N'Competitive' OR [level]=N'Intermediate' OR [level]=N'Casual')");
                    table.ForeignKey(
                        name: "FK_teams_captain_id_0",
                        column: x => x.captain_id,
                        principalSchema: "dbo",
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "field_amenities",
                schema: "dbo",
                columns: table => new
                {
                    field_id = table.Column<int>(type: "int", nullable: false),
                    amenity_id = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_field_amenities", x => new { x.field_id, x.amenity_id });
                    table.ForeignKey(
                        name: "FK_field_amenities_amenity_id_1",
                        column: x => x.amenity_id,
                        principalSchema: "dbo",
                        principalTable: "amenities",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_field_amenities_field_id_0",
                        column: x => x.field_id,
                        principalSchema: "dbo",
                        principalTable: "fields",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "field_images",
                schema: "dbo",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    field_id = table.Column<int>(type: "int", nullable: false),
                    sort_order = table.Column<int>(type: "int", nullable: false, defaultValueSql: "((0))"),
                    storage_url = table.Column<string>(type: "nvarchar(1000)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_field_images", x => x.id);
                    table.UniqueConstraint("UQ_field_images_field_id_sort_order", x => new { x.field_id, x.sort_order });
                    table.CheckConstraint("CK_field_images_sort_order", "([sort_order]>=(0))");
                    table.ForeignKey(
                        name: "FK_field_images_field_id_0",
                        column: x => x.field_id,
                        principalSchema: "dbo",
                        principalTable: "fields",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "open_games",
                schema: "dbo",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    capacity = table.Column<int>(type: "int", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "datetimeoffset(0)", nullable: false, defaultValueSql: "(sysutcdatetime())"),
                    duration_minutes = table.Column<int>(type: "int", nullable: false),
                    ends_at = table.Column<DateTimeOffset>(type: "datetimeoffset(0)", nullable: true, computedColumnSql: "(dateadd(minute,[duration_minutes],[starts_at]))", stored: true),
                    field_id = table.Column<int>(type: "int", nullable: false),
                    level = table.Column<string>(type: "nvarchar(40)", nullable: false),
                    organizer_id = table.Column<int>(type: "int", nullable: false),
                    player_fee = table.Column<decimal>(type: "decimal(12,2)", nullable: false),
                    starts_at = table.Column<DateTimeOffset>(type: "datetimeoffset(0)", nullable: false),
                    title = table.Column<string>(type: "nvarchar(120)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_open_games", x => x.id);
                    table.CheckConstraint("CK_open_games_capacity", "([capacity]>=(2))");
                    table.CheckConstraint("CK_open_games_duration_minutes", "([duration_minutes]>(0))");
                    table.CheckConstraint("CK_open_games_player_fee", "([player_fee]>=(0))");
                    table.ForeignKey(
                        name: "FK_open_games_field_id_1",
                        column: x => x.field_id,
                        principalSchema: "dbo",
                        principalTable: "fields",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_open_games_organizer_id_0",
                        column: x => x.organizer_id,
                        principalSchema: "dbo",
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "tournaments",
                schema: "dbo",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    capacity = table.Column<int>(type: "int", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "datetimeoffset(0)", nullable: false, defaultValueSql: "(sysutcdatetime())"),
                    created_by = table.Column<int>(type: "int", nullable: false),
                    draw_started_at = table.Column<DateTimeOffset>(type: "datetimeoffset(0)", nullable: true),
                    duration_minutes = table.Column<int>(type: "int", nullable: false),
                    ends_at = table.Column<DateTimeOffset>(type: "datetimeoffset(0)", nullable: true, computedColumnSql: "(dateadd(minute,[duration_minutes],[starts_at]))", stored: true),
                    entry_fee = table.Column<decimal>(type: "decimal(12,2)", nullable: false),
                    field_id = table.Column<int>(type: "int", nullable: false),
                    level = table.Column<string>(type: "nvarchar(40)", nullable: false),
                    name = table.Column<string>(type: "nvarchar(120)", nullable: false),
                    prize = table.Column<string>(type: "nvarchar(200)", nullable: false),
                    starts_at = table.Column<DateTimeOffset>(type: "datetimeoffset(0)", nullable: false),
                    system = table.Column<string>(type: "varchar(24)", nullable: false, defaultValueSql: "('Single elimination')")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tournaments", x => x.id);
                    table.CheckConstraint("CK_tournaments_capacity", "([capacity]>=(2) AND [capacity]<=(64))");
                    table.CheckConstraint("CK_tournaments_duration_minutes", "([duration_minutes]>(0))");
                    table.CheckConstraint("CK_tournaments_entry_fee", "([entry_fee]>=(0))");
                    table.CheckConstraint("CK_tournaments_system_enum", "([system]=N'Group stage + knockout' OR [system]=N'Single elimination')");
                    table.ForeignKey(
                        name: "FK_tournaments_created_by_0",
                        column: x => x.created_by,
                        principalSchema: "dbo",
                        principalTable: "users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_tournaments_field_id_1",
                        column: x => x.field_id,
                        principalSchema: "dbo",
                        principalTable: "fields",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "challenges",
                schema: "dbo",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    city = table.Column<string>(type: "nvarchar(100)", nullable: false),
                    cost_agreement = table.Column<string>(type: "varchar(24)", nullable: false, defaultValueSql: "('Half each')"),
                    created_at = table.Column<DateTimeOffset>(type: "datetimeoffset(0)", nullable: false, defaultValueSql: "(sysutcdatetime())"),
                    format = table.Column<string>(type: "varchar(24)", nullable: false, defaultValueSql: "('5v5')"),
                    host_team_id = table.Column<int>(type: "int", nullable: false),
                    level = table.Column<string>(type: "nvarchar(40)", nullable: false),
                    note = table.Column<string>(type: "nvarchar(300)", nullable: true),
                    opponent_team_id = table.Column<int>(type: "int", nullable: true),
                    posted_by = table.Column<int>(type: "int", nullable: false),
                    starts_at = table.Column<DateTimeOffset>(type: "datetimeoffset(0)", nullable: false),
                    status = table.Column<string>(type: "varchar(24)", nullable: false, defaultValueSql: "('Open')")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_challenges", x => x.id);
                    table.CheckConstraint("CK_challenges_cost_agreement_enum", "([cost_agreement]=N'Visiting team pays' OR [cost_agreement]=N'Host team pays' OR [cost_agreement]=N'Half each')");
                    table.CheckConstraint("CK_challenges_format_enum", "([format]=N'7v7' OR [format]=N'6v6' OR [format]=N'5v5')");
                    table.CheckConstraint("CK_challenges_rule_1", "([opponent_team_id] IS NULL OR [opponent_team_id]<>[host_team_id])");
                    table.CheckConstraint("CK_challenges_status_enum", "([status]=N'Cancelled' OR [status]=N'Booked' OR [status]=N'Agreed' OR [status]=N'Open')");
                    table.ForeignKey(
                        name: "FK_challenges_host_team_id_0",
                        column: x => x.host_team_id,
                        principalSchema: "dbo",
                        principalTable: "teams",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_challenges_opponent_team_id_1",
                        column: x => x.opponent_team_id,
                        principalSchema: "dbo",
                        principalTable: "teams",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_challenges_posted_by_2",
                        column: x => x.posted_by,
                        principalSchema: "dbo",
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "team_members",
                schema: "dbo",
                columns: table => new
                {
                    user_id = table.Column<int>(type: "int", nullable: false),
                    joined_at = table.Column<DateTimeOffset>(type: "datetimeoffset(0)", nullable: false, defaultValueSql: "(sysutcdatetime())"),
                    position = table.Column<string>(type: "varchar(24)", nullable: false, defaultValueSql: "('Midfielder')"),
                    team_id = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_team_members", x => x.user_id);
                    table.CheckConstraint("CK_team_members_position_enum", "([position]=N'Forward' OR [position]=N'Midfielder' OR [position]=N'Defender' OR [position]=N'Goalkeeper')");
                    table.ForeignKey(
                        name: "FK_team_members_team_id_1",
                        column: x => x.team_id,
                        principalSchema: "dbo",
                        principalTable: "teams",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_team_members_user_id_0",
                        column: x => x.user_id,
                        principalSchema: "dbo",
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "team_requests",
                schema: "dbo",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    created_at = table.Column<DateTimeOffset>(type: "datetimeoffset(0)", nullable: false, defaultValueSql: "(sysutcdatetime())"),
                    message = table.Column<string>(type: "nvarchar(300)", nullable: true),
                    position = table.Column<string>(type: "varchar(24)", nullable: false, defaultValueSql: "('Midfielder')"),
                    request_type = table.Column<string>(type: "varchar(24)", nullable: false, defaultValueSql: "('join')"),
                    reviewed_at = table.Column<DateTimeOffset>(type: "datetimeoffset(0)", nullable: true),
                    reviewed_by = table.Column<int>(type: "int", nullable: true),
                    status = table.Column<string>(type: "varchar(24)", nullable: false, defaultValueSql: "('Pending')"),
                    team_id = table.Column<int>(type: "int", nullable: false),
                    user_id = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_team_requests", x => x.id);
                    table.CheckConstraint("CK_team_requests_position_enum", "([position]=N'Forward' OR [position]=N'Midfielder' OR [position]=N'Defender' OR [position]=N'Goalkeeper')");
                    table.CheckConstraint("CK_team_requests_request_type_enum", "([request_type]=N'leave' OR [request_type]=N'join')");
                    table.CheckConstraint("CK_team_requests_status_enum", "([status]=N'Cancelled' OR [status]=N'Declined' OR [status]=N'Accepted' OR [status]=N'Pending')");
                    table.ForeignKey(
                        name: "FK_team_requests_reviewed_by_2",
                        column: x => x.reviewed_by,
                        principalSchema: "dbo",
                        principalTable: "users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_team_requests_team_id_0",
                        column: x => x.team_id,
                        principalSchema: "dbo",
                        principalTable: "teams",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_team_requests_user_id_1",
                        column: x => x.user_id,
                        principalSchema: "dbo",
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "game_participants",
                schema: "dbo",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    created_at = table.Column<DateTimeOffset>(type: "datetimeoffset(0)", nullable: false, defaultValueSql: "(sysutcdatetime())"),
                    game_id = table.Column<int>(type: "int", nullable: false),
                    position = table.Column<string>(type: "varchar(24)", nullable: false, defaultValueSql: "('Midfielder')"),
                    status = table.Column<string>(type: "varchar(24)", nullable: false, defaultValueSql: "('Joined')"),
                    user_id = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_game_participants", x => x.id);
                    table.UniqueConstraint("UQ_game_participants_game_id_user_id", x => new { x.game_id, x.user_id });
                    table.CheckConstraint("CK_game_participants_position_enum", "([position]=N'Forward' OR [position]=N'Midfielder' OR [position]=N'Defender' OR [position]=N'Goalkeeper')");
                    table.CheckConstraint("CK_game_participants_status_enum", "([status]=N'Left' OR [status]=N'Joined')");
                    table.ForeignKey(
                        name: "FK_game_participants_game_id_0",
                        column: x => x.game_id,
                        principalSchema: "dbo",
                        principalTable: "open_games",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_game_participants_user_id_1",
                        column: x => x.user_id,
                        principalSchema: "dbo",
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "meetings",
                schema: "dbo",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    agenda = table.Column<string>(type: "nvarchar(600)", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "datetimeoffset(0)", nullable: false, defaultValueSql: "(sysutcdatetime())"),
                    duration_minutes = table.Column<int>(type: "int", nullable: false),
                    organizer_id = table.Column<int>(type: "int", nullable: false),
                    room_id = table.Column<string>(type: "nvarchar(100)", nullable: false),
                    starts_at = table.Column<DateTimeOffset>(type: "datetimeoffset(0)", nullable: false),
                    status = table.Column<string>(type: "varchar(24)", nullable: false, defaultValueSql: "('Scheduled')"),
                    title = table.Column<string>(type: "nvarchar(80)", nullable: false),
                    tournament_id = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_meetings", x => x.id);
                    table.UniqueConstraint("UQ_meetings_room_id", x => x.room_id);
                    table.CheckConstraint("CK_meetings_duration_minutes", "([duration_minutes]=(120) OR [duration_minutes]=(90) OR [duration_minutes]=(60) OR [duration_minutes]=(45) OR [duration_minutes]=(30) OR [duration_minutes]=(15))");
                    table.CheckConstraint("CK_meetings_status_enum", "([status]=N'Cancelled' OR [status]=N'Completed' OR [status]=N'Scheduled')");
                    table.ForeignKey(
                        name: "FK_meetings_organizer_id_1",
                        column: x => x.organizer_id,
                        principalSchema: "dbo",
                        principalTable: "users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_meetings_tournament_id_0",
                        column: x => x.tournament_id,
                        principalSchema: "dbo",
                        principalTable: "tournaments",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "tournament_registrations",
                schema: "dbo",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    captain_id = table.Column<int>(type: "int", nullable: false),
                    captain_name_snapshot = table.Column<string>(type: "nvarchar(100)", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "datetimeoffset(0)", nullable: false, defaultValueSql: "(sysutcdatetime())"),
                    email_snapshot = table.Column<string>(type: "nvarchar(254)", nullable: false),
                    status = table.Column<string>(type: "varchar(24)", nullable: false, defaultValueSql: "('Registered')"),
                    team_id = table.Column<int>(type: "int", nullable: false),
                    team_name_snapshot = table.Column<string>(type: "nvarchar(100)", nullable: false),
                    tournament_id = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tournament_registrations", x => x.id);
                    table.UniqueConstraint("UQ_tournament_registrations_tournament_id_captain_id", x => new { x.tournament_id, x.captain_id });
                    table.UniqueConstraint("UQ_tournament_registrations_tournament_id_id", x => new { x.tournament_id, x.id });
                    table.UniqueConstraint("UQ_tournament_registrations_tournament_id_team_id", x => new { x.tournament_id, x.team_id });
                    table.CheckConstraint("CK_tournament_registrations_status_enum", "([status]=N'Withdrawn' OR [status]=N'Registered')");
                    table.ForeignKey(
                        name: "FK_tournament_registrations_captain_id_2",
                        column: x => x.captain_id,
                        principalSchema: "dbo",
                        principalTable: "users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_tournament_registrations_team_id_1",
                        column: x => x.team_id,
                        principalSchema: "dbo",
                        principalTable: "teams",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_tournament_registrations_tournament_id_0",
                        column: x => x.tournament_id,
                        principalSchema: "dbo",
                        principalTable: "tournaments",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "bookings",
                schema: "dbo",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    challenge_id = table.Column<int>(type: "int", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "datetimeoffset(0)", nullable: false, defaultValueSql: "(sysutcdatetime())"),
                    ends_at = table.Column<DateTimeOffset>(type: "datetimeoffset(0)", nullable: true, computedColumnSql: "(dateadd(hour,(1),[starts_at]))", stored: true),
                    field_id = table.Column<int>(type: "int", nullable: false),
                    price = table.Column<decimal>(type: "decimal(12,2)", nullable: false),
                    starts_at = table.Column<DateTimeOffset>(type: "datetimeoffset(0)", nullable: false),
                    status = table.Column<string>(type: "varchar(24)", nullable: false, defaultValueSql: "('Confirmed')"),
                    user_id = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_bookings", x => x.id);
                    table.CheckConstraint("CK_bookings_price", "([price]>=(0))");
                    table.CheckConstraint("CK_bookings_starts_at", "(datepart(minute,[starts_at])=(0) AND datepart(second,[starts_at])=(0))");
                    table.CheckConstraint("CK_bookings_status_enum", "([status]=N'Cancelled' OR [status]=N'Confirmed' OR [status]=N'Pending')");
                    table.ForeignKey(
                        name: "FK_bookings_challenge_id_2",
                        column: x => x.challenge_id,
                        principalSchema: "dbo",
                        principalTable: "challenges",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_bookings_field_id_0",
                        column: x => x.field_id,
                        principalSchema: "dbo",
                        principalTable: "fields",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_bookings_user_id_1",
                        column: x => x.user_id,
                        principalSchema: "dbo",
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "challenge_requests",
                schema: "dbo",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    challenge_id = table.Column<int>(type: "int", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "datetimeoffset(0)", nullable: false, defaultValueSql: "(sysutcdatetime())"),
                    note = table.Column<string>(type: "nvarchar(300)", nullable: true),
                    status = table.Column<string>(type: "varchar(24)", nullable: false, defaultValueSql: "('Pending')"),
                    team_id = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_challenge_requests", x => x.id);
                    table.CheckConstraint("CK_challenge_requests_status_enum", "([status]=N'Withdrawn' OR [status]=N'Declined' OR [status]=N'Accepted' OR [status]=N'Pending')");
                    table.ForeignKey(
                        name: "FK_challenge_requests_challenge_id_0",
                        column: x => x.challenge_id,
                        principalSchema: "dbo",
                        principalTable: "challenges",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_challenge_requests_team_id_1",
                        column: x => x.team_id,
                        principalSchema: "dbo",
                        principalTable: "teams",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "registration_players",
                schema: "dbo",
                columns: table => new
                {
                    registration_id = table.Column<int>(type: "int", nullable: false),
                    user_id = table.Column<int>(type: "int", nullable: false),
                    name_snapshot = table.Column<string>(type: "nvarchar(100)", nullable: false),
                    position = table.Column<string>(type: "varchar(24)", nullable: false, defaultValueSql: "('Midfielder')")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_registration_players", x => new { x.registration_id, x.user_id });
                    table.CheckConstraint("CK_registration_players_position_enum", "([position]=N'Forward' OR [position]=N'Midfielder' OR [position]=N'Defender' OR [position]=N'Goalkeeper')");
                    table.ForeignKey(
                        name: "FK_registration_players_registration_id_0",
                        column: x => x.registration_id,
                        principalSchema: "dbo",
                        principalTable: "tournament_registrations",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_registration_players_user_id_1",
                        column: x => x.user_id,
                        principalSchema: "dbo",
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "tournament_draw_entries",
                schema: "dbo",
                columns: table => new
                {
                    registration_id = table.Column<int>(type: "int", nullable: false),
                    seed_position = table.Column<int>(type: "int", nullable: false),
                    tournament_id = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tournament_draw_entries", x => x.registration_id);
                    table.UniqueConstraint("UQ_tournament_draw_entries_tournament_id_seed_position", x => new { x.tournament_id, x.seed_position });
                    table.CheckConstraint("CK_tournament_draw_entries_seed_position", "([seed_position]>=(1) AND [seed_position]<=(64))");
                    table.ForeignKey(
                        name: "FK_tournament_draw_entries_registration_id_0",
                        column: x => x.registration_id,
                        principalSchema: "dbo",
                        principalTable: "tournament_registrations",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_tournament_draw_entries_tournament_id_1",
                        column: x => x.tournament_id,
                        principalSchema: "dbo",
                        principalTable: "tournaments",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_tournament_draw_entries_tournament_id_registration_id_2",
                        columns: x => new { x.tournament_id, x.registration_id },
                        principalSchema: "dbo",
                        principalTable: "tournament_registrations",
                        principalColumns: new[] { "tournament_id", "id" });
                });

            migrationBuilder.CreateTable(
                name: "tournament_matches",
                schema: "dbo",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    is_bye = table.Column<bool>(type: "bit", nullable: false, defaultValueSql: "((0))"),
                    match_number = table.Column<int>(type: "int", nullable: false),
                    round_number = table.Column<int>(type: "int", nullable: false),
                    tournament_id = table.Column<int>(type: "int", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "datetimeoffset(0)", nullable: true),
                    updated_by = table.Column<int>(type: "int", nullable: true),
                    winner_registration_id = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tournament_matches", x => x.id);
                    table.UniqueConstraint("UQ_tournament_matches_tournament_id_id", x => new { x.tournament_id, x.id });
                    table.UniqueConstraint("UQ_tournament_matches_tournament_id_round_number_match_number", x => new { x.tournament_id, x.round_number, x.match_number });
                    table.CheckConstraint("CK_tournament_matches_match_number", "([match_number]>=(1) AND [match_number]<=(32))");
                    table.CheckConstraint("CK_tournament_matches_round_number", "([round_number]>=(1) AND [round_number]<=(6))");
                    table.ForeignKey(
                        name: "FK_tournament_matches_tournament_id_0",
                        column: x => x.tournament_id,
                        principalSchema: "dbo",
                        principalTable: "tournaments",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_tournament_matches_tournament_id_winner_registration_id_3",
                        columns: x => new { x.tournament_id, x.winner_registration_id },
                        principalSchema: "dbo",
                        principalTable: "tournament_registrations",
                        principalColumns: new[] { "tournament_id", "id" });
                    table.ForeignKey(
                        name: "FK_tournament_matches_updated_by_2",
                        column: x => x.updated_by,
                        principalSchema: "dbo",
                        principalTable: "users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_tournament_matches_winner_registration_id_1",
                        column: x => x.winner_registration_id,
                        principalSchema: "dbo",
                        principalTable: "tournament_registrations",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "booking_teams",
                schema: "dbo",
                columns: table => new
                {
                    booking_id = table.Column<int>(type: "int", nullable: false),
                    team_id = table.Column<int>(type: "int", nullable: false),
                    captain_id = table.Column<int>(type: "int", nullable: false),
                    team_name_snapshot = table.Column<string>(type: "nvarchar(100)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_booking_teams", x => new { x.booking_id, x.team_id });
                    table.ForeignKey(
                        name: "FK_booking_teams_booking_id_0",
                        column: x => x.booking_id,
                        principalSchema: "dbo",
                        principalTable: "bookings",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_booking_teams_captain_id_2",
                        column: x => x.captain_id,
                        principalSchema: "dbo",
                        principalTable: "users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_booking_teams_team_id_1",
                        column: x => x.team_id,
                        principalSchema: "dbo",
                        principalTable: "teams",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "payments",
                schema: "dbo",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    amount = table.Column<decimal>(type: "decimal(12,2)", nullable: false),
                    booking_id = table.Column<int>(type: "int", nullable: true),
                    commission = table.Column<decimal>(type: "decimal(12,2)", nullable: false),
                    commission_rate = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "datetimeoffset(0)", nullable: false, defaultValueSql: "(sysutcdatetime())"),
                    field_id = table.Column<int>(type: "int", nullable: true),
                    game_participant_id = table.Column<int>(type: "int", nullable: true),
                    is_demo = table.Column<bool>(type: "bit", nullable: false, defaultValueSql: "((1))"),
                    owner_id = table.Column<int>(type: "int", nullable: false),
                    owner_net = table.Column<decimal>(type: "decimal(12,2)", nullable: false),
                    payer_id = table.Column<int>(type: "int", nullable: false),
                    registration_id = table.Column<int>(type: "int", nullable: true),
                    status = table.Column<string>(type: "varchar(24)", nullable: false, defaultValueSql: "('Paid (demo)')")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_payments", x => x.id);
                    table.CheckConstraint("CK_payments_amount", "([amount]>=(0))");
                    table.CheckConstraint("CK_payments_commission", "([commission]>=(0))");
                    table.CheckConstraint("CK_payments_commission_rate", "([commission_rate]>=(0) AND [commission_rate]<=(100))");
                    table.CheckConstraint("CK_payments_owner_net", "([owner_net]>=(0))");
                    table.CheckConstraint("CK_payments_rule_1", "(([commission]+[owner_net])=[amount])");
                    table.CheckConstraint("CK_payments_rule_2", "((((case when [field_id] IS NULL then (0) else (1) end+case when [booking_id] IS NULL then (0) else (1) end)+case when [game_participant_id] IS NULL then (0) else (1) end)+case when [registration_id] IS NULL then (0) else (1) end)=(1))");
                    table.CheckConstraint("CK_payments_status_enum", "([status]=N'Free' OR [status]=N'Refunded (demo)' OR [status]=N'Paid (demo)')");
                    table.ForeignKey(
                        name: "FK_payments_booking_id_3",
                        column: x => x.booking_id,
                        principalSchema: "dbo",
                        principalTable: "bookings",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_payments_field_id_2",
                        column: x => x.field_id,
                        principalSchema: "dbo",
                        principalTable: "fields",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_payments_game_participant_id_4",
                        column: x => x.game_participant_id,
                        principalSchema: "dbo",
                        principalTable: "game_participants",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_payments_owner_id_1",
                        column: x => x.owner_id,
                        principalSchema: "dbo",
                        principalTable: "users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_payments_payer_id_0",
                        column: x => x.payer_id,
                        principalSchema: "dbo",
                        principalTable: "users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_payments_registration_id_5",
                        column: x => x.registration_id,
                        principalSchema: "dbo",
                        principalTable: "tournament_registrations",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "tournament_match_slots",
                schema: "dbo",
                columns: table => new
                {
                    match_id = table.Column<int>(type: "int", nullable: false),
                    slot_number = table.Column<int>(type: "int", nullable: false),
                    registration_id = table.Column<int>(type: "int", nullable: true),
                    source_match_id = table.Column<int>(type: "int", nullable: true),
                    tournament_id = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tournament_match_slots", x => new { x.match_id, x.slot_number });
                    table.CheckConstraint("CK_tournament_match_slots_rule_1", "([source_match_id] IS NULL OR [source_match_id]<>[match_id])");
                    table.CheckConstraint("CK_tournament_match_slots_slot_number", "([slot_number]=(2) OR [slot_number]=(1))");
                    table.ForeignKey(
                        name: "FK_tournament_match_slots_match_id_0",
                        column: x => x.match_id,
                        principalSchema: "dbo",
                        principalTable: "tournament_matches",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_tournament_match_slots_registration_id_2",
                        column: x => x.registration_id,
                        principalSchema: "dbo",
                        principalTable: "tournament_registrations",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_tournament_match_slots_source_match_id_3",
                        column: x => x.source_match_id,
                        principalSchema: "dbo",
                        principalTable: "tournament_matches",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_tournament_match_slots_tournament_id_1",
                        column: x => x.tournament_id,
                        principalSchema: "dbo",
                        principalTable: "tournaments",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_tournament_match_slots_tournament_id_match_id_4",
                        columns: x => new { x.tournament_id, x.match_id },
                        principalSchema: "dbo",
                        principalTable: "tournament_matches",
                        principalColumns: new[] { "tournament_id", "id" });
                    table.ForeignKey(
                        name: "FK_tournament_match_slots_tournament_id_registration_id_5",
                        columns: x => new { x.tournament_id, x.registration_id },
                        principalSchema: "dbo",
                        principalTable: "tournament_registrations",
                        principalColumns: new[] { "tournament_id", "id" });
                    table.ForeignKey(
                        name: "FK_tournament_match_slots_tournament_id_source_match_id_6",
                        columns: x => new { x.tournament_id, x.source_match_id },
                        principalSchema: "dbo",
                        principalTable: "tournament_matches",
                        principalColumns: new[] { "tournament_id", "id" });
                });

            migrationBuilder.CreateIndex(
                name: "IX_booking_teams_0",
                schema: "dbo",
                table: "booking_teams",
                column: "booking_id");

            migrationBuilder.CreateIndex(
                name: "IX_booking_teams_1",
                schema: "dbo",
                table: "booking_teams",
                column: "team_id");

            migrationBuilder.CreateIndex(
                name: "IX_booking_teams_2",
                schema: "dbo",
                table: "booking_teams",
                column: "captain_id");

            migrationBuilder.CreateIndex(
                name: "IX_bookings_0",
                schema: "dbo",
                table: "bookings",
                columns: new[] { "field_id", "starts_at" },
                unique: true,
                filter: "([status] IN ('Pending', 'Confirmed'))");

            migrationBuilder.CreateIndex(
                name: "IX_bookings_1",
                schema: "dbo",
                table: "bookings",
                column: "challenge_id",
                unique: true,
                filter: "([challenge_id] IS NOT NULL)");

            migrationBuilder.CreateIndex(
                name: "IX_bookings_2",
                schema: "dbo",
                table: "bookings",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_challenge_requests_0",
                schema: "dbo",
                table: "challenge_requests",
                columns: new[] { "challenge_id", "team_id" },
                unique: true,
                filter: "([status]='Pending')");

            migrationBuilder.CreateIndex(
                name: "IX_challenge_requests_1",
                schema: "dbo",
                table: "challenge_requests",
                column: "team_id");

            migrationBuilder.CreateIndex(
                name: "IX_challenges_0",
                schema: "dbo",
                table: "challenges",
                column: "host_team_id");

            migrationBuilder.CreateIndex(
                name: "IX_challenges_1",
                schema: "dbo",
                table: "challenges",
                column: "opponent_team_id");

            migrationBuilder.CreateIndex(
                name: "IX_challenges_2",
                schema: "dbo",
                table: "challenges",
                column: "posted_by");

            migrationBuilder.CreateIndex(
                name: "IX_field_amenities_0",
                schema: "dbo",
                table: "field_amenities",
                column: "field_id");

            migrationBuilder.CreateIndex(
                name: "IX_field_amenities_1",
                schema: "dbo",
                table: "field_amenities",
                column: "amenity_id");

            migrationBuilder.CreateIndex(
                name: "IX_field_images_0",
                schema: "dbo",
                table: "field_images",
                column: "field_id");

            migrationBuilder.CreateIndex(
                name: "IX_fields_0",
                schema: "dbo",
                table: "fields",
                column: "owner_id");

            migrationBuilder.CreateIndex(
                name: "IX_game_participants_0",
                schema: "dbo",
                table: "game_participants",
                column: "game_id");

            migrationBuilder.CreateIndex(
                name: "IX_game_participants_1",
                schema: "dbo",
                table: "game_participants",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_meetings_0",
                schema: "dbo",
                table: "meetings",
                column: "tournament_id");

            migrationBuilder.CreateIndex(
                name: "IX_meetings_1",
                schema: "dbo",
                table: "meetings",
                column: "organizer_id");

            migrationBuilder.CreateIndex(
                name: "IX_messages_0",
                schema: "dbo",
                table: "messages",
                columns: new[] { "recipient_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "IX_messages_1",
                schema: "dbo",
                table: "messages",
                column: "sender_id");

            migrationBuilder.CreateIndex(
                name: "IX_open_games_0",
                schema: "dbo",
                table: "open_games",
                column: "organizer_id");

            migrationBuilder.CreateIndex(
                name: "IX_open_games_1",
                schema: "dbo",
                table: "open_games",
                column: "field_id");

            migrationBuilder.CreateIndex(
                name: "IX_ownership_documents_0",
                schema: "dbo",
                table: "ownership_documents",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_payments_0",
                schema: "dbo",
                table: "payments",
                column: "payer_id");

            migrationBuilder.CreateIndex(
                name: "IX_payments_1",
                schema: "dbo",
                table: "payments",
                column: "owner_id");

            migrationBuilder.CreateIndex(
                name: "IX_payments_2",
                schema: "dbo",
                table: "payments",
                column: "field_id");

            migrationBuilder.CreateIndex(
                name: "IX_payments_3",
                schema: "dbo",
                table: "payments",
                column: "booking_id");

            migrationBuilder.CreateIndex(
                name: "IX_payments_4",
                schema: "dbo",
                table: "payments",
                column: "game_participant_id");

            migrationBuilder.CreateIndex(
                name: "IX_payments_5",
                schema: "dbo",
                table: "payments",
                column: "registration_id");

            migrationBuilder.CreateIndex(
                name: "IX_registration_players_0",
                schema: "dbo",
                table: "registration_players",
                column: "registration_id");

            migrationBuilder.CreateIndex(
                name: "IX_registration_players_1",
                schema: "dbo",
                table: "registration_players",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_team_members_0",
                schema: "dbo",
                table: "team_members",
                column: "team_id");

            migrationBuilder.CreateIndex(
                name: "IX_team_requests_0",
                schema: "dbo",
                table: "team_requests",
                columns: new[] { "team_id", "user_id", "request_type" },
                unique: true,
                filter: "([status]='Pending')");

            migrationBuilder.CreateIndex(
                name: "IX_team_requests_1",
                schema: "dbo",
                table: "team_requests",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_team_requests_2",
                schema: "dbo",
                table: "team_requests",
                column: "reviewed_by");

            migrationBuilder.CreateIndex(
                name: "IX_tournament_draw_entries_0",
                schema: "dbo",
                table: "tournament_draw_entries",
                column: "tournament_id");

            migrationBuilder.CreateIndex(
                name: "IX_tournament_match_slots_0",
                schema: "dbo",
                table: "tournament_match_slots",
                column: "match_id");

            migrationBuilder.CreateIndex(
                name: "IX_tournament_match_slots_1",
                schema: "dbo",
                table: "tournament_match_slots",
                column: "tournament_id");

            migrationBuilder.CreateIndex(
                name: "IX_tournament_match_slots_2",
                schema: "dbo",
                table: "tournament_match_slots",
                column: "registration_id");

            migrationBuilder.CreateIndex(
                name: "IX_tournament_match_slots_3",
                schema: "dbo",
                table: "tournament_match_slots",
                column: "source_match_id");

            migrationBuilder.CreateIndex(
                name: "IX_tournament_matches_0",
                schema: "dbo",
                table: "tournament_matches",
                column: "tournament_id");

            migrationBuilder.CreateIndex(
                name: "IX_tournament_matches_1",
                schema: "dbo",
                table: "tournament_matches",
                column: "winner_registration_id");

            migrationBuilder.CreateIndex(
                name: "IX_tournament_matches_2",
                schema: "dbo",
                table: "tournament_matches",
                column: "updated_by");

            migrationBuilder.CreateIndex(
                name: "IX_tournament_registrations_0",
                schema: "dbo",
                table: "tournament_registrations",
                column: "tournament_id");

            migrationBuilder.CreateIndex(
                name: "IX_tournament_registrations_1",
                schema: "dbo",
                table: "tournament_registrations",
                column: "team_id");

            migrationBuilder.CreateIndex(
                name: "IX_tournament_registrations_2",
                schema: "dbo",
                table: "tournament_registrations",
                column: "captain_id");

            migrationBuilder.CreateIndex(
                name: "IX_tournaments_0",
                schema: "dbo",
                table: "tournaments",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "IX_tournaments_1",
                schema: "dbo",
                table: "tournaments",
                column: "field_id");

            migrationBuilder.CreateIndex(
                name: "IX_users_0",
                schema: "dbo",
                table: "users",
                column: "reviewed_by");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "booking_teams",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "challenge_requests",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "feedback",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "field_amenities",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "field_images",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "meetings",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "messages",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "ownership_documents",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "payments",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "registration_players",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "settings",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "team_members",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "team_requests",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "tournament_draw_entries",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "tournament_match_slots",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "amenities",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "bookings",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "game_participants",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "tournament_matches",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "challenges",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "open_games",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "tournament_registrations",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "teams",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "tournaments",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "fields",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "users",
                schema: "dbo");
        }
    }
}
