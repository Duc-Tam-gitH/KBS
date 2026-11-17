using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KBS.DAL.Migrations
{
    /// <inheritdoc />
    public partial class AddSePayGateway : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GatewayPayments",
                columns: table => new
                {
                    OrderId = table.Column<long>(type: "bigint", nullable: false),
                    Invoice = table.Column<string>(type: "text", nullable: false),
                    Environment = table.Column<string>(type: "text", nullable: false),
                    MerchantId = table.Column<string>(type: "text", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    TransactionId = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    PaidAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GatewayPayments", x => x.OrderId);
                    table.ForeignKey(
                        name: "FK_GatewayPayments_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GatewayPayments_Invoice",
                table: "GatewayPayments",
                column: "Invoice",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GatewayPayments_TransactionId",
                table: "GatewayPayments",
                column: "TransactionId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GatewayPayments");
        }
    }
}
