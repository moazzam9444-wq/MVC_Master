using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KicksMvc.Data.Migrations
{
    /// <inheritdoc />
    public partial class BookingDurationAndOpeningHours : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF DB_NAME() <> N'Kicks' AND DB_NAME() NOT LIKE N'Kicks[_]BookingTest[_]%'
                    THROW 51020, 'Unreviewed booking migration database. Stop.', 1;
                IF EXISTS (SELECT 1 FROM dbo.bookings WITH (TABLOCKX,HOLDLOCK)
                    WHERE DATEPART(TZOFFSET,starts_at) <> 180
                       OR DATEPART(HOUR,starts_at) NOT BETWEEN 13 AND 22
                       OR DATEPART(MINUTE,starts_at) <> 0 OR DATEPART(SECOND,starts_at) <> 0
                       OR ends_at <> DATEADD(HOUR,1,starts_at))
                    THROW 51021, 'Existing reservations conflict with the new schedule. Preserve them and request a decision.', 1;
                """);
            migrationBuilder.DropCheckConstraint(
                name: "CK_bookings_rule_4",
                schema: "dbo",
                table: "bookings");

            migrationBuilder.AddColumn<int>(
                name: "duration_minutes",
                schema: "dbo",
                table: "bookings",
                type: "int",
                nullable: false,
                defaultValue: 60);

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "ends_at",
                schema: "dbo",
                table: "bookings",
                type: "datetimeoffset(0)",
                nullable: false,
                computedColumnSql: "DATEADD(MINUTE, [duration_minutes], [starts_at])",
                stored: true,
                oldClrType: typeof(DateTimeOffset),
                oldType: "datetimeoffset(0)",
                oldComputedColumnSql: "DATEADD(HOUR, 1, [starts_at])",
                oldStored: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_bookings_closing_time",
                schema: "dbo",
                table: "bookings",
                sql: "[ends_at] <= TODATETIMEOFFSET(DATEADD(HOUR,23,CONVERT(datetime2(0),CONVERT(date,[starts_at]))),180)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_bookings_duration_minutes",
                schema: "dbo",
                table: "bookings",
                sql: "[duration_minutes] IN (60,120)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_bookings_rule_4",
                schema: "dbo",
                table: "bookings",
                sql: "DATEPART(TZOFFSET,[starts_at]) = 180 AND DATEPART(HOUR,[starts_at]) BETWEEN 13 AND 22");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF EXISTS (SELECT 1 FROM dbo.bookings WITH (TABLOCKX,HOLDLOCK) WHERE duration_minutes <> 60)
                    THROW 51022, 'Downgrade would shorten retained two-hour bookings. Use a compatible forward fix.', 1;
                """);
            migrationBuilder.DropCheckConstraint(
                name: "CK_bookings_closing_time",
                schema: "dbo",
                table: "bookings");

            migrationBuilder.DropCheckConstraint(
                name: "CK_bookings_duration_minutes",
                schema: "dbo",
                table: "bookings");

            migrationBuilder.DropCheckConstraint(
                name: "CK_bookings_rule_4",
                schema: "dbo",
                table: "bookings");

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
                oldComputedColumnSql: "DATEADD(MINUTE, [duration_minutes], [starts_at])",
                oldStored: true);

            // ends_at must stop depending on duration before duration can be dropped.
            migrationBuilder.DropColumn(
                name: "duration_minutes",
                schema: "dbo",
                table: "bookings");

            migrationBuilder.AddCheckConstraint(
                name: "CK_bookings_rule_4",
                schema: "dbo",
                table: "bookings",
                sql: "DATEPART(TZOFFSET,[starts_at]) = 180 AND DATEPART(HOUR,[starts_at]) BETWEEN 10 AND 22");
        }
    }
}
