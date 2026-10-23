using KBS.DAL.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KBS.DAL.Migrations;

[DbContext(typeof(KbsDbContext))]
[Migration("20261020103000_AddStaffRole")]
public partial class AddStaffRole : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Hỗ trợ database đã tạo role thủ công với tên tiếng Việt.
        migrationBuilder.Sql("""
            UPDATE "Roles" SET "Name" = 'Staff', "Description" = 'Nhân viên vận hành'
            WHERE "Name" = 'Nhân viên'
              AND NOT EXISTS (SELECT 1 FROM "Roles" WHERE "Name" = 'Staff');

            INSERT INTO "Roles" ("Id", "Description", "Name")
            SELECT COALESCE(MAX("Id"), 0) + 1, 'Nhân viên vận hành', 'Staff'
            FROM "Roles"
            WHERE NOT EXISTS (SELECT 1 FROM "Roles" WHERE "Name" = 'Staff');
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            "DELETE FROM \"Roles\" WHERE \"Name\" = 'Staff' " +
            "AND \"Description\" = 'Nhân viên vận hành';");
    }
}
