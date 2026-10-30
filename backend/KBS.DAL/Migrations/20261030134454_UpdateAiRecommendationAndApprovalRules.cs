using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KBS.DAL.Migrations
{
    /// <inheritdoc />
    public partial class UpdateAiRecommendationAndApprovalRules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "AiModules",
                keyColumn: "Id",
                keyValue: "ai_auto",
                column: "Description",
                value: "Tự xác nhận đơn COD dưới 10 triệu; đơn chuyển khoản cần nhân viên xác nhận.");

            migrationBuilder.UpdateData(
                table: "AiModules",
                keyColumn: "Id",
                keyValue: "ai_recommender",
                column: "Description",
                value: "Gợi ý tương đồng với 5 đơn hoàn thành gần nhất; chưa mua sẽ gợi ý ngẫu nhiên.");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "AiModules",
                keyColumn: "Id",
                keyValue: "ai_auto",
                column: "Description",
                value: "Xác nhận đơn chờ dưới 10 triệu; từ 10 triệu cần staff xác nhận.");

            migrationBuilder.UpdateData(
                table: "AiModules",
                keyColumn: "Id",
                keyValue: "ai_recommender",
                column: "Description",
                value: "Gợi ý sản phẩm theo kịch bản mua hàng.");
        }
    }
}
