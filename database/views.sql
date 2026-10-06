CREATE OR REPLACE VIEW "vw_ProductFull" AS
SELECT p."Id" AS "ProductId", p."Name" AS "ProductName", p."Price", p."OriginalPrice",
       p."Description", c."Name" AS "CategoryName", b."Name" AS "BrandName",
       p."SwitchType", p."Layout", p."ConnectionType", p."Keycap", p."Color",
       p."IsHotSwap", p."IsRGB", COALESCE(i."StockQuantity", 0) AS "StockQuantity",
       p."CreatedAt"
FROM "Products" p
JOIN "Categories" c ON c."Id" = p."CategoryId"
JOIN "Brands" b ON b."Id" = p."BrandId"
LEFT JOIN "Inventories" i ON i."ProductId" = p."Id"
WHERE p."IsActive" = true;

CREATE OR REPLACE VIEW "vw_OrderSummary" AS
SELECT o."Id" AS "OrderId", o."CreatedAt", u."Username", o."CustomerName",
       o."PhoneNumber", o."ShippingAddress", o."PaymentMethod", o."PaymentStatus",
       o."OrderStatus", COUNT(d."Id") AS "ItemCount", o."TotalPrice"
FROM "Orders" o
JOIN "Users" u ON u."Id" = o."UserId"
LEFT JOIN "OrderDetails" d ON d."OrderId" = o."Id"
-- FIX: Liệt kê đầy đủ các cột không tổng hợp để không phụ thuộc hàm ý khóa của PostgreSQL.
GROUP BY o."Id", o."CreatedAt", u."Username", o."CustomerName", o."PhoneNumber",
         o."ShippingAddress", o."PaymentMethod", o."PaymentStatus", o."OrderStatus",
         o."TotalPrice";

CREATE OR REPLACE VIEW "vw_RevenueByDay" AS
SELECT o."CreatedAt"::date AS "OrderDate", COUNT(*) AS "OrderCount",
       SUM(o."TotalPrice") AS "Revenue"
FROM "Orders" o
WHERE o."PaymentStatus" = 'PAID' AND o."OrderStatus" <> 'CANCELLED'
GROUP BY o."CreatedAt"::date;

CREATE OR REPLACE VIEW "vw_RevenueByMonth" AS
SELECT date_trunc('month', o."CreatedAt")::date AS "Month",
       COUNT(*) AS "OrderCount", SUM(o."TotalPrice") AS "Revenue"
FROM "Orders" o
WHERE o."PaymentStatus" = 'PAID' AND o."OrderStatus" <> 'CANCELLED'
GROUP BY date_trunc('month', o."CreatedAt")::date;

-- FIX: Người dùng phải áp dụng ORDER BY khi truy vấn view để bảo đảm thứ tự kết quả.
CREATE OR REPLACE VIEW "vw_TopSellingProducts" AS
SELECT p."Id" AS "ProductId", p."Name" AS "ProductName", b."Name" AS "BrandName",
       SUM(d."Quantity") AS "TotalQuantity", SUM(d."Quantity" * d."UnitPrice") AS "TotalRevenue"
FROM "OrderDetails" d
JOIN "Orders" o ON o."Id" = d."OrderId"
JOIN "Products" p ON p."Id" = d."ProductId"
JOIN "Brands" b ON b."Id" = p."BrandId"
WHERE o."OrderStatus" <> 'CANCELLED'
GROUP BY p."Id", p."Name", b."Name";

-- FIX: Người dùng phải áp dụng ORDER BY khi truy vấn view để bảo đảm thứ tự kết quả.
CREATE OR REPLACE VIEW "vw_LowStockProducts" AS
SELECT p."Id" AS "ProductId", p."Name" AS "ProductName", b."Name" AS "BrandName",
       i."StockQuantity", p."Price"
FROM "Inventories" i
JOIN "Products" p ON p."Id" = i."ProductId"
JOIN "Brands" b ON b."Id" = p."BrandId"
WHERE i."StockQuantity" < 10;

CREATE OR REPLACE VIEW "vw_ProductByBrand" AS
SELECT b."Id" AS "BrandId", b."Name" AS "BrandName", COUNT(p."Id") AS "ProductCount",
       AVG(p."Price") AS "AveragePrice"
FROM "Brands" b
LEFT JOIN "Products" p ON p."BrandId" = b."Id" AND p."IsActive" = true
GROUP BY b."Id", b."Name";
