using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;
using KBS.DAL.Data;

#nullable disable

namespace KBS.DAL.Migrations;

[DbContext(typeof(KbsDbContext))]
[Migration("20261019100000_AddUniquePhoneNumber")]
public partial class AddUniquePhoneNumber : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateIndex(
            name: "IX_Users_PhoneNumber",
            table: "Users",
            column: "PhoneNumber",
            unique: true,
            filter: "\"PhoneNumber\" IS NOT NULL");
    }

    protected override void Down(MigrationBuilder migrationBuilder) =>
        migrationBuilder.DropIndex(name: "IX_Users_PhoneNumber", table: "Users");
}
