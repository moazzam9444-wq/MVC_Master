using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KicksMvc.Data.Migrations
{
    /// <inheritdoc />
    public partial class ChallengeDurationPhase5 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("IF DB_NAME()<>N'Kicks' AND DB_NAME() NOT LIKE N'Kicks[_]BookingTest[_]%' THROW 51030, 'Unreviewed migration target.', 1;");
            migrationBuilder.AddColumn<int>(
                name: "duration_minutes",
                schema: "dbo",
                table: "challenges",
                type: "int",
                nullable: false,
                defaultValue: 60);

            migrationBuilder.AddCheckConstraint(
                name: "CK_challenges_duration_minutes",
                schema: "dbo",
                table: "challenges",
                sql: "[duration_minutes] IN (60,120)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("IF EXISTS(SELECT 1 FROM dbo.challenges WITH(TABLOCKX,HOLDLOCK) WHERE duration_minutes<>60) THROW 51031, 'Retained two-hour challenges prevent a safe downgrade.', 1;");
            migrationBuilder.DropCheckConstraint(
                name: "CK_challenges_duration_minutes",
                schema: "dbo",
                table: "challenges");

            migrationBuilder.DropColumn(
                name: "duration_minutes",
                schema: "dbo",
                table: "challenges");
        }
    }
}
