using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KicksMvc.Data.Migrations
{
    /// <inheritdoc />
    public partial class TournamentManagementPhase6 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("IF DB_NAME()<>N'Kicks' AND DB_NAME() NOT LIKE N'Kicks[_]BookingTest[_]%' THROW 51040, 'Unreviewed tournament migration target.', 1;");
            migrationBuilder.AddColumn<Guid>(
                name: "creation_key",
                schema: "dbo",
                table: "tournaments",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "description",
                schema: "dbo",
                table: "tournaments",
                type: "nvarchar(2000)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "match_duration_minutes",
                schema: "dbo",
                table: "tournaments",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "registration_closes_at",
                schema: "dbo",
                table: "tournaments",
                type: "datetimeoffset(0)",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "registration_opens_at",
                schema: "dbo",
                table: "tournaments",
                type: "datetimeoffset(0)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "away_penalties",
                schema: "dbo",
                table: "tournament_matches",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "away_score",
                schema: "dbo",
                table: "tournament_matches",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "duration_minutes",
                schema: "dbo",
                table: "tournament_matches",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "home_penalties",
                schema: "dbo",
                table: "tournament_matches",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "home_score",
                schema: "dbo",
                table: "tournament_matches",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "starts_at",
                schema: "dbo",
                table: "tournament_matches",
                type: "datetimeoffset(0)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "status",
                schema: "dbo",
                table: "tournament_matches",
                type: "varchar(24)",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ends_at",
                schema: "dbo",
                table: "tournament_matches",
                type: "datetimeoffset(0)",
                nullable: true,
                computedColumnSql: "DATEADD(MINUTE, [duration_minutes], [starts_at])",
                stored: true);

            migrationBuilder.CreateIndex(
                name: "UQ_tournaments_creation_key",
                schema: "dbo",
                table: "tournaments",
                column: "creation_key",
                unique: true,
                filter: "[creation_key] IS NOT NULL");

            migrationBuilder.AddCheckConstraint(
                name: "CK_tournaments_phase6",
                schema: "dbo",
                table: "tournaments",
                sql: "[match_duration_minutes] IS NULL OR ([match_duration_minutes] IN (60,120) AND [creation_key] IS NOT NULL AND [registration_opens_at] IS NOT NULL AND [registration_closes_at] IS NOT NULL AND [registration_opens_at] < [registration_closes_at] AND [registration_closes_at] <= [starts_at] AND DATEPART(TZOFFSET,[registration_opens_at])=180 AND DATEPART(TZOFFSET,[registration_closes_at])=180)");

            migrationBuilder.CreateIndex(
                name: "IX_tournament_matches_schedule",
                schema: "dbo",
                table: "tournament_matches",
                column: "starts_at",
                filter: "[starts_at] IS NOT NULL");

            migrationBuilder.AddCheckConstraint(
                name: "CK_tournament_matches_phase6_duration",
                schema: "dbo",
                table: "tournament_matches",
                sql: "[duration_minutes] IS NULL OR [duration_minutes] IN (60,120)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_tournament_matches_phase6_result",
                schema: "dbo",
                table: "tournament_matches",
                sql: "[status] <> 'Completed' OR ([winner_registration_id] IS NOT NULL AND ([is_bye]=1 OR ([starts_at] IS NOT NULL AND [duration_minutes] IS NOT NULL AND [home_score] IS NOT NULL AND [away_score] IS NOT NULL AND (([home_score]<>[away_score] AND [home_penalties] IS NULL AND [away_penalties] IS NULL) OR ([home_score]=[away_score] AND [home_penalties] IS NOT NULL AND [away_penalties] IS NOT NULL AND [home_penalties]<>[away_penalties])))))");

            migrationBuilder.AddCheckConstraint(
                name: "CK_tournament_matches_phase6_schedule",
                schema: "dbo",
                table: "tournament_matches",
                sql: "[status] <> 'Scheduled' OR ([starts_at] IS NOT NULL AND [duration_minutes] IS NOT NULL AND DATEPART(TZOFFSET,[starts_at])=180 AND CONVERT(time(0),[starts_at])=TIMEFROMPARTS(DATEPART(HOUR,[starts_at]),0,0,0,0) AND DATEPART(HOUR,[starts_at])>=13 AND DATEPART(HOUR,[starts_at])+[duration_minutes]/60<=23)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_tournament_matches_phase6_scores",
                schema: "dbo",
                table: "tournament_matches",
                sql: "([home_score] IS NULL OR [home_score] BETWEEN 0 AND 99) AND ([away_score] IS NULL OR [away_score] BETWEEN 0 AND 99) AND ([home_penalties] IS NULL OR [home_penalties] BETWEEN 0 AND 99) AND ([away_penalties] IS NULL OR [away_penalties] BETWEEN 0 AND 99)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_tournament_matches_phase6_status",
                schema: "dbo",
                table: "tournament_matches",
                sql: "[status] IS NULL OR [status] IN ('Draft','Scheduled','Completed','Cancelled')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("IF EXISTS(SELECT 1 FROM dbo.tournaments WITH(TABLOCKX,HOLDLOCK) WHERE creation_key IS NOT NULL OR description IS NOT NULL OR match_duration_minutes IS NOT NULL OR registration_opens_at IS NOT NULL OR registration_closes_at IS NOT NULL) OR EXISTS(SELECT 1 FROM dbo.tournament_matches WITH(TABLOCKX,HOLDLOCK) WHERE status IS NOT NULL OR starts_at IS NOT NULL OR duration_minutes IS NOT NULL OR home_score IS NOT NULL OR away_score IS NOT NULL OR home_penalties IS NOT NULL OR away_penalties IS NOT NULL) THROW 51041, 'Retained Phase 6 tournament data prevents a destructive downgrade.', 1;");
            migrationBuilder.DropIndex(
                name: "UQ_tournaments_creation_key",
                schema: "dbo",
                table: "tournaments");

            migrationBuilder.DropCheckConstraint(
                name: "CK_tournaments_phase6",
                schema: "dbo",
                table: "tournaments");

            migrationBuilder.DropIndex(
                name: "IX_tournament_matches_schedule",
                schema: "dbo",
                table: "tournament_matches");

            migrationBuilder.DropCheckConstraint(
                name: "CK_tournament_matches_phase6_duration",
                schema: "dbo",
                table: "tournament_matches");

            migrationBuilder.DropCheckConstraint(
                name: "CK_tournament_matches_phase6_result",
                schema: "dbo",
                table: "tournament_matches");

            migrationBuilder.DropCheckConstraint(
                name: "CK_tournament_matches_phase6_schedule",
                schema: "dbo",
                table: "tournament_matches");

            migrationBuilder.DropCheckConstraint(
                name: "CK_tournament_matches_phase6_scores",
                schema: "dbo",
                table: "tournament_matches");

            migrationBuilder.DropCheckConstraint(
                name: "CK_tournament_matches_phase6_status",
                schema: "dbo",
                table: "tournament_matches");

            migrationBuilder.DropColumn(
                name: "ends_at",
                schema: "dbo",
                table: "tournament_matches");

            migrationBuilder.DropColumn(
                name: "creation_key",
                schema: "dbo",
                table: "tournaments");

            migrationBuilder.DropColumn(
                name: "description",
                schema: "dbo",
                table: "tournaments");

            migrationBuilder.DropColumn(
                name: "match_duration_minutes",
                schema: "dbo",
                table: "tournaments");

            migrationBuilder.DropColumn(
                name: "registration_closes_at",
                schema: "dbo",
                table: "tournaments");

            migrationBuilder.DropColumn(
                name: "registration_opens_at",
                schema: "dbo",
                table: "tournaments");

            migrationBuilder.DropColumn(
                name: "away_penalties",
                schema: "dbo",
                table: "tournament_matches");

            migrationBuilder.DropColumn(
                name: "away_score",
                schema: "dbo",
                table: "tournament_matches");

            migrationBuilder.DropColumn(
                name: "duration_minutes",
                schema: "dbo",
                table: "tournament_matches");

            migrationBuilder.DropColumn(
                name: "home_penalties",
                schema: "dbo",
                table: "tournament_matches");

            migrationBuilder.DropColumn(
                name: "home_score",
                schema: "dbo",
                table: "tournament_matches");

            migrationBuilder.DropColumn(
                name: "starts_at",
                schema: "dbo",
                table: "tournament_matches");

            migrationBuilder.DropColumn(
                name: "status",
                schema: "dbo",
                table: "tournament_matches");
        }
    }
}
