START TRANSACTION;
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

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20261118040000_RetireStaffRole', '9.0.4');

COMMIT;

