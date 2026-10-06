-- 1. Kiểm tra số lượng và danh sách bảng
SELECT table_name 
FROM information_schema.tables 
WHERE table_schema = 'public' AND table_type = 'BASE TABLE';

-- 2. Kiểm tra số lượng dòng (Seed Data) của từng bảng
SELECT 'Products' AS table_name, COUNT(*) FROM "Products"
UNION ALL SELECT 'Brands', COUNT(*) FROM "Brands"
UNION ALL SELECT 'Categories', COUNT(*) FROM "Categories"
UNION ALL SELECT 'Orders', COUNT(*) FROM "Orders"
UNION ALL SELECT 'OrderDetails', COUNT(*) FROM "OrderDetails"
UNION ALL SELECT 'Inventories', COUNT(*) FROM "Inventories"
UNION ALL SELECT 'Users', COUNT(*) FROM "Users"
UNION ALL SELECT 'Roles', COUNT(*) FROM "Roles";

SELECT "MigrationId" FROM "__EFMigrationsHistory";

-- Kiểm tra số lượng Views (kỳ vọng: 7)
SELECT COUNT(*) AS total_views 
FROM information_schema.views 
WHERE table_schema = 'public';

-- Kiểm tra số lượng Routines (Functions & Procedures, kỳ vọng: 14)
SELECT COUNT(*) AS total_routines 
FROM information_schema.routines 
WHERE routine_schema = 'public';

-- Kiểm tra số lượng Triggers (kỳ vọng: 4)
SELECT COUNT(*) AS total_triggers 
FROM information_schema.triggers 
WHERE trigger_schema = 'public';

-- Kiểm tra cột Country trong bảng Brands (kỳ vọng: 0 / Không tồn tại)
SELECT COUNT(*) AS country_column_exists
FROM information_schema.columns 
WHERE table_name = 'Brands' AND column_name = 'Country';