using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KBS.DAL.Migrations
{
    /// <inheritdoc />
    public partial class AddProductSoundAndRecommender : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Sound",
                table: "Products",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.UpdateData(
                table: "AiModules",
                keyColumn: "Id",
                keyValue: "ai_recommender",
                column: "IsIntegrated",
                value: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "AiModules",
                keyColumn: "Id",
                keyValue: "ai_recommender",
                column: "IsEnabled",
                value: false);
            migrationBuilder.DropColumn(
                name: "Sound",
                table: "Products");

            migrationBuilder.UpdateData(
                table: "AiModules",
                keyColumn: "Id",
                keyValue: "ai_recommender",
                column: "IsIntegrated",
                value: false);
        }
    }
}
