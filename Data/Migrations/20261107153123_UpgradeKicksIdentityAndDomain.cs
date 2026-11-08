using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace KicksMvc.Data.Migrations
{
    /// <inheritdoc />
    public partial class UpgradeKicksIdentityAndDomain : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Abort before DDL if inspected-empty tables have acquired any records.
            migrationBuilder.Sql("""
                IF DB_NAME() <> N'Kicks' THROW 51000, 'This reviewed upgrade is restricted to Kicks.', 1;
                IF EXISTS (SELECT 1 FROM [dbo].[amenities] WITH (TABLOCKX,HOLDLOCK)) THROW 51001, 'Pre-upgrade table is populated: amenities. Stop for a data-preserving migration review.', 1;
                IF EXISTS (SELECT 1 FROM [dbo].[booking_teams] WITH (TABLOCKX,HOLDLOCK)) THROW 51001, 'Pre-upgrade table is populated: booking_teams. Stop for a data-preserving migration review.', 1;
                IF EXISTS (SELECT 1 FROM [dbo].[bookings] WITH (TABLOCKX,HOLDLOCK)) THROW 51001, 'Pre-upgrade table is populated: bookings. Stop for a data-preserving migration review.', 1;
                IF EXISTS (SELECT 1 FROM [dbo].[challenge_requests] WITH (TABLOCKX,HOLDLOCK)) THROW 51001, 'Pre-upgrade table is populated: challenge_requests. Stop for a data-preserving migration review.', 1;
                IF EXISTS (SELECT 1 FROM [dbo].[challenges] WITH (TABLOCKX,HOLDLOCK)) THROW 51001, 'Pre-upgrade table is populated: challenges. Stop for a data-preserving migration review.', 1;
                IF EXISTS (SELECT 1 FROM [dbo].[feedback] WITH (TABLOCKX,HOLDLOCK)) THROW 51001, 'Pre-upgrade table is populated: feedback. Stop for a data-preserving migration review.', 1;
                IF EXISTS (SELECT 1 FROM [dbo].[field_amenities] WITH (TABLOCKX,HOLDLOCK)) THROW 51001, 'Pre-upgrade table is populated: field_amenities. Stop for a data-preserving migration review.', 1;
                IF EXISTS (SELECT 1 FROM [dbo].[field_images] WITH (TABLOCKX,HOLDLOCK)) THROW 51001, 'Pre-upgrade table is populated: field_images. Stop for a data-preserving migration review.', 1;
                IF EXISTS (SELECT 1 FROM [dbo].[fields] WITH (TABLOCKX,HOLDLOCK)) THROW 51001, 'Pre-upgrade table is populated: fields. Stop for a data-preserving migration review.', 1;
                IF EXISTS (SELECT 1 FROM [dbo].[game_participants] WITH (TABLOCKX,HOLDLOCK)) THROW 51001, 'Pre-upgrade table is populated: game_participants. Stop for a data-preserving migration review.', 1;
                IF EXISTS (SELECT 1 FROM [dbo].[meetings] WITH (TABLOCKX,HOLDLOCK)) THROW 51001, 'Pre-upgrade table is populated: meetings. Stop for a data-preserving migration review.', 1;
                IF EXISTS (SELECT 1 FROM [dbo].[messages] WITH (TABLOCKX,HOLDLOCK)) THROW 51001, 'Pre-upgrade table is populated: messages. Stop for a data-preserving migration review.', 1;
                IF EXISTS (SELECT 1 FROM [dbo].[open_games] WITH (TABLOCKX,HOLDLOCK)) THROW 51001, 'Pre-upgrade table is populated: open_games. Stop for a data-preserving migration review.', 1;
                IF EXISTS (SELECT 1 FROM [dbo].[ownership_documents] WITH (TABLOCKX,HOLDLOCK)) THROW 51001, 'Pre-upgrade table is populated: ownership_documents. Stop for a data-preserving migration review.', 1;
                IF EXISTS (SELECT 1 FROM [dbo].[payments] WITH (TABLOCKX,HOLDLOCK)) THROW 51001, 'Pre-upgrade table is populated: payments. Stop for a data-preserving migration review.', 1;
                IF EXISTS (SELECT 1 FROM [dbo].[registration_players] WITH (TABLOCKX,HOLDLOCK)) THROW 51001, 'Pre-upgrade table is populated: registration_players. Stop for a data-preserving migration review.', 1;
                IF EXISTS (SELECT 1 FROM [dbo].[settings] WITH (TABLOCKX,HOLDLOCK)) THROW 51001, 'Pre-upgrade table is populated: settings. Stop for a data-preserving migration review.', 1;
                IF EXISTS (SELECT 1 FROM [dbo].[team_members] WITH (TABLOCKX,HOLDLOCK)) THROW 51001, 'Pre-upgrade table is populated: team_members. Stop for a data-preserving migration review.', 1;
                IF EXISTS (SELECT 1 FROM [dbo].[team_requests] WITH (TABLOCKX,HOLDLOCK)) THROW 51001, 'Pre-upgrade table is populated: team_requests. Stop for a data-preserving migration review.', 1;
                IF EXISTS (SELECT 1 FROM [dbo].[teams] WITH (TABLOCKX,HOLDLOCK)) THROW 51001, 'Pre-upgrade table is populated: teams. Stop for a data-preserving migration review.', 1;
                IF EXISTS (SELECT 1 FROM [dbo].[tournament_draw_entries] WITH (TABLOCKX,HOLDLOCK)) THROW 51001, 'Pre-upgrade table is populated: tournament_draw_entries. Stop for a data-preserving migration review.', 1;
                IF EXISTS (SELECT 1 FROM [dbo].[tournament_match_slots] WITH (TABLOCKX,HOLDLOCK)) THROW 51001, 'Pre-upgrade table is populated: tournament_match_slots. Stop for a data-preserving migration review.', 1;
                IF EXISTS (SELECT 1 FROM [dbo].[tournament_matches] WITH (TABLOCKX,HOLDLOCK)) THROW 51001, 'Pre-upgrade table is populated: tournament_matches. Stop for a data-preserving migration review.', 1;
                IF EXISTS (SELECT 1 FROM [dbo].[tournament_registrations] WITH (TABLOCKX,HOLDLOCK)) THROW 51001, 'Pre-upgrade table is populated: tournament_registrations. Stop for a data-preserving migration review.', 1;
                IF EXISTS (SELECT 1 FROM [dbo].[tournaments] WITH (TABLOCKX,HOLDLOCK)) THROW 51001, 'Pre-upgrade table is populated: tournaments. Stop for a data-preserving migration review.', 1;
                IF EXISTS (SELECT 1 FROM [dbo].[users] WITH (TABLOCKX,HOLDLOCK)) THROW 51001, 'Pre-upgrade table is populated: users. Stop for a data-preserving migration review.', 1;
                """);
            migrationBuilder.DropUniqueConstraint(
                name: "UQ_users_email",
                schema: "dbo",
                table: "users");

            migrationBuilder.DropCheckConstraint(
                name: "CK_users_password_iterations",
                schema: "dbo",
                table: "users");

            migrationBuilder.DropCheckConstraint(
                name: "CK_users_requested_role_enum",
                schema: "dbo",
                table: "users");

            migrationBuilder.DropCheckConstraint(
                name: "CK_users_role_enum",
                schema: "dbo",
                table: "users");

            migrationBuilder.DropCheckConstraint(
                name: "CK_users_status_enum",
                schema: "dbo",
                table: "users");

            migrationBuilder.DropCheckConstraint(
                name: "CK_tournaments_capacity",
                schema: "dbo",
                table: "tournaments");

            migrationBuilder.DropCheckConstraint(
                name: "CK_tournaments_duration_minutes",
                schema: "dbo",
                table: "tournaments");

            migrationBuilder.DropCheckConstraint(
                name: "CK_tournaments_entry_fee",
                schema: "dbo",
                table: "tournaments");

            migrationBuilder.DropCheckConstraint(
                name: "CK_tournaments_system_enum",
                schema: "dbo",
                table: "tournaments");

            migrationBuilder.DropUniqueConstraint(
                name: "UQ_tournament_registrations_tournament_id_captain_id",
                schema: "dbo",
                table: "tournament_registrations");

            migrationBuilder.DropUniqueConstraint(
                name: "UQ_tournament_registrations_tournament_id_team_id",
                schema: "dbo",
                table: "tournament_registrations");

            migrationBuilder.DropIndex(
                name: "IX_tournament_registrations_0",
                schema: "dbo",
                table: "tournament_registrations");

            migrationBuilder.DropCheckConstraint(
                name: "CK_tournament_registrations_status_enum",
                schema: "dbo",
                table: "tournament_registrations");

            migrationBuilder.DropUniqueConstraint(
                name: "UQ_tournament_matches_tournament_id_round_number_match_number",
                schema: "dbo",
                table: "tournament_matches");

            migrationBuilder.DropIndex(
                name: "IX_tournament_matches_0",
                schema: "dbo",
                table: "tournament_matches");

            migrationBuilder.DropCheckConstraint(
                name: "CK_tournament_matches_match_number",
                schema: "dbo",
                table: "tournament_matches");

            migrationBuilder.DropCheckConstraint(
                name: "CK_tournament_matches_round_number",
                schema: "dbo",
                table: "tournament_matches");

            migrationBuilder.DropIndex(
                name: "IX_tournament_match_slots_0",
                schema: "dbo",
                table: "tournament_match_slots");

            migrationBuilder.DropIndex(
                name: "IX_tournament_match_slots_1",
                schema: "dbo",
                table: "tournament_match_slots");

            migrationBuilder.DropCheckConstraint(
                name: "CK_tournament_match_slots_rule_1",
                schema: "dbo",
                table: "tournament_match_slots");

            migrationBuilder.DropCheckConstraint(
                name: "CK_tournament_match_slots_slot_number",
                schema: "dbo",
                table: "tournament_match_slots");

            migrationBuilder.DropUniqueConstraint(
                name: "UQ_tournament_draw_entries_tournament_id_seed_position",
                schema: "dbo",
                table: "tournament_draw_entries");

            migrationBuilder.DropIndex(
                name: "IX_tournament_draw_entries_0",
                schema: "dbo",
                table: "tournament_draw_entries");

            migrationBuilder.DropCheckConstraint(
                name: "CK_tournament_draw_entries_seed_position",
                schema: "dbo",
                table: "tournament_draw_entries");

            migrationBuilder.DropUniqueConstraint(
                name: "UQ_teams_captain_id",
                schema: "dbo",
                table: "teams");

            migrationBuilder.DropUniqueConstraint(
                name: "UQ_teams_name",
                schema: "dbo",
                table: "teams");

            migrationBuilder.DropCheckConstraint(
                name: "CK_teams_capacity",
                schema: "dbo",
                table: "teams");

            migrationBuilder.DropCheckConstraint(
                name: "CK_teams_format_enum",
                schema: "dbo",
                table: "teams");

            migrationBuilder.DropCheckConstraint(
                name: "CK_teams_level_enum",
                schema: "dbo",
                table: "teams");

            migrationBuilder.DropIndex(
                name: "IX_team_requests_0",
                schema: "dbo",
                table: "team_requests");

            migrationBuilder.DropCheckConstraint(
                name: "CK_team_requests_position_enum",
                schema: "dbo",
                table: "team_requests");

            migrationBuilder.DropCheckConstraint(
                name: "CK_team_requests_request_type_enum",
                schema: "dbo",
                table: "team_requests");

            migrationBuilder.DropCheckConstraint(
                name: "CK_team_requests_status_enum",
                schema: "dbo",
                table: "team_requests");

            migrationBuilder.DropCheckConstraint(
                name: "CK_team_members_position_enum",
                schema: "dbo",
                table: "team_members");

            migrationBuilder.DropCheckConstraint(
                name: "CK_settings_commission_rate",
                schema: "dbo",
                table: "settings");

            migrationBuilder.DropCheckConstraint(
                name: "CK_settings_id",
                schema: "dbo",
                table: "settings");

            migrationBuilder.DropCheckConstraint(
                name: "CK_settings_listing_fee",
                schema: "dbo",
                table: "settings");

            migrationBuilder.DropIndex(
                name: "IX_registration_players_0",
                schema: "dbo",
                table: "registration_players");

            migrationBuilder.DropCheckConstraint(
                name: "CK_registration_players_position_enum",
                schema: "dbo",
                table: "registration_players");

            migrationBuilder.DropIndex(
                name: "IX_payments_0",
                schema: "dbo",
                table: "payments");

            migrationBuilder.DropCheckConstraint(
                name: "CK_payments_amount",
                schema: "dbo",
                table: "payments");

            migrationBuilder.DropCheckConstraint(
                name: "CK_payments_commission",
                schema: "dbo",
                table: "payments");

            migrationBuilder.DropCheckConstraint(
                name: "CK_payments_commission_rate",
                schema: "dbo",
                table: "payments");

            migrationBuilder.DropCheckConstraint(
                name: "CK_payments_owner_net",
                schema: "dbo",
                table: "payments");

            migrationBuilder.DropCheckConstraint(
                name: "CK_payments_rule_1",
                schema: "dbo",
                table: "payments");

            migrationBuilder.DropCheckConstraint(
                name: "CK_payments_rule_2",
                schema: "dbo",
                table: "payments");

            migrationBuilder.DropCheckConstraint(
                name: "CK_payments_status_enum",
                schema: "dbo",
                table: "payments");

            migrationBuilder.DropCheckConstraint(
                name: "CK_ownership_documents_byte_size",
                schema: "dbo",
                table: "ownership_documents");

            migrationBuilder.DropCheckConstraint(
                name: "CK_open_games_capacity",
                schema: "dbo",
                table: "open_games");

            migrationBuilder.DropCheckConstraint(
                name: "CK_open_games_duration_minutes",
                schema: "dbo",
                table: "open_games");

            migrationBuilder.DropCheckConstraint(
                name: "CK_open_games_player_fee",
                schema: "dbo",
                table: "open_games");

            migrationBuilder.DropCheckConstraint(
                name: "CK_messages_rule_1",
                schema: "dbo",
                table: "messages");

            migrationBuilder.DropUniqueConstraint(
                name: "UQ_meetings_room_id",
                schema: "dbo",
                table: "meetings");

            migrationBuilder.DropUniqueConstraint(
                name: "UQ_game_participants_game_id_user_id",
                schema: "dbo",
                table: "game_participants");

            migrationBuilder.DropIndex(
                name: "IX_game_participants_0",
                schema: "dbo",
                table: "game_participants");

            migrationBuilder.DropCheckConstraint(
                name: "CK_game_participants_position_enum",
                schema: "dbo",
                table: "game_participants");

            migrationBuilder.DropCheckConstraint(
                name: "CK_game_participants_status_enum",
                schema: "dbo",
                table: "game_participants");

            migrationBuilder.DropCheckConstraint(
                name: "CK_fields_format_enum",
                schema: "dbo",
                table: "fields");

            migrationBuilder.DropCheckConstraint(
                name: "CK_fields_hourly_price",
                schema: "dbo",
                table: "fields");

            migrationBuilder.DropCheckConstraint(
                name: "CK_fields_latitude",
                schema: "dbo",
                table: "fields");

            migrationBuilder.DropCheckConstraint(
                name: "CK_fields_location_precision_enum",
                schema: "dbo",
                table: "fields");

            migrationBuilder.DropCheckConstraint(
                name: "CK_fields_longitude",
                schema: "dbo",
                table: "fields");

            migrationBuilder.DropUniqueConstraint(
                name: "UQ_field_images_field_id_sort_order",
                schema: "dbo",
                table: "field_images");

            migrationBuilder.DropIndex(
                name: "IX_field_images_0",
                schema: "dbo",
                table: "field_images");

            migrationBuilder.DropCheckConstraint(
                name: "CK_field_images_sort_order",
                schema: "dbo",
                table: "field_images");

            migrationBuilder.DropIndex(
                name: "IX_field_amenities_0",
                schema: "dbo",
                table: "field_amenities");

            migrationBuilder.DropCheckConstraint(
                name: "CK_feedback_rating",
                schema: "dbo",
                table: "feedback");

            migrationBuilder.DropCheckConstraint(
                name: "CK_challenges_cost_agreement_enum",
                schema: "dbo",
                table: "challenges");

            migrationBuilder.DropCheckConstraint(
                name: "CK_challenges_format_enum",
                schema: "dbo",
                table: "challenges");

            migrationBuilder.DropCheckConstraint(
                name: "CK_challenges_rule_1",
                schema: "dbo",
                table: "challenges");

            migrationBuilder.DropCheckConstraint(
                name: "CK_challenges_status_enum",
                schema: "dbo",
                table: "challenges");

            migrationBuilder.DropIndex(
                name: "IX_challenge_requests_0",
                schema: "dbo",
                table: "challenge_requests");

            migrationBuilder.DropCheckConstraint(
                name: "CK_challenge_requests_status_enum",
                schema: "dbo",
                table: "challenge_requests");

            migrationBuilder.DropIndex(
                name: "IX_bookings_0",
                schema: "dbo",
                table: "bookings");

            migrationBuilder.DropIndex(
                name: "IX_bookings_1",
                schema: "dbo",
                table: "bookings");

            migrationBuilder.DropCheckConstraint(
                name: "CK_bookings_price",
                schema: "dbo",
                table: "bookings");

            migrationBuilder.DropCheckConstraint(
                name: "CK_bookings_starts_at",
                schema: "dbo",
                table: "bookings");

            migrationBuilder.DropCheckConstraint(
                name: "CK_bookings_status_enum",
                schema: "dbo",
                table: "bookings");

            migrationBuilder.DropIndex(
                name: "IX_booking_teams_0",
                schema: "dbo",
                table: "booking_teams");

            migrationBuilder.DropUniqueConstraint(
                name: "UQ_amenities_name",
                schema: "dbo",
                table: "amenities");

            migrationBuilder.DropColumn(
                name: "password_algorithm",
                schema: "dbo",
                table: "users");

            migrationBuilder.DropColumn(
                name: "password_iterations",
                schema: "dbo",
                table: "users");

            migrationBuilder.DropColumn(
                name: "password_salt",
                schema: "dbo",
                table: "users");

            migrationBuilder.DropColumn(
                name: "role",
                schema: "dbo",
                table: "users");

            migrationBuilder.RenameIndex(
                name: "IX_payments_5",
                schema: "dbo",
                table: "payments",
                newName: "IX_payments_7");

            migrationBuilder.RenameIndex(
                name: "IX_payments_4",
                schema: "dbo",
                table: "payments",
                newName: "IX_payments_6");

            migrationBuilder.RenameIndex(
                name: "IX_payments_3",
                schema: "dbo",
                table: "payments",
                newName: "IX_payments_5");

            migrationBuilder.RenameIndex(
                name: "IX_payments_2",
                schema: "dbo",
                table: "payments",
                newName: "IX_payments_4");

            migrationBuilder.RenameIndex(
                name: "IX_payments_1",
                schema: "dbo",
                table: "payments",
                newName: "IX_payments_3");

            migrationBuilder.RenameIndex(
                name: "IX_challenge_requests_1",
                schema: "dbo",
                table: "challenge_requests",
                newName: "IX_challenge_requests_2");

            migrationBuilder.AlterColumn<string>(
                name: "status",
                schema: "dbo",
                table: "users",
                type: "varchar(24)",
                nullable: false,
                defaultValue: "Pending",
                oldClrType: typeof(string),
                oldType: "varchar(24)",
                oldDefaultValueSql: "('Pending')");

            migrationBuilder.AlterColumn<string>(
                name: "password_hash",
                schema: "dbo",
                table: "users",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(512)");

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "created_at",
                schema: "dbo",
                table: "users",
                type: "datetimeoffset(0)",
                nullable: false,
                defaultValueSql: "SYSUTCDATETIME()",
                oldClrType: typeof(DateTimeOffset),
                oldType: "datetimeoffset(0)",
                oldDefaultValueSql: "(sysutcdatetime())");

            migrationBuilder.AddColumn<int>(
                name: "access_failed_count",
                schema: "dbo",
                table: "users",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "address",
                schema: "dbo",
                table: "users",
                type: "nvarchar(300)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "concurrency_stamp",
                schema: "dbo",
                table: "users",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "email_confirmed",
                schema: "dbo",
                table: "users",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "lockout_enabled",
                schema: "dbo",
                table: "users",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "lockout_end",
                schema: "dbo",
                table: "users",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "normalized_email",
                schema: "dbo",
                table: "users",
                type: "nvarchar(254)",
                maxLength: 254,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "normalized_user_name",
                schema: "dbo",
                table: "users",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "phone_number_confirmed",
                schema: "dbo",
                table: "users",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "security_stamp",
                schema: "dbo",
                table: "users",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "two_factor_enabled",
                schema: "dbo",
                table: "users",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "user_name",
                schema: "dbo",
                table: "users",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "system",
                schema: "dbo",
                table: "tournaments",
                type: "varchar(24)",
                nullable: false,
                defaultValue: "Single elimination",
                oldClrType: typeof(string),
                oldType: "varchar(24)",
                oldDefaultValueSql: "('Single elimination')");

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "created_at",
                schema: "dbo",
                table: "tournaments",
                type: "datetimeoffset(0)",
                nullable: false,
                defaultValueSql: "SYSUTCDATETIME()",
                oldClrType: typeof(DateTimeOffset),
                oldType: "datetimeoffset(0)",
                oldDefaultValueSql: "(sysutcdatetime())");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "cancelled_at",
                schema: "dbo",
                table: "tournaments",
                type: "datetimeoffset(0)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "cancelled_by",
                schema: "dbo",
                table: "tournaments",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "status",
                schema: "dbo",
                table: "tournaments",
                type: "varchar(24)",
                nullable: false,
                defaultValue: "Scheduled");

            migrationBuilder.AlterColumn<string>(
                name: "status",
                schema: "dbo",
                table: "tournament_registrations",
                type: "varchar(24)",
                nullable: false,
                defaultValue: "Registered",
                oldClrType: typeof(string),
                oldType: "varchar(24)",
                oldDefaultValueSql: "('Registered')");

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "created_at",
                schema: "dbo",
                table: "tournament_registrations",
                type: "datetimeoffset(0)",
                nullable: false,
                defaultValueSql: "SYSUTCDATETIME()",
                oldClrType: typeof(DateTimeOffset),
                oldType: "datetimeoffset(0)",
                oldDefaultValueSql: "(sysutcdatetime())");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "withdrawn_at",
                schema: "dbo",
                table: "tournament_registrations",
                type: "datetimeoffset(0)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "withdrawn_by",
                schema: "dbo",
                table: "tournament_registrations",
                type: "int",
                nullable: true);

            migrationBuilder.AlterColumn<bool>(
                name: "is_bye",
                schema: "dbo",
                table: "tournament_matches",
                type: "bit",
                nullable: false,
                defaultValue: false,
                oldClrType: typeof(bool),
                oldType: "bit",
                oldDefaultValueSql: "((0))");

            migrationBuilder.AlterColumn<string>(
                name: "level",
                schema: "dbo",
                table: "teams",
                type: "varchar(24)",
                nullable: false,
                defaultValue: "Intermediate",
                oldClrType: typeof(string),
                oldType: "varchar(24)",
                oldDefaultValueSql: "('Intermediate')");

            migrationBuilder.AlterColumn<bool>(
                name: "is_recruiting",
                schema: "dbo",
                table: "teams",
                type: "bit",
                nullable: false,
                defaultValue: true,
                oldClrType: typeof(bool),
                oldType: "bit",
                oldDefaultValueSql: "((1))");

            migrationBuilder.AlterColumn<string>(
                name: "format",
                schema: "dbo",
                table: "teams",
                type: "varchar(24)",
                nullable: false,
                defaultValue: "5v5",
                oldClrType: typeof(string),
                oldType: "varchar(24)",
                oldDefaultValueSql: "('5v5')");

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "created_at",
                schema: "dbo",
                table: "teams",
                type: "datetimeoffset(0)",
                nullable: false,
                defaultValueSql: "SYSUTCDATETIME()",
                oldClrType: typeof(DateTimeOffset),
                oldType: "datetimeoffset(0)",
                oldDefaultValueSql: "(sysutcdatetime())");

            migrationBuilder.AlterColumn<string>(
                name: "color",
                schema: "dbo",
                table: "teams",
                type: "varchar(20)",
                nullable: false,
                defaultValue: "forest",
                oldClrType: typeof(string),
                oldType: "varchar(20)",
                oldDefaultValueSql: "('forest')");

            migrationBuilder.AlterColumn<int>(
                name: "capacity",
                schema: "dbo",
                table: "teams",
                type: "int",
                nullable: false,
                defaultValue: 12,
                oldClrType: typeof(int),
                oldType: "int",
                oldDefaultValueSql: "((12))");

            migrationBuilder.AlterColumn<string>(
                name: "status",
                schema: "dbo",
                table: "team_requests",
                type: "varchar(24)",
                nullable: false,
                defaultValue: "Pending",
                oldClrType: typeof(string),
                oldType: "varchar(24)",
                oldDefaultValueSql: "('Pending')");

            migrationBuilder.AlterColumn<string>(
                name: "request_type",
                schema: "dbo",
                table: "team_requests",
                type: "varchar(24)",
                nullable: false,
                defaultValue: "join",
                oldClrType: typeof(string),
                oldType: "varchar(24)",
                oldDefaultValueSql: "('join')");

            migrationBuilder.AlterColumn<string>(
                name: "position",
                schema: "dbo",
                table: "team_requests",
                type: "varchar(24)",
                nullable: false,
                defaultValue: "Midfielder",
                oldClrType: typeof(string),
                oldType: "varchar(24)",
                oldDefaultValueSql: "('Midfielder')");

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "created_at",
                schema: "dbo",
                table: "team_requests",
                type: "datetimeoffset(0)",
                nullable: false,
                defaultValueSql: "SYSUTCDATETIME()",
                oldClrType: typeof(DateTimeOffset),
                oldType: "datetimeoffset(0)",
                oldDefaultValueSql: "(sysutcdatetime())");

            migrationBuilder.AlterColumn<string>(
                name: "position",
                schema: "dbo",
                table: "team_members",
                type: "varchar(24)",
                nullable: false,
                defaultValue: "Midfielder",
                oldClrType: typeof(string),
                oldType: "varchar(24)",
                oldDefaultValueSql: "('Midfielder')");

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "joined_at",
                schema: "dbo",
                table: "team_members",
                type: "datetimeoffset(0)",
                nullable: false,
                defaultValueSql: "SYSUTCDATETIME()",
                oldClrType: typeof(DateTimeOffset),
                oldType: "datetimeoffset(0)",
                oldDefaultValueSql: "(sysutcdatetime())");

            migrationBuilder.AlterColumn<decimal>(
                name: "listing_fee",
                schema: "dbo",
                table: "settings",
                type: "decimal(12,2)",
                nullable: false,
                defaultValue: 5m,
                oldClrType: typeof(decimal),
                oldType: "decimal(12,2)",
                oldDefaultValueSql: "((5))");

            migrationBuilder.AlterColumn<decimal>(
                name: "commission_rate",
                schema: "dbo",
                table: "settings",
                type: "decimal(5,2)",
                nullable: false,
                defaultValue: 10m,
                oldClrType: typeof(decimal),
                oldType: "decimal(5,2)",
                oldDefaultValueSql: "((10))");

            migrationBuilder.AlterColumn<int>(
                name: "id",
                schema: "dbo",
                table: "settings",
                type: "int",
                nullable: false,
                defaultValue: 1,
                oldClrType: typeof(int),
                oldType: "int",
                oldDefaultValueSql: "((1))");

            migrationBuilder.AlterColumn<string>(
                name: "position",
                schema: "dbo",
                table: "registration_players",
                type: "varchar(24)",
                nullable: false,
                defaultValue: "Midfielder",
                oldClrType: typeof(string),
                oldType: "varchar(24)",
                oldDefaultValueSql: "('Midfielder')");

            migrationBuilder.AlterColumn<string>(
                name: "status",
                schema: "dbo",
                table: "payments",
                type: "varchar(24)",
                nullable: false,
                defaultValue: "Paid (demo)",
                oldClrType: typeof(string),
                oldType: "varchar(24)",
                oldDefaultValueSql: "('Paid (demo)')");

            migrationBuilder.AlterColumn<int>(
                name: "owner_id",
                schema: "dbo",
                table: "payments",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<bool>(
                name: "is_demo",
                schema: "dbo",
                table: "payments",
                type: "bit",
                nullable: false,
                defaultValue: true,
                oldClrType: typeof(bool),
                oldType: "bit",
                oldDefaultValueSql: "((1))");

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "created_at",
                schema: "dbo",
                table: "payments",
                type: "datetimeoffset(0)",
                nullable: false,
                defaultValueSql: "SYSUTCDATETIME()",
                oldClrType: typeof(DateTimeOffset),
                oldType: "datetimeoffset(0)",
                oldDefaultValueSql: "(sysutcdatetime())");

            migrationBuilder.AddColumn<int>(
                name: "platform_admin_id",
                schema: "dbo",
                table: "payments",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "refunded_at",
                schema: "dbo",
                table: "payments",
                type: "datetimeoffset(0)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "share_id",
                schema: "dbo",
                table: "payments",
                type: "int",
                nullable: true);

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "created_at",
                schema: "dbo",
                table: "ownership_documents",
                type: "datetimeoffset(0)",
                nullable: false,
                defaultValueSql: "SYSUTCDATETIME()",
                oldClrType: typeof(DateTimeOffset),
                oldType: "datetimeoffset(0)",
                oldDefaultValueSql: "(sysutcdatetime())");

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "created_at",
                schema: "dbo",
                table: "open_games",
                type: "datetimeoffset(0)",
                nullable: false,
                defaultValueSql: "SYSUTCDATETIME()",
                oldClrType: typeof(DateTimeOffset),
                oldType: "datetimeoffset(0)",
                oldDefaultValueSql: "(sysutcdatetime())");

            migrationBuilder.AddColumn<string>(
                name: "status",
                schema: "dbo",
                table: "open_games",
                type: "varchar(24)",
                nullable: false,
                defaultValue: "Scheduled");

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "created_at",
                schema: "dbo",
                table: "messages",
                type: "datetimeoffset(0)",
                nullable: false,
                defaultValueSql: "SYSUTCDATETIME()",
                oldClrType: typeof(DateTimeOffset),
                oldType: "datetimeoffset(0)",
                oldDefaultValueSql: "(sysutcdatetime())");

            migrationBuilder.AlterColumn<string>(
                name: "status",
                schema: "dbo",
                table: "game_participants",
                type: "varchar(24)",
                nullable: false,
                defaultValue: "Joined",
                oldClrType: typeof(string),
                oldType: "varchar(24)",
                oldDefaultValueSql: "('Joined')");

            migrationBuilder.AlterColumn<string>(
                name: "position",
                schema: "dbo",
                table: "game_participants",
                type: "varchar(24)",
                nullable: false,
                defaultValue: "Midfielder",
                oldClrType: typeof(string),
                oldType: "varchar(24)",
                oldDefaultValueSql: "('Midfielder')");

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "created_at",
                schema: "dbo",
                table: "game_participants",
                type: "datetimeoffset(0)",
                nullable: false,
                defaultValueSql: "SYSUTCDATETIME()",
                oldClrType: typeof(DateTimeOffset),
                oldType: "datetimeoffset(0)",
                oldDefaultValueSql: "(sysutcdatetime())");

            migrationBuilder.AlterColumn<string>(
                name: "location_precision",
                schema: "dbo",
                table: "fields",
                type: "varchar(24)",
                nullable: false,
                defaultValue: "unknown",
                oldClrType: typeof(string),
                oldType: "varchar(24)",
                oldDefaultValueSql: "('unknown')");

            migrationBuilder.AlterColumn<bool>(
                name: "is_archived",
                schema: "dbo",
                table: "fields",
                type: "bit",
                nullable: false,
                defaultValue: false,
                oldClrType: typeof(bool),
                oldType: "bit",
                oldDefaultValueSql: "((0))");

            migrationBuilder.AlterColumn<string>(
                name: "format",
                schema: "dbo",
                table: "fields",
                type: "varchar(24)",
                nullable: false,
                defaultValue: "5v5",
                oldClrType: typeof(string),
                oldType: "varchar(24)",
                oldDefaultValueSql: "('5v5')");

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "created_at",
                schema: "dbo",
                table: "fields",
                type: "datetimeoffset(0)",
                nullable: false,
                defaultValueSql: "SYSUTCDATETIME()",
                oldClrType: typeof(DateTimeOffset),
                oldType: "datetimeoffset(0)",
                oldDefaultValueSql: "(sysutcdatetime())");

            migrationBuilder.AddColumn<decimal>(
                name: "rating",
                schema: "dbo",
                table: "fields",
                type: "decimal(3,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AlterColumn<int>(
                name: "sort_order",
                schema: "dbo",
                table: "field_images",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldDefaultValueSql: "((0))");

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "created_at",
                schema: "dbo",
                table: "feedback",
                type: "datetimeoffset(0)",
                nullable: false,
                defaultValueSql: "SYSUTCDATETIME()",
                oldClrType: typeof(DateTimeOffset),
                oldType: "datetimeoffset(0)",
                oldDefaultValueSql: "(sysutcdatetime())");

            migrationBuilder.AlterColumn<string>(
                name: "status",
                schema: "dbo",
                table: "challenges",
                type: "varchar(24)",
                nullable: false,
                defaultValue: "Open",
                oldClrType: typeof(string),
                oldType: "varchar(24)",
                oldDefaultValueSql: "('Open')");

            migrationBuilder.AlterColumn<string>(
                name: "format",
                schema: "dbo",
                table: "challenges",
                type: "varchar(24)",
                nullable: false,
                defaultValue: "5v5",
                oldClrType: typeof(string),
                oldType: "varchar(24)",
                oldDefaultValueSql: "('5v5')");

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "created_at",
                schema: "dbo",
                table: "challenges",
                type: "datetimeoffset(0)",
                nullable: false,
                defaultValueSql: "SYSUTCDATETIME()",
                oldClrType: typeof(DateTimeOffset),
                oldType: "datetimeoffset(0)",
                oldDefaultValueSql: "(sysutcdatetime())");

            migrationBuilder.AlterColumn<string>(
                name: "cost_agreement",
                schema: "dbo",
                table: "challenges",
                type: "varchar(24)",
                nullable: false,
                defaultValue: "Half each",
                oldClrType: typeof(string),
                oldType: "varchar(24)",
                oldDefaultValueSql: "('Half each')");

            migrationBuilder.AddColumn<int>(
                name: "field_id",
                schema: "dbo",
                table: "challenges",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "payment_mode",
                schema: "dbo",
                table: "challenges",
                type: "varchar(24)",
                nullable: false,
                defaultValue: "Split checkout");

            migrationBuilder.AddColumn<decimal>(
                name: "quoted_price",
                schema: "dbo",
                table: "challenges",
                type: "decimal(12,2)",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "status",
                schema: "dbo",
                table: "challenge_requests",
                type: "varchar(24)",
                nullable: false,
                defaultValue: "Pending",
                oldClrType: typeof(string),
                oldType: "varchar(24)",
                oldDefaultValueSql: "('Pending')");

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "created_at",
                schema: "dbo",
                table: "challenge_requests",
                type: "datetimeoffset(0)",
                nullable: false,
                defaultValueSql: "SYSUTCDATETIME()",
                oldClrType: typeof(DateTimeOffset),
                oldType: "datetimeoffset(0)",
                oldDefaultValueSql: "(sysutcdatetime())");

            migrationBuilder.AlterColumn<string>(
                name: "status",
                schema: "dbo",
                table: "bookings",
                type: "varchar(24)",
                nullable: false,
                defaultValue: "Confirmed",
                oldClrType: typeof(string),
                oldType: "varchar(24)",
                oldDefaultValueSql: "('Confirmed')");

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "created_at",
                schema: "dbo",
                table: "bookings",
                type: "datetimeoffset(0)",
                nullable: false,
                defaultValueSql: "SYSUTCDATETIME()",
                oldClrType: typeof(DateTimeOffset),
                oldType: "datetimeoffset(0)",
                oldDefaultValueSql: "(sysutcdatetime())");

            migrationBuilder.AddColumn<string>(
                name: "cancellation_reason",
                schema: "dbo",
                table: "bookings",
                type: "varchar(24)",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "cancelled_at",
                schema: "dbo",
                table: "bookings",
                type: "datetimeoffset(0)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "cancelled_by",
                schema: "dbo",
                table: "bookings",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "commission",
                schema: "dbo",
                table: "bookings",
                type: "decimal(12,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "commission_rate",
                schema: "dbo",
                table: "bookings",
                type: "decimal(5,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<bool>(
                name: "is_split",
                schema: "dbo",
                table: "bookings",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "owner_id",
                schema: "dbo",
                table: "bookings",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "owner_net",
                schema: "dbo",
                table: "bookings",
                type: "decimal(12,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "payment_deadline",
                schema: "dbo",
                table: "bookings",
                type: "datetimeoffset(0)",
                nullable: true);

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "ends_at",
                schema: "dbo",
                table: "tournaments",
                type: "datetimeoffset(0)",
                nullable: false,
                computedColumnSql: "DATEADD(MINUTE, [duration_minutes], [starts_at])",
                stored: true,
                oldClrType: typeof(DateTimeOffset),
                oldType: "datetimeoffset(0)",
                oldNullable: true,
                oldComputedColumnSql: "(dateadd(minute,[duration_minutes],[starts_at]))",
                oldStored: true);

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "ends_at",
                schema: "dbo",
                table: "open_games",
                type: "datetimeoffset(0)",
                nullable: false,
                computedColumnSql: "DATEADD(MINUTE, [duration_minutes], [starts_at])",
                stored: true,
                oldClrType: typeof(DateTimeOffset),
                oldType: "datetimeoffset(0)",
                oldNullable: true,
                oldComputedColumnSql: "(dateadd(minute,[duration_minutes],[starts_at]))",
                oldStored: true);

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "ends_at",
                schema: "dbo",
                table: "bookings",
                type: "datetimeoffset(0)",
                nullable: false,
                computedColumnSql: "DATEADD(HOUR, 1, [starts_at])",
                stored: true,
                oldClrType: typeof(DateTimeOffset),
                oldType: "datetimeoffset(0)",
                oldNullable: true,
                oldComputedColumnSql: "(dateadd(hour,(1),[starts_at]))",
                oldStored: true);

            migrationBuilder.AddUniqueConstraint(
                name: "UQ_booking_teams_booking_id_team_id_captain_id",
                schema: "dbo",
                table: "booking_teams",
                columns: new[] { "booking_id", "team_id", "captain_id" });

            migrationBuilder.CreateTable(
                name: "AspNetRoles",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    NormalizedName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoles", x => x.Id);
                    table.CheckConstraint("CK_AspNetRoles_name", "[Name] IN (N'player',N'owner',N'admin')");
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserClaims",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    ClaimType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClaimValue = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetUserClaims_users_UserId",
                        column: x => x.UserId,
                        principalSchema: "dbo",
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserLogins",
                schema: "dbo",
                columns: table => new
                {
                    LoginProvider = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ProviderKey = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ProviderDisplayName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UserId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserLogins", x => new { x.LoginProvider, x.ProviderKey });
                    table.ForeignKey(
                        name: "FK_AspNetUserLogins_users_UserId",
                        column: x => x.UserId,
                        principalSchema: "dbo",
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserTokens",
                schema: "dbo",
                columns: table => new
                {
                    UserId = table.Column<int>(type: "int", nullable: false),
                    LoginProvider = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Value = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserTokens", x => new { x.UserId, x.LoginProvider, x.Name });
                    table.ForeignKey(
                        name: "FK_AspNetUserTokens_users_UserId",
                        column: x => x.UserId,
                        principalSchema: "dbo",
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "booking_shares",
                schema: "dbo",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    booking_id = table.Column<int>(type: "int", nullable: false),
                    team_id = table.Column<int>(type: "int", nullable: false),
                    payer_id = table.Column<int>(type: "int", nullable: false),
                    amount = table.Column<decimal>(type: "decimal(12,2)", nullable: false),
                    commission = table.Column<decimal>(type: "decimal(12,2)", nullable: false),
                    payment_state = table.Column<string>(type: "varchar(24)", nullable: false, defaultValue: "Unpaid"),
                    paid_at = table.Column<DateTimeOffset>(type: "datetimeoffset(0)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_booking_shares", x => x.id);
                    table.CheckConstraint("CK_booking_shares_amount", "[amount] >= 0");
                    table.CheckConstraint("CK_booking_shares_commission", "[commission] >= 0");
                    table.CheckConstraint("CK_booking_shares_payment_state_enum", "[payment_state] IN (N'Unpaid', N'Held', N'Paid', N'Refunded')");
                    table.CheckConstraint("CK_booking_shares_rule_1", "[commission] <= [amount]");
                    table.ForeignKey(
                        name: "FK_booking_shares_booking_id_0",
                        column: x => x.booking_id,
                        principalSchema: "dbo",
                        principalTable: "bookings",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_booking_shares_booking_id_team_id_payer_id_3",
                        columns: x => new { x.booking_id, x.team_id, x.payer_id },
                        principalSchema: "dbo",
                        principalTable: "booking_teams",
                        principalColumns: new[] { "booking_id", "team_id", "captain_id" });
                    table.ForeignKey(
                        name: "FK_booking_shares_payer_id_2",
                        column: x => x.payer_id,
                        principalSchema: "dbo",
                        principalTable: "users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_booking_shares_team_id_1",
                        column: x => x.team_id,
                        principalSchema: "dbo",
                        principalTable: "teams",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "team_recruiting_positions",
                schema: "dbo",
                columns: table => new
                {
                    team_id = table.Column<int>(type: "int", nullable: false),
                    position = table.Column<string>(type: "varchar(24)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_team_recruiting_positions", x => new { x.team_id, x.position });
                    table.CheckConstraint("CK_team_recruiting_positions_position_enum", "[position] IN (N'Goalkeeper', N'Defender', N'Midfielder', N'Forward')");
                    table.ForeignKey(
                        name: "FK_team_recruiting_positions_team_id_0",
                        column: x => x.team_id,
                        principalSchema: "dbo",
                        principalTable: "teams",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "AspNetRoleClaims",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoleId = table.Column<int>(type: "int", nullable: false),
                    ClaimType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClaimValue = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoleClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetRoleClaims_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalSchema: "dbo",
                        principalTable: "AspNetRoles",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserRoles",
                schema: "dbo",
                columns: table => new
                {
                    UserId = table.Column<int>(type: "int", nullable: false),
                    RoleId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserRoles", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalSchema: "dbo",
                        principalTable: "AspNetRoles",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_users_UserId",
                        column: x => x.UserId,
                        principalSchema: "dbo",
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.InsertData(
                schema: "dbo",
                table: "AspNetRoles",
                columns: new[] { "Id", "ConcurrencyStamp", "Name", "NormalizedName" },
                values: new object[,]
                {
                    { 1, "kicks-player-v1", "player", "PLAYER" },
                    { 2, "kicks-owner-v1", "owner", "OWNER" },
                    { 3, "kicks-admin-v1", "admin", "ADMIN" }
                });

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                schema: "dbo",
                table: "users",
                column: "normalized_email",
                unique: true,
                filter: "[normalized_email] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "UQ_users_email",
                schema: "dbo",
                table: "users",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                schema: "dbo",
                table: "users",
                column: "normalized_user_name",
                unique: true,
                filter: "[normalized_user_name] IS NOT NULL");

            migrationBuilder.AddCheckConstraint(
                name: "CK_users_requested_role_enum",
                schema: "dbo",
                table: "users",
                sql: "[requested_role] IN (N'player', N'owner')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_users_status_enum",
                schema: "dbo",
                table: "users",
                sql: "[status] IN (N'Pending', N'Active', N'Rejected', N'Inactive')");

            migrationBuilder.CreateIndex(
                name: "IX_tournaments_2",
                schema: "dbo",
                table: "tournaments",
                column: "cancelled_by");

            migrationBuilder.AddCheckConstraint(
                name: "CK_tournaments_capacity",
                schema: "dbo",
                table: "tournaments",
                sql: "[capacity] BETWEEN 2 AND 64");

            migrationBuilder.AddCheckConstraint(
                name: "CK_tournaments_duration_minutes",
                schema: "dbo",
                table: "tournaments",
                sql: "[duration_minutes] > 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_tournaments_entry_fee",
                schema: "dbo",
                table: "tournaments",
                sql: "[entry_fee] >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_tournaments_rule_1",
                schema: "dbo",
                table: "tournaments",
                sql: "[status] <> 'Cancelled' OR ([cancelled_by] IS NOT NULL AND [cancelled_at] IS NOT NULL)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_tournaments_status_enum",
                schema: "dbo",
                table: "tournaments",
                sql: "[status] IN (N'Scheduled', N'Cancelled')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_tournaments_system_enum",
                schema: "dbo",
                table: "tournaments",
                sql: "[system] IN (N'Single elimination', N'Group stage + knockout')");

            migrationBuilder.CreateIndex(
                name: "IX_tournament_registrations_3",
                schema: "dbo",
                table: "tournament_registrations",
                column: "withdrawn_by");

            migrationBuilder.CreateIndex(
                name: "UQ_tournament_registrations_tournament_id_captain_id",
                schema: "dbo",
                table: "tournament_registrations",
                columns: new[] { "tournament_id", "captain_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ_tournament_registrations_tournament_id_team_id",
                schema: "dbo",
                table: "tournament_registrations",
                columns: new[] { "tournament_id", "team_id" },
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_tournament_registrations_status_enum",
                schema: "dbo",
                table: "tournament_registrations",
                sql: "[status] IN (N'Registered', N'Withdrawn')");

            migrationBuilder.CreateIndex(
                name: "IX_tournament_matches_tournament_id_winner_registration_id",
                schema: "dbo",
                table: "tournament_matches",
                columns: new[] { "tournament_id", "winner_registration_id" });

            migrationBuilder.CreateIndex(
                name: "UQ_tournament_matches_tournament_id_round_number_match_number",
                schema: "dbo",
                table: "tournament_matches",
                columns: new[] { "tournament_id", "round_number", "match_number" },
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_tournament_matches_match_number",
                schema: "dbo",
                table: "tournament_matches",
                sql: "[match_number] BETWEEN 1 AND 32");

            migrationBuilder.AddCheckConstraint(
                name: "CK_tournament_matches_round_number",
                schema: "dbo",
                table: "tournament_matches",
                sql: "[round_number] BETWEEN 1 AND 6");

            migrationBuilder.CreateIndex(
                name: "IX_tournament_match_slots_tournament_id_match_id",
                schema: "dbo",
                table: "tournament_match_slots",
                columns: new[] { "tournament_id", "match_id" });

            migrationBuilder.CreateIndex(
                name: "IX_tournament_match_slots_tournament_id_registration_id",
                schema: "dbo",
                table: "tournament_match_slots",
                columns: new[] { "tournament_id", "registration_id" });

            migrationBuilder.CreateIndex(
                name: "IX_tournament_match_slots_tournament_id_source_match_id",
                schema: "dbo",
                table: "tournament_match_slots",
                columns: new[] { "tournament_id", "source_match_id" });

            migrationBuilder.AddCheckConstraint(
                name: "CK_tournament_match_slots_rule_1",
                schema: "dbo",
                table: "tournament_match_slots",
                sql: "[source_match_id] IS NULL OR [source_match_id] <> [match_id]");

            migrationBuilder.AddCheckConstraint(
                name: "CK_tournament_match_slots_slot_number",
                schema: "dbo",
                table: "tournament_match_slots",
                sql: "[slot_number] IN (1,2)");

            migrationBuilder.CreateIndex(
                name: "IX_tournament_draw_entries_tournament_id_registration_id",
                schema: "dbo",
                table: "tournament_draw_entries",
                columns: new[] { "tournament_id", "registration_id" });

            migrationBuilder.CreateIndex(
                name: "UQ_tournament_draw_entries_tournament_id_seed_position",
                schema: "dbo",
                table: "tournament_draw_entries",
                columns: new[] { "tournament_id", "seed_position" },
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_tournament_draw_entries_seed_position",
                schema: "dbo",
                table: "tournament_draw_entries",
                sql: "[seed_position] BETWEEN 1 AND 64");

            migrationBuilder.CreateIndex(
                name: "UQ_teams_captain_id",
                schema: "dbo",
                table: "teams",
                column: "captain_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ_teams_name",
                schema: "dbo",
                table: "teams",
                column: "name",
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_teams_capacity",
                schema: "dbo",
                table: "teams",
                sql: "[capacity] BETWEEN 5 AND 30");

            migrationBuilder.AddCheckConstraint(
                name: "CK_teams_format_enum",
                schema: "dbo",
                table: "teams",
                sql: "[format] IN (N'5v5', N'6v6', N'7v7')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_teams_level_enum",
                schema: "dbo",
                table: "teams",
                sql: "[level] IN (N'Casual', N'Intermediate', N'Competitive')");

            migrationBuilder.CreateIndex(
                name: "IX_team_requests_0",
                schema: "dbo",
                table: "team_requests",
                columns: new[] { "team_id", "user_id", "request_type" },
                unique: true,
                filter: "[status] = 'Pending'");

            migrationBuilder.AddCheckConstraint(
                name: "CK_team_requests_position_enum",
                schema: "dbo",
                table: "team_requests",
                sql: "[position] IN (N'Goalkeeper', N'Defender', N'Midfielder', N'Forward')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_team_requests_request_type_enum",
                schema: "dbo",
                table: "team_requests",
                sql: "[request_type] IN (N'join', N'leave')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_team_requests_status_enum",
                schema: "dbo",
                table: "team_requests",
                sql: "[status] IN (N'Pending', N'Accepted', N'Declined', N'Cancelled')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_team_members_position_enum",
                schema: "dbo",
                table: "team_members",
                sql: "[position] IN (N'Goalkeeper', N'Defender', N'Midfielder', N'Forward')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_settings_commission_rate",
                schema: "dbo",
                table: "settings",
                sql: "[commission_rate] BETWEEN 0 AND 100");

            migrationBuilder.AddCheckConstraint(
                name: "CK_settings_id",
                schema: "dbo",
                table: "settings",
                sql: "[id] = 1");

            migrationBuilder.AddCheckConstraint(
                name: "CK_settings_listing_fee",
                schema: "dbo",
                table: "settings",
                sql: "[listing_fee] >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_registration_players_position_enum",
                schema: "dbo",
                table: "registration_players",
                sql: "[position] IN (N'Goalkeeper', N'Defender', N'Midfielder', N'Forward')");

            migrationBuilder.CreateIndex(
                name: "IX_payments_0",
                schema: "dbo",
                table: "payments",
                column: "share_id",
                unique: true,
                filter: "[share_id] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_payments_1",
                schema: "dbo",
                table: "payments",
                columns: new[] { "payer_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "IX_payments_2",
                schema: "dbo",
                table: "payments",
                columns: new[] { "platform_admin_id", "status", "created_at" });

            migrationBuilder.AddCheckConstraint(
                name: "CK_payments_amount",
                schema: "dbo",
                table: "payments",
                sql: "[amount] >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_payments_commission",
                schema: "dbo",
                table: "payments",
                sql: "[commission] >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_payments_commission_rate",
                schema: "dbo",
                table: "payments",
                sql: "[commission_rate] BETWEEN 0 AND 100");

            migrationBuilder.AddCheckConstraint(
                name: "CK_payments_owner_net",
                schema: "dbo",
                table: "payments",
                sql: "[owner_net] >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_payments_rule_1",
                schema: "dbo",
                table: "payments",
                sql: "[amount] > 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_payments_rule_2",
                schema: "dbo",
                table: "payments",
                sql: "[commission] + [owner_net] = [amount]");

            migrationBuilder.AddCheckConstraint(
                name: "CK_payments_rule_3",
                schema: "dbo",
                table: "payments",
                sql: "CASE WHEN [field_id] IS NULL THEN 0 ELSE 1 END + CASE WHEN [booking_id] IS NULL THEN 0 ELSE 1 END + CASE WHEN [share_id] IS NULL THEN 0 ELSE 1 END + CASE WHEN [game_participant_id] IS NULL THEN 0 ELSE 1 END + CASE WHEN [registration_id] IS NULL THEN 0 ELSE 1 END = 1");

            migrationBuilder.AddCheckConstraint(
                name: "CK_payments_rule_4",
                schema: "dbo",
                table: "payments",
                sql: "[registration_id] IS NULL OR ([commission_rate] = 100 AND [commission] = [amount] AND [owner_net] = 0 AND [owner_id] IS NULL)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_payments_rule_5",
                schema: "dbo",
                table: "payments",
                sql: "[status] <> 'Held (demo)' OR [share_id] IS NOT NULL");

            migrationBuilder.AddCheckConstraint(
                name: "CK_payments_status_enum",
                schema: "dbo",
                table: "payments",
                sql: "[status] IN (N'Held (demo)', N'Paid (demo)', N'Refunded (demo)')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_ownership_documents_byte_size",
                schema: "dbo",
                table: "ownership_documents",
                sql: "[byte_size] BETWEEN 1 AND 1048576");

            migrationBuilder.AddCheckConstraint(
                name: "CK_open_games_capacity",
                schema: "dbo",
                table: "open_games",
                sql: "[capacity] >= 2");

            migrationBuilder.AddCheckConstraint(
                name: "CK_open_games_duration_minutes",
                schema: "dbo",
                table: "open_games",
                sql: "[duration_minutes] > 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_open_games_player_fee",
                schema: "dbo",
                table: "open_games",
                sql: "[player_fee] >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_open_games_status_enum",
                schema: "dbo",
                table: "open_games",
                sql: "[status] IN (N'Scheduled', N'Cancelled')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_messages_rule_1",
                schema: "dbo",
                table: "messages",
                sql: "[sender_id] <> [recipient_id]");

            migrationBuilder.CreateIndex(
                name: "UQ_game_participants_game_id_user_id",
                schema: "dbo",
                table: "game_participants",
                columns: new[] { "game_id", "user_id" },
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_game_participants_position_enum",
                schema: "dbo",
                table: "game_participants",
                sql: "[position] IN (N'Goalkeeper', N'Defender', N'Midfielder', N'Forward')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_game_participants_status_enum",
                schema: "dbo",
                table: "game_participants",
                sql: "[status] IN (N'Joined', N'Left')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_fields_format_enum",
                schema: "dbo",
                table: "fields",
                sql: "[format] IN (N'5v5', N'6v6', N'7v7')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_fields_hourly_price",
                schema: "dbo",
                table: "fields",
                sql: "[hourly_price] >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_fields_latitude",
                schema: "dbo",
                table: "fields",
                sql: "[latitude] BETWEEN -90 AND 90");

            migrationBuilder.AddCheckConstraint(
                name: "CK_fields_location_precision_enum",
                schema: "dbo",
                table: "fields",
                sql: "[location_precision] IN (N'exact', N'area', N'city', N'unknown')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_fields_longitude",
                schema: "dbo",
                table: "fields",
                sql: "[longitude] BETWEEN -180 AND 180");

            migrationBuilder.AddCheckConstraint(
                name: "CK_fields_rating",
                schema: "dbo",
                table: "fields",
                sql: "[rating] BETWEEN 0 AND 5");

            migrationBuilder.CreateIndex(
                name: "UQ_field_images_field_id_sort_order",
                schema: "dbo",
                table: "field_images",
                columns: new[] { "field_id", "sort_order" },
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_field_images_sort_order",
                schema: "dbo",
                table: "field_images",
                sql: "[sort_order] >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_feedback_rating",
                schema: "dbo",
                table: "feedback",
                sql: "[rating] BETWEEN 1 AND 5");

            migrationBuilder.CreateIndex(
                name: "IX_challenges_3",
                schema: "dbo",
                table: "challenges",
                column: "field_id");

            migrationBuilder.AddCheckConstraint(
                name: "CK_challenges_cost_agreement_enum",
                schema: "dbo",
                table: "challenges",
                sql: "[cost_agreement] IN (N'Half each', N'Host team pays', N'Visiting team pays')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_challenges_format_enum",
                schema: "dbo",
                table: "challenges",
                sql: "[format] IN (N'5v5', N'6v6', N'7v7')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_challenges_payment_mode_enum",
                schema: "dbo",
                table: "challenges",
                sql: "[payment_mode] IN (N'Split checkout', N'Legacy full')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_challenges_quoted_price",
                schema: "dbo",
                table: "challenges",
                sql: "[quoted_price] >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_challenges_rule_1",
                schema: "dbo",
                table: "challenges",
                sql: "[opponent_team_id] IS NULL OR [opponent_team_id] <> [host_team_id]");

            migrationBuilder.AddCheckConstraint(
                name: "CK_challenges_rule_2",
                schema: "dbo",
                table: "challenges",
                sql: "[payment_mode] <> 'Split checkout' OR ([field_id] IS NOT NULL AND [quoted_price] IS NOT NULL AND [cost_agreement] = 'Half each')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_challenges_status_enum",
                schema: "dbo",
                table: "challenges",
                sql: "[status] IN (N'Open', N'Agreed', N'Awaiting payment', N'Booked', N'Expired', N'Cancelled')");

            migrationBuilder.CreateIndex(
                name: "IX_challenge_requests_0",
                schema: "dbo",
                table: "challenge_requests",
                columns: new[] { "challenge_id", "team_id" },
                unique: true,
                filter: "[status] = 'Pending'");

            migrationBuilder.CreateIndex(
                name: "IX_challenge_requests_1",
                schema: "dbo",
                table: "challenge_requests",
                column: "challenge_id",
                unique: true,
                filter: "[status] = 'Accepted'");

            migrationBuilder.AddCheckConstraint(
                name: "CK_challenge_requests_status_enum",
                schema: "dbo",
                table: "challenge_requests",
                sql: "[status] IN (N'Pending', N'Accepted', N'Declined', N'Withdrawn')");

            migrationBuilder.CreateIndex(
                name: "IX_bookings_0",
                schema: "dbo",
                table: "bookings",
                columns: new[] { "field_id", "starts_at" },
                unique: true,
                filter: "[status] IN ('Pending','Confirmed')");

            migrationBuilder.CreateIndex(
                name: "IX_bookings_1",
                schema: "dbo",
                table: "bookings",
                column: "challenge_id",
                unique: true,
                filter: "[challenge_id] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_bookings_3",
                schema: "dbo",
                table: "bookings",
                column: "owner_id");

            migrationBuilder.CreateIndex(
                name: "IX_bookings_4",
                schema: "dbo",
                table: "bookings",
                column: "cancelled_by");

            migrationBuilder.AddCheckConstraint(
                name: "CK_bookings_cancellation_reason_enum",
                schema: "dbo",
                table: "bookings",
                sql: "[cancellation_reason] IN (N'Cancelled', N'Expired')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_bookings_commission",
                schema: "dbo",
                table: "bookings",
                sql: "[commission] >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_bookings_commission_rate",
                schema: "dbo",
                table: "bookings",
                sql: "[commission_rate] BETWEEN 0 AND 100");

            migrationBuilder.AddCheckConstraint(
                name: "CK_bookings_owner_net",
                schema: "dbo",
                table: "bookings",
                sql: "[owner_net] >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_bookings_price",
                schema: "dbo",
                table: "bookings",
                sql: "[price] >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_bookings_rule_1",
                schema: "dbo",
                table: "bookings",
                sql: "[commission] + [owner_net] = [price]");

            migrationBuilder.AddCheckConstraint(
                name: "CK_bookings_rule_2",
                schema: "dbo",
                table: "bookings",
                sql: "[is_split] = 0 OR ([challenge_id] IS NOT NULL AND [payment_deadline] IS NOT NULL)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_bookings_rule_3",
                schema: "dbo",
                table: "bookings",
                sql: "[payment_deadline] IS NULL OR [payment_deadline] <= [starts_at]");

            migrationBuilder.AddCheckConstraint(
                name: "CK_bookings_rule_4",
                schema: "dbo",
                table: "bookings",
                sql: "DATEPART(TZOFFSET,[starts_at]) = 180 AND DATEPART(HOUR,[starts_at]) BETWEEN 10 AND 22");

            migrationBuilder.AddCheckConstraint(
                name: "CK_bookings_starts_at",
                schema: "dbo",
                table: "bookings",
                sql: "DATEPART(MINUTE,[starts_at]) = 0 AND DATEPART(SECOND,[starts_at]) = 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_bookings_status_enum",
                schema: "dbo",
                table: "bookings",
                sql: "[status] IN (N'Pending', N'Confirmed', N'Completed', N'Cancelled')");

            migrationBuilder.CreateIndex(
                name: "UQ_amenities_name",
                schema: "dbo",
                table: "amenities",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AspNetRoleClaims_RoleId",
                schema: "dbo",
                table: "AspNetRoleClaims",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                schema: "dbo",
                table: "AspNetRoles",
                column: "NormalizedName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserClaims_UserId",
                schema: "dbo",
                table: "AspNetUserClaims",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserLogins_UserId",
                schema: "dbo",
                table: "AspNetUserLogins",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserRoles_RoleId",
                schema: "dbo",
                table: "AspNetUserRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "UX_AspNetUserRoles_UserId",
                schema: "dbo",
                table: "AspNetUserRoles",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_booking_shares_1",
                schema: "dbo",
                table: "booking_shares",
                column: "team_id");

            migrationBuilder.CreateIndex(
                name: "IX_booking_shares_2",
                schema: "dbo",
                table: "booking_shares",
                column: "payer_id");

            migrationBuilder.CreateIndex(
                name: "IX_booking_shares_booking_id_team_id_payer_id",
                schema: "dbo",
                table: "booking_shares",
                columns: new[] { "booking_id", "team_id", "payer_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ_booking_shares_booking_id_payer_id",
                schema: "dbo",
                table: "booking_shares",
                columns: new[] { "booking_id", "payer_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ_booking_shares_booking_id_team_id",
                schema: "dbo",
                table: "booking_shares",
                columns: new[] { "booking_id", "team_id" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_bookings_cancelled_by_4",
                schema: "dbo",
                table: "bookings",
                column: "cancelled_by",
                principalSchema: "dbo",
                principalTable: "users",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_bookings_owner_id_2",
                schema: "dbo",
                table: "bookings",
                column: "owner_id",
                principalSchema: "dbo",
                principalTable: "users",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_challenges_field_id_3",
                schema: "dbo",
                table: "challenges",
                column: "field_id",
                principalSchema: "dbo",
                principalTable: "fields",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_payments_platform_admin_id_1",
                schema: "dbo",
                table: "payments",
                column: "platform_admin_id",
                principalSchema: "dbo",
                principalTable: "users",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_payments_share_id_5",
                schema: "dbo",
                table: "payments",
                column: "share_id",
                principalSchema: "dbo",
                principalTable: "booking_shares",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_tournament_registrations_withdrawn_by_3",
                schema: "dbo",
                table: "tournament_registrations",
                column: "withdrawn_by",
                principalSchema: "dbo",
                principalTable: "users",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_tournaments_cancelled_by_2",
                schema: "dbo",
                table: "tournaments",
                column: "cancelled_by",
                principalSchema: "dbo",
                principalTable: "users",
                principalColumn: "id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_bookings_cancelled_by_4",
                schema: "dbo",
                table: "bookings");

            migrationBuilder.DropForeignKey(
                name: "FK_bookings_owner_id_2",
                schema: "dbo",
                table: "bookings");

            migrationBuilder.DropForeignKey(
                name: "FK_challenges_field_id_3",
                schema: "dbo",
                table: "challenges");

            migrationBuilder.DropForeignKey(
                name: "FK_payments_platform_admin_id_1",
                schema: "dbo",
                table: "payments");

            migrationBuilder.DropForeignKey(
                name: "FK_payments_share_id_5",
                schema: "dbo",
                table: "payments");

            migrationBuilder.DropForeignKey(
                name: "FK_tournament_registrations_withdrawn_by_3",
                schema: "dbo",
                table: "tournament_registrations");

            migrationBuilder.DropForeignKey(
                name: "FK_tournaments_cancelled_by_2",
                schema: "dbo",
                table: "tournaments");

            migrationBuilder.DropTable(
                name: "AspNetRoleClaims",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "AspNetUserClaims",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "AspNetUserLogins",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "AspNetUserRoles",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "AspNetUserTokens",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "booking_shares",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "team_recruiting_positions",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "AspNetRoles",
                schema: "dbo");

            migrationBuilder.DropIndex(
                name: "EmailIndex",
                schema: "dbo",
                table: "users");

            migrationBuilder.DropIndex(
                name: "UQ_users_email",
                schema: "dbo",
                table: "users");

            migrationBuilder.DropIndex(
                name: "UserNameIndex",
                schema: "dbo",
                table: "users");

            migrationBuilder.DropCheckConstraint(
                name: "CK_users_requested_role_enum",
                schema: "dbo",
                table: "users");

            migrationBuilder.DropCheckConstraint(
                name: "CK_users_status_enum",
                schema: "dbo",
                table: "users");

            migrationBuilder.DropIndex(
                name: "IX_tournaments_2",
                schema: "dbo",
                table: "tournaments");

            migrationBuilder.DropCheckConstraint(
                name: "CK_tournaments_capacity",
                schema: "dbo",
                table: "tournaments");

            migrationBuilder.DropCheckConstraint(
                name: "CK_tournaments_duration_minutes",
                schema: "dbo",
                table: "tournaments");

            migrationBuilder.DropCheckConstraint(
                name: "CK_tournaments_entry_fee",
                schema: "dbo",
                table: "tournaments");

            migrationBuilder.DropCheckConstraint(
                name: "CK_tournaments_rule_1",
                schema: "dbo",
                table: "tournaments");

            migrationBuilder.DropCheckConstraint(
                name: "CK_tournaments_status_enum",
                schema: "dbo",
                table: "tournaments");

            migrationBuilder.DropCheckConstraint(
                name: "CK_tournaments_system_enum",
                schema: "dbo",
                table: "tournaments");

            migrationBuilder.DropIndex(
                name: "IX_tournament_registrations_3",
                schema: "dbo",
                table: "tournament_registrations");

            migrationBuilder.DropIndex(
                name: "UQ_tournament_registrations_tournament_id_captain_id",
                schema: "dbo",
                table: "tournament_registrations");

            migrationBuilder.DropIndex(
                name: "UQ_tournament_registrations_tournament_id_team_id",
                schema: "dbo",
                table: "tournament_registrations");

            migrationBuilder.DropCheckConstraint(
                name: "CK_tournament_registrations_status_enum",
                schema: "dbo",
                table: "tournament_registrations");

            migrationBuilder.DropIndex(
                name: "IX_tournament_matches_tournament_id_winner_registration_id",
                schema: "dbo",
                table: "tournament_matches");

            migrationBuilder.DropIndex(
                name: "UQ_tournament_matches_tournament_id_round_number_match_number",
                schema: "dbo",
                table: "tournament_matches");

            migrationBuilder.DropCheckConstraint(
                name: "CK_tournament_matches_match_number",
                schema: "dbo",
                table: "tournament_matches");

            migrationBuilder.DropCheckConstraint(
                name: "CK_tournament_matches_round_number",
                schema: "dbo",
                table: "tournament_matches");

            migrationBuilder.DropIndex(
                name: "IX_tournament_match_slots_tournament_id_match_id",
                schema: "dbo",
                table: "tournament_match_slots");

            migrationBuilder.DropIndex(
                name: "IX_tournament_match_slots_tournament_id_registration_id",
                schema: "dbo",
                table: "tournament_match_slots");

            migrationBuilder.DropIndex(
                name: "IX_tournament_match_slots_tournament_id_source_match_id",
                schema: "dbo",
                table: "tournament_match_slots");

            migrationBuilder.DropCheckConstraint(
                name: "CK_tournament_match_slots_rule_1",
                schema: "dbo",
                table: "tournament_match_slots");

            migrationBuilder.DropCheckConstraint(
                name: "CK_tournament_match_slots_slot_number",
                schema: "dbo",
                table: "tournament_match_slots");

            migrationBuilder.DropIndex(
                name: "IX_tournament_draw_entries_tournament_id_registration_id",
                schema: "dbo",
                table: "tournament_draw_entries");

            migrationBuilder.DropIndex(
                name: "UQ_tournament_draw_entries_tournament_id_seed_position",
                schema: "dbo",
                table: "tournament_draw_entries");

            migrationBuilder.DropCheckConstraint(
                name: "CK_tournament_draw_entries_seed_position",
                schema: "dbo",
                table: "tournament_draw_entries");

            migrationBuilder.DropIndex(
                name: "UQ_teams_captain_id",
                schema: "dbo",
                table: "teams");

            migrationBuilder.DropIndex(
                name: "UQ_teams_name",
                schema: "dbo",
                table: "teams");

            migrationBuilder.DropCheckConstraint(
                name: "CK_teams_capacity",
                schema: "dbo",
                table: "teams");

            migrationBuilder.DropCheckConstraint(
                name: "CK_teams_format_enum",
                schema: "dbo",
                table: "teams");

            migrationBuilder.DropCheckConstraint(
                name: "CK_teams_level_enum",
                schema: "dbo",
                table: "teams");

            migrationBuilder.DropIndex(
                name: "IX_team_requests_0",
                schema: "dbo",
                table: "team_requests");

            migrationBuilder.DropCheckConstraint(
                name: "CK_team_requests_position_enum",
                schema: "dbo",
                table: "team_requests");

            migrationBuilder.DropCheckConstraint(
                name: "CK_team_requests_request_type_enum",
                schema: "dbo",
                table: "team_requests");

            migrationBuilder.DropCheckConstraint(
                name: "CK_team_requests_status_enum",
                schema: "dbo",
                table: "team_requests");

            migrationBuilder.DropCheckConstraint(
                name: "CK_team_members_position_enum",
                schema: "dbo",
                table: "team_members");

            migrationBuilder.DropCheckConstraint(
                name: "CK_settings_commission_rate",
                schema: "dbo",
                table: "settings");

            migrationBuilder.DropCheckConstraint(
                name: "CK_settings_id",
                schema: "dbo",
                table: "settings");

            migrationBuilder.DropCheckConstraint(
                name: "CK_settings_listing_fee",
                schema: "dbo",
                table: "settings");

            migrationBuilder.DropCheckConstraint(
                name: "CK_registration_players_position_enum",
                schema: "dbo",
                table: "registration_players");

            migrationBuilder.DropIndex(
                name: "IX_payments_0",
                schema: "dbo",
                table: "payments");

            migrationBuilder.DropIndex(
                name: "IX_payments_1",
                schema: "dbo",
                table: "payments");

            migrationBuilder.DropIndex(
                name: "IX_payments_2",
                schema: "dbo",
                table: "payments");

            migrationBuilder.DropCheckConstraint(
                name: "CK_payments_amount",
                schema: "dbo",
                table: "payments");

            migrationBuilder.DropCheckConstraint(
                name: "CK_payments_commission",
                schema: "dbo",
                table: "payments");

            migrationBuilder.DropCheckConstraint(
                name: "CK_payments_commission_rate",
                schema: "dbo",
                table: "payments");

            migrationBuilder.DropCheckConstraint(
                name: "CK_payments_owner_net",
                schema: "dbo",
                table: "payments");

            migrationBuilder.DropCheckConstraint(
                name: "CK_payments_rule_1",
                schema: "dbo",
                table: "payments");

            migrationBuilder.DropCheckConstraint(
                name: "CK_payments_rule_2",
                schema: "dbo",
                table: "payments");

            migrationBuilder.DropCheckConstraint(
                name: "CK_payments_rule_3",
                schema: "dbo",
                table: "payments");

            migrationBuilder.DropCheckConstraint(
                name: "CK_payments_rule_4",
                schema: "dbo",
                table: "payments");

            migrationBuilder.DropCheckConstraint(
                name: "CK_payments_rule_5",
                schema: "dbo",
                table: "payments");

            migrationBuilder.DropCheckConstraint(
                name: "CK_payments_status_enum",
                schema: "dbo",
                table: "payments");

            migrationBuilder.DropCheckConstraint(
                name: "CK_ownership_documents_byte_size",
                schema: "dbo",
                table: "ownership_documents");

            migrationBuilder.DropCheckConstraint(
                name: "CK_open_games_capacity",
                schema: "dbo",
                table: "open_games");

            migrationBuilder.DropCheckConstraint(
                name: "CK_open_games_duration_minutes",
                schema: "dbo",
                table: "open_games");

            migrationBuilder.DropCheckConstraint(
                name: "CK_open_games_player_fee",
                schema: "dbo",
                table: "open_games");

            migrationBuilder.DropCheckConstraint(
                name: "CK_open_games_status_enum",
                schema: "dbo",
                table: "open_games");

            migrationBuilder.DropCheckConstraint(
                name: "CK_messages_rule_1",
                schema: "dbo",
                table: "messages");

            migrationBuilder.DropIndex(
                name: "UQ_game_participants_game_id_user_id",
                schema: "dbo",
                table: "game_participants");

            migrationBuilder.DropCheckConstraint(
                name: "CK_game_participants_position_enum",
                schema: "dbo",
                table: "game_participants");

            migrationBuilder.DropCheckConstraint(
                name: "CK_game_participants_status_enum",
                schema: "dbo",
                table: "game_participants");

            migrationBuilder.DropCheckConstraint(
                name: "CK_fields_format_enum",
                schema: "dbo",
                table: "fields");

            migrationBuilder.DropCheckConstraint(
                name: "CK_fields_hourly_price",
                schema: "dbo",
                table: "fields");

            migrationBuilder.DropCheckConstraint(
                name: "CK_fields_latitude",
                schema: "dbo",
                table: "fields");

            migrationBuilder.DropCheckConstraint(
                name: "CK_fields_location_precision_enum",
                schema: "dbo",
                table: "fields");

            migrationBuilder.DropCheckConstraint(
                name: "CK_fields_longitude",
                schema: "dbo",
                table: "fields");

            migrationBuilder.DropCheckConstraint(
                name: "CK_fields_rating",
                schema: "dbo",
                table: "fields");

            migrationBuilder.DropIndex(
                name: "UQ_field_images_field_id_sort_order",
                schema: "dbo",
                table: "field_images");

            migrationBuilder.DropCheckConstraint(
                name: "CK_field_images_sort_order",
                schema: "dbo",
                table: "field_images");

            migrationBuilder.DropCheckConstraint(
                name: "CK_feedback_rating",
                schema: "dbo",
                table: "feedback");

            migrationBuilder.DropIndex(
                name: "IX_challenges_3",
                schema: "dbo",
                table: "challenges");

            migrationBuilder.DropCheckConstraint(
                name: "CK_challenges_cost_agreement_enum",
                schema: "dbo",
                table: "challenges");

            migrationBuilder.DropCheckConstraint(
                name: "CK_challenges_format_enum",
                schema: "dbo",
                table: "challenges");

            migrationBuilder.DropCheckConstraint(
                name: "CK_challenges_payment_mode_enum",
                schema: "dbo",
                table: "challenges");

            migrationBuilder.DropCheckConstraint(
                name: "CK_challenges_quoted_price",
                schema: "dbo",
                table: "challenges");

            migrationBuilder.DropCheckConstraint(
                name: "CK_challenges_rule_1",
                schema: "dbo",
                table: "challenges");

            migrationBuilder.DropCheckConstraint(
                name: "CK_challenges_rule_2",
                schema: "dbo",
                table: "challenges");

            migrationBuilder.DropCheckConstraint(
                name: "CK_challenges_status_enum",
                schema: "dbo",
                table: "challenges");

            migrationBuilder.DropIndex(
                name: "IX_challenge_requests_0",
                schema: "dbo",
                table: "challenge_requests");

            migrationBuilder.DropIndex(
                name: "IX_challenge_requests_1",
                schema: "dbo",
                table: "challenge_requests");

            migrationBuilder.DropCheckConstraint(
                name: "CK_challenge_requests_status_enum",
                schema: "dbo",
                table: "challenge_requests");

            migrationBuilder.DropIndex(
                name: "IX_bookings_0",
                schema: "dbo",
                table: "bookings");

            migrationBuilder.DropIndex(
                name: "IX_bookings_1",
                schema: "dbo",
                table: "bookings");

            migrationBuilder.DropIndex(
                name: "IX_bookings_3",
                schema: "dbo",
                table: "bookings");

            migrationBuilder.DropIndex(
                name: "IX_bookings_4",
                schema: "dbo",
                table: "bookings");

            migrationBuilder.DropCheckConstraint(
                name: "CK_bookings_cancellation_reason_enum",
                schema: "dbo",
                table: "bookings");

            migrationBuilder.DropCheckConstraint(
                name: "CK_bookings_commission",
                schema: "dbo",
                table: "bookings");

            migrationBuilder.DropCheckConstraint(
                name: "CK_bookings_commission_rate",
                schema: "dbo",
                table: "bookings");

            migrationBuilder.DropCheckConstraint(
                name: "CK_bookings_owner_net",
                schema: "dbo",
                table: "bookings");

            migrationBuilder.DropCheckConstraint(
                name: "CK_bookings_price",
                schema: "dbo",
                table: "bookings");

            migrationBuilder.DropCheckConstraint(
                name: "CK_bookings_rule_1",
                schema: "dbo",
                table: "bookings");

            migrationBuilder.DropCheckConstraint(
                name: "CK_bookings_rule_2",
                schema: "dbo",
                table: "bookings");

            migrationBuilder.DropCheckConstraint(
                name: "CK_bookings_rule_3",
                schema: "dbo",
                table: "bookings");

            migrationBuilder.DropCheckConstraint(
                name: "CK_bookings_rule_4",
                schema: "dbo",
                table: "bookings");

            migrationBuilder.DropCheckConstraint(
                name: "CK_bookings_starts_at",
                schema: "dbo",
                table: "bookings");

            migrationBuilder.DropCheckConstraint(
                name: "CK_bookings_status_enum",
                schema: "dbo",
                table: "bookings");

            migrationBuilder.DropUniqueConstraint(
                name: "UQ_booking_teams_booking_id_team_id_captain_id",
                schema: "dbo",
                table: "booking_teams");

            migrationBuilder.DropIndex(
                name: "UQ_amenities_name",
                schema: "dbo",
                table: "amenities");

            migrationBuilder.DropColumn(
                name: "access_failed_count",
                schema: "dbo",
                table: "users");

            migrationBuilder.DropColumn(
                name: "address",
                schema: "dbo",
                table: "users");

            migrationBuilder.DropColumn(
                name: "concurrency_stamp",
                schema: "dbo",
                table: "users");

            migrationBuilder.DropColumn(
                name: "email_confirmed",
                schema: "dbo",
                table: "users");

            migrationBuilder.DropColumn(
                name: "lockout_enabled",
                schema: "dbo",
                table: "users");

            migrationBuilder.DropColumn(
                name: "lockout_end",
                schema: "dbo",
                table: "users");

            migrationBuilder.DropColumn(
                name: "normalized_email",
                schema: "dbo",
                table: "users");

            migrationBuilder.DropColumn(
                name: "normalized_user_name",
                schema: "dbo",
                table: "users");

            migrationBuilder.DropColumn(
                name: "phone_number_confirmed",
                schema: "dbo",
                table: "users");

            migrationBuilder.DropColumn(
                name: "security_stamp",
                schema: "dbo",
                table: "users");

            migrationBuilder.DropColumn(
                name: "two_factor_enabled",
                schema: "dbo",
                table: "users");

            migrationBuilder.DropColumn(
                name: "user_name",
                schema: "dbo",
                table: "users");

            migrationBuilder.DropColumn(
                name: "cancelled_at",
                schema: "dbo",
                table: "tournaments");

            migrationBuilder.DropColumn(
                name: "cancelled_by",
                schema: "dbo",
                table: "tournaments");

            migrationBuilder.DropColumn(
                name: "status",
                schema: "dbo",
                table: "tournaments");

            migrationBuilder.DropColumn(
                name: "withdrawn_at",
                schema: "dbo",
                table: "tournament_registrations");

            migrationBuilder.DropColumn(
                name: "withdrawn_by",
                schema: "dbo",
                table: "tournament_registrations");

            migrationBuilder.DropColumn(
                name: "platform_admin_id",
                schema: "dbo",
                table: "payments");

            migrationBuilder.DropColumn(
                name: "refunded_at",
                schema: "dbo",
                table: "payments");

            migrationBuilder.DropColumn(
                name: "share_id",
                schema: "dbo",
                table: "payments");

            migrationBuilder.DropColumn(
                name: "status",
                schema: "dbo",
                table: "open_games");

            migrationBuilder.DropColumn(
                name: "rating",
                schema: "dbo",
                table: "fields");

            migrationBuilder.DropColumn(
                name: "field_id",
                schema: "dbo",
                table: "challenges");

            migrationBuilder.DropColumn(
                name: "payment_mode",
                schema: "dbo",
                table: "challenges");

            migrationBuilder.DropColumn(
                name: "quoted_price",
                schema: "dbo",
                table: "challenges");

            migrationBuilder.DropColumn(
                name: "cancellation_reason",
                schema: "dbo",
                table: "bookings");

            migrationBuilder.DropColumn(
                name: "cancelled_at",
                schema: "dbo",
                table: "bookings");

            migrationBuilder.DropColumn(
                name: "cancelled_by",
                schema: "dbo",
                table: "bookings");

            migrationBuilder.DropColumn(
                name: "commission",
                schema: "dbo",
                table: "bookings");

            migrationBuilder.DropColumn(
                name: "commission_rate",
                schema: "dbo",
                table: "bookings");

            migrationBuilder.DropColumn(
                name: "is_split",
                schema: "dbo",
                table: "bookings");

            migrationBuilder.DropColumn(
                name: "owner_id",
                schema: "dbo",
                table: "bookings");

            migrationBuilder.DropColumn(
                name: "owner_net",
                schema: "dbo",
                table: "bookings");

            migrationBuilder.DropColumn(
                name: "payment_deadline",
                schema: "dbo",
                table: "bookings");

            migrationBuilder.RenameIndex(
                name: "IX_payments_7",
                schema: "dbo",
                table: "payments",
                newName: "IX_payments_5");

            migrationBuilder.RenameIndex(
                name: "IX_payments_6",
                schema: "dbo",
                table: "payments",
                newName: "IX_payments_4");

            migrationBuilder.RenameIndex(
                name: "IX_payments_5",
                schema: "dbo",
                table: "payments",
                newName: "IX_payments_3");

            migrationBuilder.RenameIndex(
                name: "IX_payments_4",
                schema: "dbo",
                table: "payments",
                newName: "IX_payments_2");

            migrationBuilder.RenameIndex(
                name: "IX_payments_3",
                schema: "dbo",
                table: "payments",
                newName: "IX_payments_1");

            migrationBuilder.RenameIndex(
                name: "IX_challenge_requests_2",
                schema: "dbo",
                table: "challenge_requests",
                newName: "IX_challenge_requests_1");

            migrationBuilder.AlterColumn<string>(
                name: "status",
                schema: "dbo",
                table: "users",
                type: "varchar(24)",
                nullable: false,
                defaultValueSql: "('Pending')",
                oldClrType: typeof(string),
                oldType: "varchar(24)",
                oldDefaultValue: "Pending");

            migrationBuilder.AlterColumn<string>(
                name: "password_hash",
                schema: "dbo",
                table: "users",
                type: "varchar(512)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "created_at",
                schema: "dbo",
                table: "users",
                type: "datetimeoffset(0)",
                nullable: false,
                defaultValueSql: "(sysutcdatetime())",
                oldClrType: typeof(DateTimeOffset),
                oldType: "datetimeoffset(0)",
                oldDefaultValueSql: "SYSUTCDATETIME()");

            migrationBuilder.AddColumn<string>(
                name: "password_algorithm",
                schema: "dbo",
                table: "users",
                type: "varchar(40)",
                nullable: false,
                defaultValueSql: "('PBKDF2-SHA256')");

            migrationBuilder.AddColumn<int>(
                name: "password_iterations",
                schema: "dbo",
                table: "users",
                type: "int",
                nullable: false,
                defaultValueSql: "((100000))");

            migrationBuilder.AddColumn<string>(
                name: "password_salt",
                schema: "dbo",
                table: "users",
                type: "varchar(128)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "role",
                schema: "dbo",
                table: "users",
                type: "varchar(24)",
                nullable: false,
                defaultValueSql: "('player')");

            migrationBuilder.AlterColumn<string>(
                name: "system",
                schema: "dbo",
                table: "tournaments",
                type: "varchar(24)",
                nullable: false,
                defaultValueSql: "('Single elimination')",
                oldClrType: typeof(string),
                oldType: "varchar(24)",
                oldDefaultValue: "Single elimination");

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "created_at",
                schema: "dbo",
                table: "tournaments",
                type: "datetimeoffset(0)",
                nullable: false,
                defaultValueSql: "(sysutcdatetime())",
                oldClrType: typeof(DateTimeOffset),
                oldType: "datetimeoffset(0)",
                oldDefaultValueSql: "SYSUTCDATETIME()");

            migrationBuilder.AlterColumn<string>(
                name: "status",
                schema: "dbo",
                table: "tournament_registrations",
                type: "varchar(24)",
                nullable: false,
                defaultValueSql: "('Registered')",
                oldClrType: typeof(string),
                oldType: "varchar(24)",
                oldDefaultValue: "Registered");

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "created_at",
                schema: "dbo",
                table: "tournament_registrations",
                type: "datetimeoffset(0)",
                nullable: false,
                defaultValueSql: "(sysutcdatetime())",
                oldClrType: typeof(DateTimeOffset),
                oldType: "datetimeoffset(0)",
                oldDefaultValueSql: "SYSUTCDATETIME()");

            migrationBuilder.AlterColumn<bool>(
                name: "is_bye",
                schema: "dbo",
                table: "tournament_matches",
                type: "bit",
                nullable: false,
                defaultValueSql: "((0))",
                oldClrType: typeof(bool),
                oldType: "bit",
                oldDefaultValue: false);

            migrationBuilder.AlterColumn<string>(
                name: "level",
                schema: "dbo",
                table: "teams",
                type: "varchar(24)",
                nullable: false,
                defaultValueSql: "('Intermediate')",
                oldClrType: typeof(string),
                oldType: "varchar(24)",
                oldDefaultValue: "Intermediate");

            migrationBuilder.AlterColumn<bool>(
                name: "is_recruiting",
                schema: "dbo",
                table: "teams",
                type: "bit",
                nullable: false,
                defaultValueSql: "((1))",
                oldClrType: typeof(bool),
                oldType: "bit",
                oldDefaultValue: true);

            migrationBuilder.AlterColumn<string>(
                name: "format",
                schema: "dbo",
                table: "teams",
                type: "varchar(24)",
                nullable: false,
                defaultValueSql: "('5v5')",
                oldClrType: typeof(string),
                oldType: "varchar(24)",
                oldDefaultValue: "5v5");

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "created_at",
                schema: "dbo",
                table: "teams",
                type: "datetimeoffset(0)",
                nullable: false,
                defaultValueSql: "(sysutcdatetime())",
                oldClrType: typeof(DateTimeOffset),
                oldType: "datetimeoffset(0)",
                oldDefaultValueSql: "SYSUTCDATETIME()");

            migrationBuilder.AlterColumn<string>(
                name: "color",
                schema: "dbo",
                table: "teams",
                type: "varchar(20)",
                nullable: false,
                defaultValueSql: "('forest')",
                oldClrType: typeof(string),
                oldType: "varchar(20)",
                oldDefaultValue: "forest");

            migrationBuilder.AlterColumn<int>(
                name: "capacity",
                schema: "dbo",
                table: "teams",
                type: "int",
                nullable: false,
                defaultValueSql: "((12))",
                oldClrType: typeof(int),
                oldType: "int",
                oldDefaultValue: 12);

            migrationBuilder.AlterColumn<string>(
                name: "status",
                schema: "dbo",
                table: "team_requests",
                type: "varchar(24)",
                nullable: false,
                defaultValueSql: "('Pending')",
                oldClrType: typeof(string),
                oldType: "varchar(24)",
                oldDefaultValue: "Pending");

            migrationBuilder.AlterColumn<string>(
                name: "request_type",
                schema: "dbo",
                table: "team_requests",
                type: "varchar(24)",
                nullable: false,
                defaultValueSql: "('join')",
                oldClrType: typeof(string),
                oldType: "varchar(24)",
                oldDefaultValue: "join");

            migrationBuilder.AlterColumn<string>(
                name: "position",
                schema: "dbo",
                table: "team_requests",
                type: "varchar(24)",
                nullable: false,
                defaultValueSql: "('Midfielder')",
                oldClrType: typeof(string),
                oldType: "varchar(24)",
                oldDefaultValue: "Midfielder");

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "created_at",
                schema: "dbo",
                table: "team_requests",
                type: "datetimeoffset(0)",
                nullable: false,
                defaultValueSql: "(sysutcdatetime())",
                oldClrType: typeof(DateTimeOffset),
                oldType: "datetimeoffset(0)",
                oldDefaultValueSql: "SYSUTCDATETIME()");

            migrationBuilder.AlterColumn<string>(
                name: "position",
                schema: "dbo",
                table: "team_members",
                type: "varchar(24)",
                nullable: false,
                defaultValueSql: "('Midfielder')",
                oldClrType: typeof(string),
                oldType: "varchar(24)",
                oldDefaultValue: "Midfielder");

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "joined_at",
                schema: "dbo",
                table: "team_members",
                type: "datetimeoffset(0)",
                nullable: false,
                defaultValueSql: "(sysutcdatetime())",
                oldClrType: typeof(DateTimeOffset),
                oldType: "datetimeoffset(0)",
                oldDefaultValueSql: "SYSUTCDATETIME()");

            migrationBuilder.AlterColumn<decimal>(
                name: "listing_fee",
                schema: "dbo",
                table: "settings",
                type: "decimal(12,2)",
                nullable: false,
                defaultValueSql: "((5))",
                oldClrType: typeof(decimal),
                oldType: "decimal(12,2)",
                oldDefaultValue: 5m);

            migrationBuilder.AlterColumn<decimal>(
                name: "commission_rate",
                schema: "dbo",
                table: "settings",
                type: "decimal(5,2)",
                nullable: false,
                defaultValueSql: "((10))",
                oldClrType: typeof(decimal),
                oldType: "decimal(5,2)",
                oldDefaultValue: 10m);

            migrationBuilder.AlterColumn<int>(
                name: "id",
                schema: "dbo",
                table: "settings",
                type: "int",
                nullable: false,
                defaultValueSql: "((1))",
                oldClrType: typeof(int),
                oldType: "int",
                oldDefaultValue: 1);

            migrationBuilder.AlterColumn<string>(
                name: "position",
                schema: "dbo",
                table: "registration_players",
                type: "varchar(24)",
                nullable: false,
                defaultValueSql: "('Midfielder')",
                oldClrType: typeof(string),
                oldType: "varchar(24)",
                oldDefaultValue: "Midfielder");

            migrationBuilder.AlterColumn<string>(
                name: "status",
                schema: "dbo",
                table: "payments",
                type: "varchar(24)",
                nullable: false,
                defaultValueSql: "('Paid (demo)')",
                oldClrType: typeof(string),
                oldType: "varchar(24)",
                oldDefaultValue: "Paid (demo)");

            migrationBuilder.AlterColumn<int>(
                name: "owner_id",
                schema: "dbo",
                table: "payments",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<bool>(
                name: "is_demo",
                schema: "dbo",
                table: "payments",
                type: "bit",
                nullable: false,
                defaultValueSql: "((1))",
                oldClrType: typeof(bool),
                oldType: "bit",
                oldDefaultValue: true);

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "created_at",
                schema: "dbo",
                table: "payments",
                type: "datetimeoffset(0)",
                nullable: false,
                defaultValueSql: "(sysutcdatetime())",
                oldClrType: typeof(DateTimeOffset),
                oldType: "datetimeoffset(0)",
                oldDefaultValueSql: "SYSUTCDATETIME()");

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "created_at",
                schema: "dbo",
                table: "ownership_documents",
                type: "datetimeoffset(0)",
                nullable: false,
                defaultValueSql: "(sysutcdatetime())",
                oldClrType: typeof(DateTimeOffset),
                oldType: "datetimeoffset(0)",
                oldDefaultValueSql: "SYSUTCDATETIME()");

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "created_at",
                schema: "dbo",
                table: "open_games",
                type: "datetimeoffset(0)",
                nullable: false,
                defaultValueSql: "(sysutcdatetime())",
                oldClrType: typeof(DateTimeOffset),
                oldType: "datetimeoffset(0)",
                oldDefaultValueSql: "SYSUTCDATETIME()");

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "created_at",
                schema: "dbo",
                table: "messages",
                type: "datetimeoffset(0)",
                nullable: false,
                defaultValueSql: "(sysutcdatetime())",
                oldClrType: typeof(DateTimeOffset),
                oldType: "datetimeoffset(0)",
                oldDefaultValueSql: "SYSUTCDATETIME()");

            migrationBuilder.AlterColumn<string>(
                name: "status",
                schema: "dbo",
                table: "game_participants",
                type: "varchar(24)",
                nullable: false,
                defaultValueSql: "('Joined')",
                oldClrType: typeof(string),
                oldType: "varchar(24)",
                oldDefaultValue: "Joined");

            migrationBuilder.AlterColumn<string>(
                name: "position",
                schema: "dbo",
                table: "game_participants",
                type: "varchar(24)",
                nullable: false,
                defaultValueSql: "('Midfielder')",
                oldClrType: typeof(string),
                oldType: "varchar(24)",
                oldDefaultValue: "Midfielder");

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "created_at",
                schema: "dbo",
                table: "game_participants",
                type: "datetimeoffset(0)",
                nullable: false,
                defaultValueSql: "(sysutcdatetime())",
                oldClrType: typeof(DateTimeOffset),
                oldType: "datetimeoffset(0)",
                oldDefaultValueSql: "SYSUTCDATETIME()");

            migrationBuilder.AlterColumn<string>(
                name: "location_precision",
                schema: "dbo",
                table: "fields",
                type: "varchar(24)",
                nullable: false,
                defaultValueSql: "('unknown')",
                oldClrType: typeof(string),
                oldType: "varchar(24)",
                oldDefaultValue: "unknown");

            migrationBuilder.AlterColumn<bool>(
                name: "is_archived",
                schema: "dbo",
                table: "fields",
                type: "bit",
                nullable: false,
                defaultValueSql: "((0))",
                oldClrType: typeof(bool),
                oldType: "bit",
                oldDefaultValue: false);

            migrationBuilder.AlterColumn<string>(
                name: "format",
                schema: "dbo",
                table: "fields",
                type: "varchar(24)",
                nullable: false,
                defaultValueSql: "('5v5')",
                oldClrType: typeof(string),
                oldType: "varchar(24)",
                oldDefaultValue: "5v5");

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "created_at",
                schema: "dbo",
                table: "fields",
                type: "datetimeoffset(0)",
                nullable: false,
                defaultValueSql: "(sysutcdatetime())",
                oldClrType: typeof(DateTimeOffset),
                oldType: "datetimeoffset(0)",
                oldDefaultValueSql: "SYSUTCDATETIME()");

            migrationBuilder.AlterColumn<int>(
                name: "sort_order",
                schema: "dbo",
                table: "field_images",
                type: "int",
                nullable: false,
                defaultValueSql: "((0))",
                oldClrType: typeof(int),
                oldType: "int",
                oldDefaultValue: 0);

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "created_at",
                schema: "dbo",
                table: "feedback",
                type: "datetimeoffset(0)",
                nullable: false,
                defaultValueSql: "(sysutcdatetime())",
                oldClrType: typeof(DateTimeOffset),
                oldType: "datetimeoffset(0)",
                oldDefaultValueSql: "SYSUTCDATETIME()");

            migrationBuilder.AlterColumn<string>(
                name: "status",
                schema: "dbo",
                table: "challenges",
                type: "varchar(24)",
                nullable: false,
                defaultValueSql: "('Open')",
                oldClrType: typeof(string),
                oldType: "varchar(24)",
                oldDefaultValue: "Open");

            migrationBuilder.AlterColumn<string>(
                name: "format",
                schema: "dbo",
                table: "challenges",
                type: "varchar(24)",
                nullable: false,
                defaultValueSql: "('5v5')",
                oldClrType: typeof(string),
                oldType: "varchar(24)",
                oldDefaultValue: "5v5");

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "created_at",
                schema: "dbo",
                table: "challenges",
                type: "datetimeoffset(0)",
                nullable: false,
                defaultValueSql: "(sysutcdatetime())",
                oldClrType: typeof(DateTimeOffset),
                oldType: "datetimeoffset(0)",
                oldDefaultValueSql: "SYSUTCDATETIME()");

            migrationBuilder.AlterColumn<string>(
                name: "cost_agreement",
                schema: "dbo",
                table: "challenges",
                type: "varchar(24)",
                nullable: false,
                defaultValueSql: "('Half each')",
                oldClrType: typeof(string),
                oldType: "varchar(24)",
                oldDefaultValue: "Half each");

            migrationBuilder.AlterColumn<string>(
                name: "status",
                schema: "dbo",
                table: "challenge_requests",
                type: "varchar(24)",
                nullable: false,
                defaultValueSql: "('Pending')",
                oldClrType: typeof(string),
                oldType: "varchar(24)",
                oldDefaultValue: "Pending");

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "created_at",
                schema: "dbo",
                table: "challenge_requests",
                type: "datetimeoffset(0)",
                nullable: false,
                defaultValueSql: "(sysutcdatetime())",
                oldClrType: typeof(DateTimeOffset),
                oldType: "datetimeoffset(0)",
                oldDefaultValueSql: "SYSUTCDATETIME()");

            migrationBuilder.AlterColumn<string>(
                name: "status",
                schema: "dbo",
                table: "bookings",
                type: "varchar(24)",
                nullable: false,
                defaultValueSql: "('Confirmed')",
                oldClrType: typeof(string),
                oldType: "varchar(24)",
                oldDefaultValue: "Confirmed");

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "created_at",
                schema: "dbo",
                table: "bookings",
                type: "datetimeoffset(0)",
                nullable: false,
                defaultValueSql: "(sysutcdatetime())",
                oldClrType: typeof(DateTimeOffset),
                oldType: "datetimeoffset(0)",
                oldDefaultValueSql: "SYSUTCDATETIME()");

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "ends_at",
                schema: "dbo",
                table: "tournaments",
                type: "datetimeoffset(0)",
                nullable: true,
                computedColumnSql: "(dateadd(minute,[duration_minutes],[starts_at]))",
                stored: true,
                oldClrType: typeof(DateTimeOffset),
                oldType: "datetimeoffset(0)",
                oldComputedColumnSql: "DATEADD(MINUTE, [duration_minutes], [starts_at])",
                oldStored: true);

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "ends_at",
                schema: "dbo",
                table: "open_games",
                type: "datetimeoffset(0)",
                nullable: true,
                computedColumnSql: "(dateadd(minute,[duration_minutes],[starts_at]))",
                stored: true,
                oldClrType: typeof(DateTimeOffset),
                oldType: "datetimeoffset(0)",
                oldComputedColumnSql: "DATEADD(MINUTE, [duration_minutes], [starts_at])",
                oldStored: true);

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "ends_at",
                schema: "dbo",
                table: "bookings",
                type: "datetimeoffset(0)",
                nullable: true,
                computedColumnSql: "(dateadd(hour,(1),[starts_at]))",
                stored: true,
                oldClrType: typeof(DateTimeOffset),
                oldType: "datetimeoffset(0)",
                oldComputedColumnSql: "DATEADD(HOUR, 1, [starts_at])",
                oldStored: true);

            migrationBuilder.AddUniqueConstraint(
                name: "UQ_users_email",
                schema: "dbo",
                table: "users",
                column: "email");

            migrationBuilder.AddUniqueConstraint(
                name: "UQ_tournament_registrations_tournament_id_captain_id",
                schema: "dbo",
                table: "tournament_registrations",
                columns: new[] { "tournament_id", "captain_id" });

            migrationBuilder.AddUniqueConstraint(
                name: "UQ_tournament_registrations_tournament_id_team_id",
                schema: "dbo",
                table: "tournament_registrations",
                columns: new[] { "tournament_id", "team_id" });

            migrationBuilder.AddUniqueConstraint(
                name: "UQ_tournament_matches_tournament_id_round_number_match_number",
                schema: "dbo",
                table: "tournament_matches",
                columns: new[] { "tournament_id", "round_number", "match_number" });

            migrationBuilder.AddUniqueConstraint(
                name: "UQ_tournament_draw_entries_tournament_id_seed_position",
                schema: "dbo",
                table: "tournament_draw_entries",
                columns: new[] { "tournament_id", "seed_position" });

            migrationBuilder.AddUniqueConstraint(
                name: "UQ_teams_captain_id",
                schema: "dbo",
                table: "teams",
                column: "captain_id");

            migrationBuilder.AddUniqueConstraint(
                name: "UQ_teams_name",
                schema: "dbo",
                table: "teams",
                column: "name");

            migrationBuilder.AddUniqueConstraint(
                name: "UQ_meetings_room_id",
                schema: "dbo",
                table: "meetings",
                column: "room_id");

            migrationBuilder.AddUniqueConstraint(
                name: "UQ_game_participants_game_id_user_id",
                schema: "dbo",
                table: "game_participants",
                columns: new[] { "game_id", "user_id" });

            migrationBuilder.AddUniqueConstraint(
                name: "UQ_field_images_field_id_sort_order",
                schema: "dbo",
                table: "field_images",
                columns: new[] { "field_id", "sort_order" });

            migrationBuilder.AddUniqueConstraint(
                name: "UQ_amenities_name",
                schema: "dbo",
                table: "amenities",
                column: "name");

            migrationBuilder.AddCheckConstraint(
                name: "CK_users_password_iterations",
                schema: "dbo",
                table: "users",
                sql: "([password_iterations]>=(100000))");

            migrationBuilder.AddCheckConstraint(
                name: "CK_users_requested_role_enum",
                schema: "dbo",
                table: "users",
                sql: "([requested_role]=N'owner' OR [requested_role]=N'player')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_users_role_enum",
                schema: "dbo",
                table: "users",
                sql: "([role]=N'admin' OR [role]=N'owner' OR [role]=N'player')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_users_status_enum",
                schema: "dbo",
                table: "users",
                sql: "([status]=N'Inactive' OR [status]=N'Rejected' OR [status]=N'Active' OR [status]=N'Pending')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_tournaments_capacity",
                schema: "dbo",
                table: "tournaments",
                sql: "([capacity]>=(2) AND [capacity]<=(64))");

            migrationBuilder.AddCheckConstraint(
                name: "CK_tournaments_duration_minutes",
                schema: "dbo",
                table: "tournaments",
                sql: "([duration_minutes]>(0))");

            migrationBuilder.AddCheckConstraint(
                name: "CK_tournaments_entry_fee",
                schema: "dbo",
                table: "tournaments",
                sql: "([entry_fee]>=(0))");

            migrationBuilder.AddCheckConstraint(
                name: "CK_tournaments_system_enum",
                schema: "dbo",
                table: "tournaments",
                sql: "([system]=N'Group stage + knockout' OR [system]=N'Single elimination')");

            migrationBuilder.CreateIndex(
                name: "IX_tournament_registrations_0",
                schema: "dbo",
                table: "tournament_registrations",
                column: "tournament_id");

            migrationBuilder.AddCheckConstraint(
                name: "CK_tournament_registrations_status_enum",
                schema: "dbo",
                table: "tournament_registrations",
                sql: "([status]=N'Withdrawn' OR [status]=N'Registered')");

            migrationBuilder.CreateIndex(
                name: "IX_tournament_matches_0",
                schema: "dbo",
                table: "tournament_matches",
                column: "tournament_id");

            migrationBuilder.AddCheckConstraint(
                name: "CK_tournament_matches_match_number",
                schema: "dbo",
                table: "tournament_matches",
                sql: "([match_number]>=(1) AND [match_number]<=(32))");

            migrationBuilder.AddCheckConstraint(
                name: "CK_tournament_matches_round_number",
                schema: "dbo",
                table: "tournament_matches",
                sql: "([round_number]>=(1) AND [round_number]<=(6))");

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

            migrationBuilder.AddCheckConstraint(
                name: "CK_tournament_match_slots_rule_1",
                schema: "dbo",
                table: "tournament_match_slots",
                sql: "([source_match_id] IS NULL OR [source_match_id]<>[match_id])");

            migrationBuilder.AddCheckConstraint(
                name: "CK_tournament_match_slots_slot_number",
                schema: "dbo",
                table: "tournament_match_slots",
                sql: "([slot_number]=(2) OR [slot_number]=(1))");

            migrationBuilder.CreateIndex(
                name: "IX_tournament_draw_entries_0",
                schema: "dbo",
                table: "tournament_draw_entries",
                column: "tournament_id");

            migrationBuilder.AddCheckConstraint(
                name: "CK_tournament_draw_entries_seed_position",
                schema: "dbo",
                table: "tournament_draw_entries",
                sql: "([seed_position]>=(1) AND [seed_position]<=(64))");

            migrationBuilder.AddCheckConstraint(
                name: "CK_teams_capacity",
                schema: "dbo",
                table: "teams",
                sql: "([capacity]>=(5) AND [capacity]<=(30))");

            migrationBuilder.AddCheckConstraint(
                name: "CK_teams_format_enum",
                schema: "dbo",
                table: "teams",
                sql: "([format]=N'7v7' OR [format]=N'6v6' OR [format]=N'5v5')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_teams_level_enum",
                schema: "dbo",
                table: "teams",
                sql: "([level]=N'Competitive' OR [level]=N'Intermediate' OR [level]=N'Casual')");

            migrationBuilder.CreateIndex(
                name: "IX_team_requests_0",
                schema: "dbo",
                table: "team_requests",
                columns: new[] { "team_id", "user_id", "request_type" },
                unique: true,
                filter: "([status]='Pending')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_team_requests_position_enum",
                schema: "dbo",
                table: "team_requests",
                sql: "([position]=N'Forward' OR [position]=N'Midfielder' OR [position]=N'Defender' OR [position]=N'Goalkeeper')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_team_requests_request_type_enum",
                schema: "dbo",
                table: "team_requests",
                sql: "([request_type]=N'leave' OR [request_type]=N'join')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_team_requests_status_enum",
                schema: "dbo",
                table: "team_requests",
                sql: "([status]=N'Cancelled' OR [status]=N'Declined' OR [status]=N'Accepted' OR [status]=N'Pending')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_team_members_position_enum",
                schema: "dbo",
                table: "team_members",
                sql: "([position]=N'Forward' OR [position]=N'Midfielder' OR [position]=N'Defender' OR [position]=N'Goalkeeper')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_settings_commission_rate",
                schema: "dbo",
                table: "settings",
                sql: "([commission_rate]>=(0) AND [commission_rate]<=(100))");

            migrationBuilder.AddCheckConstraint(
                name: "CK_settings_id",
                schema: "dbo",
                table: "settings",
                sql: "([id]=(1))");

            migrationBuilder.AddCheckConstraint(
                name: "CK_settings_listing_fee",
                schema: "dbo",
                table: "settings",
                sql: "([listing_fee]>=(0))");

            migrationBuilder.CreateIndex(
                name: "IX_registration_players_0",
                schema: "dbo",
                table: "registration_players",
                column: "registration_id");

            migrationBuilder.AddCheckConstraint(
                name: "CK_registration_players_position_enum",
                schema: "dbo",
                table: "registration_players",
                sql: "([position]=N'Forward' OR [position]=N'Midfielder' OR [position]=N'Defender' OR [position]=N'Goalkeeper')");

            migrationBuilder.CreateIndex(
                name: "IX_payments_0",
                schema: "dbo",
                table: "payments",
                column: "payer_id");

            migrationBuilder.AddCheckConstraint(
                name: "CK_payments_amount",
                schema: "dbo",
                table: "payments",
                sql: "([amount]>=(0))");

            migrationBuilder.AddCheckConstraint(
                name: "CK_payments_commission",
                schema: "dbo",
                table: "payments",
                sql: "([commission]>=(0))");

            migrationBuilder.AddCheckConstraint(
                name: "CK_payments_commission_rate",
                schema: "dbo",
                table: "payments",
                sql: "([commission_rate]>=(0) AND [commission_rate]<=(100))");

            migrationBuilder.AddCheckConstraint(
                name: "CK_payments_owner_net",
                schema: "dbo",
                table: "payments",
                sql: "([owner_net]>=(0))");

            migrationBuilder.AddCheckConstraint(
                name: "CK_payments_rule_1",
                schema: "dbo",
                table: "payments",
                sql: "(([commission]+[owner_net])=[amount])");

            migrationBuilder.AddCheckConstraint(
                name: "CK_payments_rule_2",
                schema: "dbo",
                table: "payments",
                sql: "((((case when [field_id] IS NULL then (0) else (1) end+case when [booking_id] IS NULL then (0) else (1) end)+case when [game_participant_id] IS NULL then (0) else (1) end)+case when [registration_id] IS NULL then (0) else (1) end)=(1))");

            migrationBuilder.AddCheckConstraint(
                name: "CK_payments_status_enum",
                schema: "dbo",
                table: "payments",
                sql: "([status]=N'Free' OR [status]=N'Refunded (demo)' OR [status]=N'Paid (demo)')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_ownership_documents_byte_size",
                schema: "dbo",
                table: "ownership_documents",
                sql: "([byte_size]>=(1) AND [byte_size]<=(1048576))");

            migrationBuilder.AddCheckConstraint(
                name: "CK_open_games_capacity",
                schema: "dbo",
                table: "open_games",
                sql: "([capacity]>=(2))");

            migrationBuilder.AddCheckConstraint(
                name: "CK_open_games_duration_minutes",
                schema: "dbo",
                table: "open_games",
                sql: "([duration_minutes]>(0))");

            migrationBuilder.AddCheckConstraint(
                name: "CK_open_games_player_fee",
                schema: "dbo",
                table: "open_games",
                sql: "([player_fee]>=(0))");

            migrationBuilder.AddCheckConstraint(
                name: "CK_messages_rule_1",
                schema: "dbo",
                table: "messages",
                sql: "([sender_id]<>[recipient_id])");

            migrationBuilder.CreateIndex(
                name: "IX_game_participants_0",
                schema: "dbo",
                table: "game_participants",
                column: "game_id");

            migrationBuilder.AddCheckConstraint(
                name: "CK_game_participants_position_enum",
                schema: "dbo",
                table: "game_participants",
                sql: "([position]=N'Forward' OR [position]=N'Midfielder' OR [position]=N'Defender' OR [position]=N'Goalkeeper')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_game_participants_status_enum",
                schema: "dbo",
                table: "game_participants",
                sql: "([status]=N'Left' OR [status]=N'Joined')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_fields_format_enum",
                schema: "dbo",
                table: "fields",
                sql: "([format]=N'7v7' OR [format]=N'6v6' OR [format]=N'5v5')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_fields_hourly_price",
                schema: "dbo",
                table: "fields",
                sql: "([hourly_price]>=(0))");

            migrationBuilder.AddCheckConstraint(
                name: "CK_fields_latitude",
                schema: "dbo",
                table: "fields",
                sql: "([latitude]>=(-90) AND [latitude]<=(90))");

            migrationBuilder.AddCheckConstraint(
                name: "CK_fields_location_precision_enum",
                schema: "dbo",
                table: "fields",
                sql: "([location_precision]=N'unknown' OR [location_precision]=N'city' OR [location_precision]=N'area' OR [location_precision]=N'exact')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_fields_longitude",
                schema: "dbo",
                table: "fields",
                sql: "([longitude]>=(-180) AND [longitude]<=(180))");

            migrationBuilder.CreateIndex(
                name: "IX_field_images_0",
                schema: "dbo",
                table: "field_images",
                column: "field_id");

            migrationBuilder.AddCheckConstraint(
                name: "CK_field_images_sort_order",
                schema: "dbo",
                table: "field_images",
                sql: "([sort_order]>=(0))");

            migrationBuilder.CreateIndex(
                name: "IX_field_amenities_0",
                schema: "dbo",
                table: "field_amenities",
                column: "field_id");

            migrationBuilder.AddCheckConstraint(
                name: "CK_feedback_rating",
                schema: "dbo",
                table: "feedback",
                sql: "([rating]>=(1) AND [rating]<=(5))");

            migrationBuilder.AddCheckConstraint(
                name: "CK_challenges_cost_agreement_enum",
                schema: "dbo",
                table: "challenges",
                sql: "([cost_agreement]=N'Visiting team pays' OR [cost_agreement]=N'Host team pays' OR [cost_agreement]=N'Half each')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_challenges_format_enum",
                schema: "dbo",
                table: "challenges",
                sql: "([format]=N'7v7' OR [format]=N'6v6' OR [format]=N'5v5')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_challenges_rule_1",
                schema: "dbo",
                table: "challenges",
                sql: "([opponent_team_id] IS NULL OR [opponent_team_id]<>[host_team_id])");

            migrationBuilder.AddCheckConstraint(
                name: "CK_challenges_status_enum",
                schema: "dbo",
                table: "challenges",
                sql: "([status]=N'Cancelled' OR [status]=N'Booked' OR [status]=N'Agreed' OR [status]=N'Open')");

            migrationBuilder.CreateIndex(
                name: "IX_challenge_requests_0",
                schema: "dbo",
                table: "challenge_requests",
                columns: new[] { "challenge_id", "team_id" },
                unique: true,
                filter: "([status]='Pending')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_challenge_requests_status_enum",
                schema: "dbo",
                table: "challenge_requests",
                sql: "([status]=N'Withdrawn' OR [status]=N'Declined' OR [status]=N'Accepted' OR [status]=N'Pending')");

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

            migrationBuilder.AddCheckConstraint(
                name: "CK_bookings_price",
                schema: "dbo",
                table: "bookings",
                sql: "([price]>=(0))");

            migrationBuilder.AddCheckConstraint(
                name: "CK_bookings_starts_at",
                schema: "dbo",
                table: "bookings",
                sql: "(datepart(minute,[starts_at])=(0) AND datepart(second,[starts_at])=(0))");

            migrationBuilder.AddCheckConstraint(
                name: "CK_bookings_status_enum",
                schema: "dbo",
                table: "bookings",
                sql: "([status]=N'Cancelled' OR [status]=N'Confirmed' OR [status]=N'Pending')");

            migrationBuilder.CreateIndex(
                name: "IX_booking_teams_0",
                schema: "dbo",
                table: "booking_teams",
                column: "booking_id");
        }
    }
}
