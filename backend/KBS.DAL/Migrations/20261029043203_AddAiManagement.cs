using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace KBS.DAL.Migrations
{
    /// <inheritdoc />
    public partial class AddAiManagement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AiModules",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    SourceFile = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    IsIntegrated = table.Column<bool>(type: "boolean", nullable: false),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    ApprovalLimit = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    LastHeartbeatAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AiModules", x => x.Id);
                    table.CheckConstraint("CK_AiModules_Enabled", "NOT \"IsEnabled\" OR \"IsIntegrated\"");
                    table.CheckConstraint("CK_AiModules_Limit", "\"ApprovalLimit\" IS NULL OR \"ApprovalLimit\" > 0");
                });

            migrationBuilder.CreateTable(
                name: "AiEvents",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ModuleId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    OrderId = table.Column<long>(type: "bigint", nullable: true),
                    ActorId = table.Column<long>(type: "bigint", nullable: true),
                    Kind = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Message = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AiEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AiEvents_AiModules_ModuleId",
                        column: x => x.ModuleId,
                        principalTable: "AiModules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AiEvents_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AiEvents_Users_ActorId",
                        column: x => x.ActorId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "AiModules",
                columns: new[] { "Id", "ApprovalLimit", "Description", "IsEnabled", "IsIntegrated", "LastHeartbeatAt", "Name", "SourceFile", "UpdatedAt" },
                values: new object[,]
                {
                    { "ai_anti", null, "Phát hiện mua trên 100 sản phẩm trong 24 giờ.", false, false, null, "Phát hiện mua gom", "ai_anti.py", new DateTimeOffset(new DateTime(2026, 10, 29, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { "ai_auto", 10000000m, "Xác nhận đơn chờ dưới 10 triệu; từ 10 triệu cần staff xác nhận.", false, true, null, "Tự động xác nhận đơn", "ai_auto.py", new DateTimeOffset(new DateTime(2026, 10, 29, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { "ai_recommender", null, "Gợi ý sản phẩm theo kịch bản mua hàng.", false, false, null, "Gợi ý sản phẩm", "ai_recommender.py", new DateTimeOffset(new DateTime(2026, 10, 29, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { "ai_reviewnpl", null, "Lọc từ khóa và phát hiện spam đánh giá.", false, false, null, "Kiểm tra đánh giá", "ai_reviewnpl.py", new DateTimeOffset(new DateTime(2026, 10, 29, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { "ai_security", null, "Phát hiện giá trị đơn hàng bất thường.", false, false, null, "Giám sát đơn hàng", "ai_security.py", new DateTimeOffset(new DateTime(2026, 10, 29, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) }
                });

            migrationBuilder.CreateIndex(
                name: "IX_AiEvents_ActorId",
                table: "AiEvents",
                column: "ActorId");

            migrationBuilder.CreateIndex(
                name: "IX_AiEvents_ModuleId_Id",
                table: "AiEvents",
                columns: new[] { "ModuleId", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_AiEvents_ModuleId_OrderId_Kind",
                table: "AiEvents",
                columns: new[] { "ModuleId", "OrderId", "Kind" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AiEvents_OrderId",
                table: "AiEvents",
                column: "OrderId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AiEvents");

            migrationBuilder.DropTable(
                name: "AiModules");
        }
    }
}
