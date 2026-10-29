using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KBS.DAL.Migrations
{
    /// <inheritdoc />
    public partial class AddShopSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ShopSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    Slogan = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Hotline = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    Email = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: true),
                    WarehouseAddress = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    BankName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    BankBin = table.Column<string>(type: "character varying(6)", maxLength: 6, nullable: true),
                    AccountNumber = table.Column<string>(type: "character varying(19)", maxLength: 19, nullable: true),
                    AccountName = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    LogoData = table.Column<byte[]>(type: "bytea", nullable: true),
                    BannerData = table.Column<byte[]>(type: "bytea", nullable: true),
                    LogoContentType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    BannerContentType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShopSettings", x => x.Id);
                    table.CheckConstraint("CK_ShopSettings_Singleton", "\"Id\" = 1");
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ShopSettings");
        }
    }
}
