using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KicksMvc.Data.Migrations
{
    /// <inheritdoc />
    public partial class StripeSandboxPhase7 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_tournament_registrations_status_enum",
                schema: "dbo",
                table: "tournament_registrations");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "payment_deadline",
                schema: "dbo",
                table: "tournament_registrations",
                type: "datetimeoffset(0)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "gateway_payments",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PayerId = table.Column<int>(type: "int", nullable: false),
                    BookingId = table.Column<int>(type: "int", nullable: true),
                    RegistrationId = table.Column<int>(type: "int", nullable: true),
                    AmountJod = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    UsdPerJod = table.Column<decimal>(type: "decimal(12,6)", precision: 12, scale: 6, nullable: false),
                    AmountUsdCents = table.Column<long>(type: "bigint", nullable: false),
                    Currency = table.Column<string>(type: "varchar(3)", unicode: false, maxLength: 3, nullable: false),
                    Status = table.Column<string>(type: "varchar(16)", unicode: false, maxLength: 16, nullable: false),
                    HoldExpiresAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    PaidAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    FinancialReview = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_gateway_payments", x => x.Id);
                    table.CheckConstraint("CK_gateway_amount", "[AmountJod]>0 AND [UsdPerJod]>0 AND [AmountUsdCents] BETWEEN 50 AND 99999999 AND [Currency]='usd'");
                    table.CheckConstraint("CK_gateway_status", "[Status] IN ('Pending','Paid','Expired','Cancelled')");
                    table.CheckConstraint("CK_gateway_target", "CASE WHEN [BookingId] IS NULL THEN 0 ELSE 1 END + CASE WHEN [RegistrationId] IS NULL THEN 0 ELSE 1 END = 1");
                    table.ForeignKey(
                        name: "FK_gateway_payments_bookings_BookingId",
                        column: x => x.BookingId,
                        principalSchema: "dbo",
                        principalTable: "bookings",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_gateway_payments_tournament_registrations_RegistrationId",
                        column: x => x.RegistrationId,
                        principalSchema: "dbo",
                        principalTable: "tournament_registrations",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_gateway_payments_users_PayerId",
                        column: x => x.PayerId,
                        principalSchema: "dbo",
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "stripe_webhook_events",
                schema: "dbo",
                columns: table => new
                {
                    EventId = table.Column<string>(type: "varchar(255)", unicode: false, maxLength: 255, nullable: false),
                    EventType = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false),
                    ProcessedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Outcome = table.Column<string>(type: "varchar(40)", unicode: false, maxLength: 40, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_stripe_webhook_events", x => x.EventId);
                });

            migrationBuilder.CreateTable(
                name: "stripe_checkout_attempts",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PaymentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SessionId = table.Column<string>(type: "varchar(255)", unicode: false, maxLength: 255, nullable: true),
                    PaymentIntentId = table.Column<string>(type: "varchar(255)", unicode: false, maxLength: 255, nullable: true),
                    Status = table.Column<string>(type: "varchar(16)", unicode: false, maxLength: 16, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_stripe_checkout_attempts", x => x.Id);
                    table.CheckConstraint("CK_checkout_status", "[Status] IN ('Creating','Open','Paid','Expired')");
                    table.ForeignKey(
                        name: "FK_stripe_checkout_attempts_gateway_payments_PaymentId",
                        column: x => x.PaymentId,
                        principalSchema: "dbo",
                        principalTable: "gateway_payments",
                        principalColumn: "Id");
                });

            migrationBuilder.AddCheckConstraint(
                name: "CK_tournament_registrations_status_enum",
                schema: "dbo",
                table: "tournament_registrations",
                sql: "[status] IN (N'Registered', N'Withdrawn', N'Pending')");

            migrationBuilder.CreateIndex(
                name: "IX_gateway_payments_BookingId",
                schema: "dbo",
                table: "gateway_payments",
                column: "BookingId",
                unique: true,
                filter: "[BookingId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_gateway_payments_PayerId",
                schema: "dbo",
                table: "gateway_payments",
                column: "PayerId");

            migrationBuilder.CreateIndex(
                name: "IX_gateway_payments_RegistrationId",
                schema: "dbo",
                table: "gateway_payments",
                column: "RegistrationId",
                unique: true,
                filter: "[RegistrationId] IS NOT NULL AND [Status] = 'Pending'");

            migrationBuilder.CreateIndex(
                name: "IX_gateway_payments_Status_HoldExpiresAt",
                schema: "dbo",
                table: "gateway_payments",
                columns: new[] { "Status", "HoldExpiresAt" });

            migrationBuilder.CreateIndex(
                name: "IX_stripe_checkout_attempts_PaymentId",
                schema: "dbo",
                table: "stripe_checkout_attempts",
                column: "PaymentId");

            migrationBuilder.CreateIndex(
                name: "IX_stripe_checkout_attempts_PaymentIntentId",
                schema: "dbo",
                table: "stripe_checkout_attempts",
                column: "PaymentIntentId",
                unique: true,
                filter: "[PaymentIntentId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_stripe_checkout_attempts_SessionId",
                schema: "dbo",
                table: "stripe_checkout_attempts",
                column: "SessionId",
                unique: true,
                filter: "[SessionId] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Never silently erase financial history or pending entry information.
            migrationBuilder.Sql("""
                IF EXISTS(SELECT 1 FROM dbo.gateway_payments)
                  OR EXISTS(SELECT 1 FROM dbo.stripe_checkout_attempts)
                  OR EXISTS(SELECT 1 FROM dbo.stripe_webhook_events)
                  OR EXISTS(SELECT 1 FROM dbo.tournament_registrations WHERE payment_deadline IS NOT NULL OR status=N'Pending')
                    THROW 51071,'Stripe sandbox history exists. Preserve it and use a reviewed forward fix.',1;
                """);
            migrationBuilder.DropTable(
                name: "stripe_checkout_attempts",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "stripe_webhook_events",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "gateway_payments",
                schema: "dbo");

            migrationBuilder.DropCheckConstraint(
                name: "CK_tournament_registrations_status_enum",
                schema: "dbo",
                table: "tournament_registrations");

            migrationBuilder.DropColumn(
                name: "payment_deadline",
                schema: "dbo",
                table: "tournament_registrations");

            migrationBuilder.AddCheckConstraint(
                name: "CK_tournament_registrations_status_enum",
                schema: "dbo",
                table: "tournament_registrations",
                sql: "[status] IN (N'Registered', N'Withdrawn')");
        }
    }
}
