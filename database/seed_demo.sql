-- MOCK DATA — dữ liệu mẫu do người dùng yêu cầu, chỉ dành cho development/demo.
BEGIN;

INSERT INTO "Brands" ("Name", "Description", "IsActive", "CreatedAt")
SELECT v.name, v.description, true, NOW()
FROM (VALUES ('Keychron','Bàn phím cơ và custom'), ('Akko','Bàn phím và switch'), ('NuPhy','Low-profile không dây')) AS v(name, description)
WHERE NOT EXISTS (SELECT 1 FROM "Brands" b WHERE lower(b."Name") = lower(v.name));

INSERT INTO "Users" ("Username","Email","PasswordHash","FullName","PhoneNumber","RoleId","IsActive","CreatedAt")
SELECT v.username, v.email, 'AQAAAAIAAYagAAAAEAUXkApn6knwaqbFcJWH4hjVb99w1b1dQ63IKfhBFUdDOGyYIpbexYIMG3ovbSYnSQ==', v.full_name, v.phone, r."Id", true, NOW()
FROM (VALUES ('minh.demo','minh.demo@kbs.local','Nguyễn Minh','0901000001'), ('lan.demo','lan.demo@kbs.local','Trần Lan','0901000002')) AS v(username,email,full_name,phone)
JOIN "Roles" r ON r."Name" = 'Customer'
WHERE NOT EXISTS (SELECT 1 FROM "Users" u WHERE u."Email" = v.email OR u."Username" = v.username OR u."PhoneNumber" = v.phone);

INSERT INTO "Products" ("Name","Price","OriginalPrice","Description","CategoryId","BrandId","SwitchType","Layout","ConnectionType","Keycap","Color","IsHotSwap","IsRGB","IsActive","CreatedAt")
SELECT v.name,v.price,v.original_price,v.description,c."Id",b."Id",v.switch_type,v.layout,v.connection_type,'PBT',v.color,true,true,true,NOW()
FROM (VALUES
 ('Keychron K8 Pro',2490000::numeric,2790000::numeric,'TKL ba chế độ','Mechanical','Keychron','Gateron Brown','TKL','Ba chế độ','Đen'),
 ('Keychron Q1 Max',4690000::numeric,NULL,'Bàn phím nhôm 75%','Mechanical','Keychron','Gateron Jupiter','75%','Ba chế độ','Xanh navy'),
 ('Akko 5075B Plus',1890000::numeric,NULL,'Bàn phím 75%','Mechanical','Akko','Cream Yellow','75%','Ba chế độ','Kem'),
 ('Akko MOD 007B HE',3290000::numeric,3590000::numeric,'Hall Effect 75%','Hall Effect','Akko','Magnetic','75%','Ba chế độ','Đen'),
 ('NuPhy Air75 V2',2990000::numeric,3190000::numeric,'Low-profile 75%','Low-profile','NuPhy','Aloe','75%','Ba chế độ','Xám'),
 ('NuPhy Halo75 V2',3490000::numeric,NULL,'Bàn phím gasket 75%','Mechanical','NuPhy','Mint','75%','Ba chế độ','Trắng')
) AS v(name,price,original_price,description,category_name,brand_name,switch_type,layout,connection_type,color)
JOIN "Categories" c ON c."Name" = v.category_name
JOIN "Brands" b ON b."Name" = v.brand_name
WHERE NOT EXISTS (SELECT 1 FROM "Products" p WHERE p."Name" = v.name);

INSERT INTO "Inventories" ("ProductId","StockQuantity","UpdatedAt")
SELECT p."Id", CASE p."Name" WHEN 'Keychron K8 Pro' THEN 35 WHEN 'Keychron Q1 Max' THEN 4 WHEN 'Akko 5075B Plus' THEN 20 WHEN 'Akko MOD 007B HE' THEN 3 WHEN 'NuPhy Air75 V2' THEN 12 ELSE 6 END, NOW()
FROM "Products" p WHERE p."Name" IN ('Keychron K8 Pro','Keychron Q1 Max','Akko 5075B Plus','Akko MOD 007B HE','NuPhy Air75 V2','NuPhy Halo75 V2')
ON CONFLICT ("ProductId") DO NOTHING;

-- Tồn kho phía trên là số dư demo hiện tại; đơn lịch sử không trừ tồn lần nữa.
-- Chỉ thêm dòng hàng cho đơn vừa tạo; chạy lại không thay đổi đơn đã có.
WITH demo(email, address, product_name, payment_method, payment_status, order_status, age) AS (
    VALUES
    ('minh.demo@kbs.local', '[DEMO-01] Quận 1, TP. Hồ Chí Minh', 'Keychron K8 Pro', 'COD', 'PAID', 'COMPLETED', 6),
    ('lan.demo@kbs.local', '[DEMO-02] Hải Châu, Đà Nẵng', 'Akko 5075B Plus', 'BANKING', 'PAID', 'COMPLETED', 3),
    ('minh.demo@kbs.local', '[DEMO-03] Cầu Giấy, Hà Nội', 'NuPhy Air75 V2', 'COD', 'PENDING', 'PENDING', 1)
), inserted AS (
    INSERT INTO "Orders" (
        "UserId", "CustomerName", "PhoneNumber", "ShippingAddress", "TotalPrice",
        "PaymentMethod", "PaymentStatus", "OrderStatus", "CreatedAt"
    )
    SELECT u."Id", u."FullName", u."PhoneNumber", d.address, p."Price",
        d.payment_method, d.payment_status, d.order_status, NOW() - d.age * INTERVAL '1 day'
    FROM demo d
    JOIN "Users" u ON u."Email" = d.email
    JOIN "Products" p ON p."Name" = d.product_name
    WHERE NOT EXISTS (
        SELECT 1 FROM "Orders" o WHERE o."UserId" = u."Id" AND o."ShippingAddress" = d.address
    )
    RETURNING "Id", "ShippingAddress", "TotalPrice"
)
INSERT INTO "OrderDetails" ("OrderId", "ProductId", "UnitPrice", "Quantity")
SELECT o."Id", p."Id", o."TotalPrice", 1
FROM inserted o
JOIN demo d ON d.address = o."ShippingAddress"
JOIN "Products" p ON p."Name" = d.product_name;

-- Acceptance checks: đủ dữ liệu, FK do DB bảo vệ, tổng tiền khớp chi tiết.
DO $$
BEGIN
    IF (SELECT count(*) FROM "Products" WHERE "Name" IN (
        'Keychron K8 Pro', 'Keychron Q1 Max', 'Akko 5075B Plus',
        'Akko MOD 007B HE', 'NuPhy Air75 V2', 'NuPhy Halo75 V2')) <> 6 THEN
        RAISE EXCEPTION 'Seed requires six unique demo products and their categories/brands';
    END IF;
    IF (SELECT count(*) FROM "Users" WHERE "Email" IN ('minh.demo@kbs.local', 'lan.demo@kbs.local')) <> 2 THEN
        RAISE EXCEPTION 'Demo accounts missing or conflict with an existing username/phone';
    END IF;
    IF (SELECT count(*) FROM "Orders" WHERE "ShippingAddress" LIKE '[DEMO-0%') <> 3 THEN
        RAISE EXCEPTION 'Expected three demo orders';
    END IF;
    IF EXISTS (
        SELECT 1 FROM "Orders" o LEFT JOIN "OrderDetails" d ON d."OrderId" = o."Id"
        WHERE o."ShippingAddress" LIKE '[DEMO-0%'
        GROUP BY o."Id", o."TotalPrice"
        HAVING count(d."Id") = 0 OR sum(d."UnitPrice" * d."Quantity") <> o."TotalPrice"
    ) THEN
        RAISE EXCEPTION 'Demo order totals do not match their lines';
    END IF;
END $$;

COMMIT;
