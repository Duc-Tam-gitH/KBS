using KBS.DAL.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace KBS.DAL.Migrations;

[DbContext(typeof(KbsDbContext))]
[Migration("20261118040000_RetireStaffRole")]
public class RetireStaffRole : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            UPDATE "ChatConversations" SET "StaffId" = NULL
            WHERE "StaffId" IN (
                SELECT u."Id" FROM "Users" u JOIN "Roles" r ON r."Id" = u."RoleId"
                WHERE r."Name" IN ('Staff', 'Nhân viên'));
            UPDATE "StaffShifts" SET "EndedAt" = CURRENT_TIMESTAMP
            WHERE "EndedAt" IS NULL AND "StaffId" IN (
                SELECT u."Id" FROM "Users" u JOIN "Roles" r ON r."Id" = u."RoleId"
                WHERE r."Name" IN ('Staff', 'Nhân viên'));
            UPDATE "Users" SET "RoleId" = (SELECT "Id" FROM "Roles" WHERE "Name" = 'Customer')
            WHERE "RoleId" IN (SELECT "Id" FROM "Roles" WHERE "Name" IN ('Staff', 'Nhân viên'));
            DELETE FROM "Roles" WHERE "Name" IN ('Staff', 'Nhân viên');
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        throw new NotSupportedException("Restore a backup to recover former Staff assignments.");
    }
}
