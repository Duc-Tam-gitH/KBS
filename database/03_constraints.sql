-- Constraints are added conditionally so the script is safe to re-run.
DO $$
DECLARE
    r record;
BEGIN
    FOR r IN
        SELECT * FROM (VALUES
            ('Categories', 'PK_Categories', 'PRIMARY KEY ("Id")'),
            ('Categories', 'UQ_Categories_Name', 'UNIQUE ("Name")'),
            ('Brands', 'PK_Brands', 'PRIMARY KEY ("Id")'),
            ('Brands', 'UQ_Brands_Name', 'UNIQUE ("Name")'),
            ('Products', 'PK_Products', 'PRIMARY KEY ("Id")'),
            ('Products', 'CK_Products_Price', 'CHECK ("Price" >= 0)'),
            ('Products', 'CK_Products_OriginalPrice', 'CHECK ("OriginalPrice" IS NULL OR "OriginalPrice" >= 0)'),
            ('Inventories', 'PK_Inventories', 'PRIMARY KEY ("Id")'),
            ('Inventories', 'UQ_Inventories_ProductId', 'UNIQUE ("ProductId")'),
            ('Inventories', 'CK_Inventories_StockQuantity', 'CHECK ("StockQuantity" >= 0)'),
            ('Roles', 'PK_Roles', 'PRIMARY KEY ("Id")'),
            ('Roles', 'UQ_Roles_Name', 'UNIQUE ("Name")'),
            ('Users', 'PK_Users', 'PRIMARY KEY ("Id")'),
            ('Users', 'UQ_Users_Username', 'UNIQUE ("Username")'),
            ('Users', 'UQ_Users_Email', 'UNIQUE ("Email")'),
            ('Orders', 'PK_Orders', 'PRIMARY KEY ("Id")'),
            ('Orders', 'CK_Orders_PaymentMethod', 'CHECK ("PaymentMethod" IN (''COD'', ''BANK_TRANSFER''))'),
            ('Orders', 'CK_Orders_PaymentStatus', 'CHECK ("PaymentStatus" IN (''PENDING'', ''PAID''))'),
            ('Orders', 'CK_Orders_OrderStatus', 'CHECK ("OrderStatus" IN (''PENDING'', ''CONFIRMED'', ''SHIPPED'', ''COMPLETED'', ''CANCELLED''))'),
            ('OrderDetails', 'PK_OrderDetails', 'PRIMARY KEY ("Id")'),
            ('OrderDetails', 'CK_OrderDetails_Quantity', 'CHECK ("Quantity" > 0)')
        ) AS c(table_name, constraint_name, definition)
    LOOP
        IF NOT EXISTS (
            SELECT 1 FROM pg_constraint
            WHERE conname = r.constraint_name
              AND conrelid = format('%I.%I', 'public', r.table_name)::regclass
        ) THEN
            EXECUTE format('ALTER TABLE %I.%I ADD CONSTRAINT %I %s',
                'public', r.table_name, r.constraint_name, r.definition);
        END IF;
    END LOOP;
END $$;

DO $$
DECLARE
    r record;
BEGIN
    FOR r IN
        SELECT * FROM (VALUES
            ('Products', 'FK_Products_Categories', 'FOREIGN KEY ("CategoryId") REFERENCES "Categories" ("Id")'),
            ('Products', 'FK_Products_Brands', 'FOREIGN KEY ("BrandId") REFERENCES "Brands" ("Id")'),
            ('Inventories', 'FK_Inventories_Products', 'FOREIGN KEY ("ProductId") REFERENCES "Products" ("Id") ON DELETE CASCADE'),
            ('Users', 'FK_Users_Roles', 'FOREIGN KEY ("RoleId") REFERENCES "Roles" ("Id")'),
            ('Orders', 'FK_Orders_Users', 'FOREIGN KEY ("UserId") REFERENCES "Users" ("Id")'),
            ('OrderDetails', 'FK_OrderDetails_Orders', 'FOREIGN KEY ("OrderId") REFERENCES "Orders" ("Id") ON DELETE CASCADE'),
            ('OrderDetails', 'FK_OrderDetails_Products', 'FOREIGN KEY ("ProductId") REFERENCES "Products" ("Id")')
        ) AS c(table_name, constraint_name, definition)
    LOOP
        IF NOT EXISTS (
            SELECT 1 FROM pg_constraint
            WHERE conname = r.constraint_name
              AND conrelid = format('%I.%I', 'public', r.table_name)::regclass
        ) THEN
            EXECUTE format('ALTER TABLE %I.%I ADD CONSTRAINT %I %s',
                'public', r.table_name, r.constraint_name, r.definition);
        END IF;
    END LOOP;
END $$;

CREATE INDEX IF NOT EXISTS "IX_Products_CategoryId" ON "Products" ("CategoryId");
CREATE INDEX IF NOT EXISTS "IX_Products_BrandId" ON "Products" ("BrandId");
CREATE INDEX IF NOT EXISTS "IX_Orders_UserId" ON "Orders" ("UserId");
CREATE INDEX IF NOT EXISTS "IX_Orders_CreatedAt" ON "Orders" ("CreatedAt");
CREATE INDEX IF NOT EXISTS "IX_Orders_OrderStatus" ON "Orders" ("OrderStatus");
CREATE INDEX IF NOT EXISTS "IX_OrderDetails_OrderId" ON "OrderDetails" ("OrderId");
CREATE INDEX IF NOT EXISTS "IX_OrderDetails_ProductId" ON "OrderDetails" ("ProductId");
CREATE INDEX IF NOT EXISTS "IX_Users_Email" ON "Users" ("Email");
CREATE INDEX IF NOT EXISTS "IX_Users_Username" ON "Users" ("Username");
