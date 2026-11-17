using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KBS.DAL.Migrations
{
    /// <inheritdoc />
    public partial class AddBankPaymentReconciliation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SePayBankAccountId",
                table: "ShopSettings",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "BankPayments",
                columns: table => new
                {
                    OrderId = table.Column<long>(type: "bigint", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    BankBin = table.Column<string>(type: "text", nullable: false),
                    SePayBankAccountId = table.Column<string>(type: "text", nullable: true),
                    BankName = table.Column<string>(type: "text", nullable: false),
                    AccountNumber = table.Column<string>(type: "text", nullable: false),
                    AccountName = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    TransactionId = table.Column<string>(type: "text", nullable: true),
                    PaidAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BankPayments", x => x.OrderId);
                    table.ForeignKey(
                        name: "FK_BankPayments_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.UpdateData(
                table: "AiModules",
                keyColumn: "Id",
                keyValue: "ai_auto",
                column: "Description",
                value: "Tự xác nhận COD hoặc chuyển khoản đã thanh toán dưới 10 triệu; từ 10 triệu cần người duyệt.");

            migrationBuilder.CreateIndex(
                name: "IX_BankPayments_TransactionId",
                table: "BankPayments",
                column: "TransactionId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BankPayments");

            migrationBuilder.DropColumn(
                name: "SePayBankAccountId",
                table: "ShopSettings");

            migrationBuilder.UpdateData(
                table: "AiModules",
                keyColumn: "Id",
                keyValue: "ai_auto",
                column: "Description",
                value: "Tự xác nhận đơn COD dưới 10 triệu; đơn chuyển khoản cần nhân viên xác nhận.");
        }
    }
}
