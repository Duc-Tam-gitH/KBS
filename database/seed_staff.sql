-- Local development account requested by the project owner.
-- Does not overwrite an existing username or email.
INSERT INTO "Users" ("Username", "Email", "PasswordHash", "FullName", "RoleId", "IsActive", "CreatedAt")
SELECT 'staff', 'staff@kbs.local', 'AQAAAAIAAYagAAAAEAxP3dUuIaTbRK7qeAkwEHhrBStPcc2c2lcgxpXPCOmCek8dW9e0dA06q5UUGsqcDQ==', 'Staff', r."Id", true, NOW()
FROM "Roles" r
WHERE r."Name" = 'Staff'
  AND NOT EXISTS (
    SELECT 1 FROM "Users" WHERE "Username" = 'staff' OR "Email" = 'staff@kbs.local'
  )
RETURNING "Id", "Username", "Email", "RoleId", "IsActive";
