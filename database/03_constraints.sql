-- FIX: Bảo đảm psql trên Windows đọc các chú thích tiếng Việt theo UTF-8.
SET client_encoding = 'UTF8';

-- Các ràng buộc được thêm có điều kiện để script có thể chạy lặp lại.
DO $$
DECLARE
    r record;
BEGIN
    FOR r IN
        SELECT * FROM (VALUES
            ('Categories', 'UQ_Categories_Name', 'UNIQUE ("Name")'),
            ('Brands', 'UQ_Brands_Name', 'UNIQUE ("Name")'),
            ('Products', 'CK_Products_Price', 'CHECK ("Price" >= 0)'),
            ('Products', 'CK_Products_OriginalPrice', 'CHECK ("OriginalPrice" IS NULL OR "OriginalPrice" >= 0)'),
            ('Inventories', 'UQ_Inventories_ProductId', 'UNIQUE ("ProductId")'),
            ('Inventories', 'CK_Inventories_StockQuantity', 'CHECK ("StockQuantity" >= 0)'),
            ('Roles', 'UQ_Roles_Name', 'UNIQUE ("Name")'),
            ('Users', 'UQ_Users_Username', 'UNIQUE ("Username")'),
            ('Users', 'UQ_Users_Email', 'UNIQUE ("Email")'),
            ('Orders', 'CK_Orders_PaymentMethod', 'CHECK ("PaymentMethod" IN (''COD'', ''BANK_TRANSFER''))'),
            ('Orders', 'CK_Orders_PaymentStatus', 'CHECK ("PaymentStatus" IN (''PENDING'', ''PAID''))'),
            ('Orders', 'CK_Orders_OrderStatus', 'CHECK ("OrderStatus" IN (''PENDING'', ''CONFIRMED'', ''SHIPPED'', ''COMPLETED'', ''CANCELLED''))'),
            ('OrderDetails', 'CK_OrderDetails_Quantity', 'CHECK ("Quantity" > 0)')
        ) AS c(table_name, constraint_name, definition)
    LOOP
        -- FIX: to_regclass trả về NULL an toàn khi bảng chưa tồn tại.
        IF to_regclass(format('%I.%I', 'public', r.table_name)) IS NOT NULL
           AND NOT EXISTS (
            SELECT 1 FROM pg_constraint
            WHERE conname = r.constraint_name
              AND conrelid = to_regclass(format('%I.%I', 'public', r.table_name))
        ) THEN
            EXECUTE format('ALTER TABLE %I.%I ADD CONSTRAINT %I %s',
                'public', r.table_name, r.constraint_name, r.definition);
        END IF;
    END LOOP;
END $$;

-- FIX: Bổ sung ràng buộc tên sản phẩm cho database đã được tạo từ phiên bản cũ.
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1
        FROM pg_constraint c
        JOIN pg_attribute a
          ON a.attrelid = c.conrelid
         AND a.attnum = c.conkey[1]
        WHERE c.conrelid = to_regclass('public."Products"')
          AND c.contype = 'u'
          AND array_length(c.conkey, 1) = 1
          AND a.attname = 'Name'
    ) THEN
        ALTER TABLE "Products"
        ADD CONSTRAINT "UQ_Products_Name" UNIQUE ("Name");
    END IF;
END $$;

DO $$
DECLARE
    r record;
BEGIN
    FOR r IN
        SELECT * FROM (VALUES
            -- FIX: Chặn xóa danh mục hoặc thương hiệu còn được sản phẩm tham chiếu.
            ('Products', 'FK_Products_Categories', 'FOREIGN KEY ("CategoryId") REFERENCES "Categories" ("Id") ON DELETE RESTRICT'),
            ('Products', 'FK_Products_Brands', 'FOREIGN KEY ("BrandId") REFERENCES "Brands" ("Id") ON DELETE RESTRICT'),
            ('Inventories', 'FK_Inventories_Products', 'FOREIGN KEY ("ProductId") REFERENCES "Products" ("Id") ON DELETE CASCADE'),
            ('Users', 'FK_Users_Roles', 'FOREIGN KEY ("RoleId") REFERENCES "Roles" ("Id")'),
            -- FIX: Chặn xóa người dùng còn có đơn hàng.
            ('Orders', 'FK_Orders_Users', 'FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE RESTRICT'),
            ('OrderDetails', 'FK_OrderDetails_Orders', 'FOREIGN KEY ("OrderId") REFERENCES "Orders" ("Id") ON DELETE CASCADE'),
            ('OrderDetails', 'FK_OrderDetails_Products', 'FOREIGN KEY ("ProductId") REFERENCES "Products" ("Id")')
        ) AS c(table_name, constraint_name, definition)
    LOOP
        -- FIX: to_regclass trả về NULL an toàn khi bảng chưa tồn tại.
        IF to_regclass(format('%I.%I', 'public', r.table_name)) IS NOT NULL
           AND NOT EXISTS (
            SELECT 1 FROM pg_constraint
            WHERE conname = r.constraint_name
              AND conrelid = to_regclass(format('%I.%I', 'public', r.table_name))
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
