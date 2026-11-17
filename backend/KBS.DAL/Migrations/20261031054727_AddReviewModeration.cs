using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KBS.DAL.Migrations
{
    /// <inheritdoc />
    public partial class AddReviewModeration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CheckedAt",
                table: "ProductReviews",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ModerationReason",
                table: "ProductReviews",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ModerationStatus",
                table: "ProductReviews",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "PENDING");

            migrationBuilder.UpdateData(
                table: "AiModules",
                keyColumn: "Id",
                keyValue: "ai_reviewnpl",
                column: "IsIntegrated",
                value: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductReviews_ModerationStatus_Id",
                table: "ProductReviews",
                columns: new[] { "ModerationStatus", "Id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "AiModules",
                keyColumn: "Id",
                keyValue: "ai_reviewnpl",
                column: "IsEnabled",
                value: false);
            migrationBuilder.DropIndex(
                name: "IX_ProductReviews_ModerationStatus_Id",
                table: "ProductReviews");

            migrationBuilder.DropColumn(
                name: "CheckedAt",
                table: "ProductReviews");

            migrationBuilder.DropColumn(
                name: "ModerationReason",
                table: "ProductReviews");

            migrationBuilder.DropColumn(
                name: "ModerationStatus",
                table: "ProductReviews");

            migrationBuilder.UpdateData(
                table: "AiModules",
                keyColumn: "Id",
                keyValue: "ai_reviewnpl",
                column: "IsIntegrated",
                value: false);
        }
    }
}
