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

CREATE TEMP TABLE seed_demo_products (
    name text PRIMARY KEY,
    price numeric NOT NULL,
    original_price numeric,
    description text NOT NULL,
    category_name text NOT NULL,
    brand_name text NOT NULL,
    switch_type text NOT NULL,
    layout text NOT NULL,
    connection_type text NOT NULL,
    color text NOT NULL,
    stock_quantity integer NOT NULL
) ON COMMIT DROP;

INSERT INTO seed_demo_products
    (name, price, original_price, description, category_name, brand_name,
     switch_type, layout, connection_type, color, stock_quantity)
VALUES
 ('Keychron K8 Pro',2490000,2790000,'TKL ba chế độ','Mechanical','Keychron','Gateron Brown','TKL','Ba chế độ','Đen',35),
 ('Keychron Q1 Max',4690000,NULL,'Bàn phím nhôm 75%','Mechanical','Keychron','Gateron Jupiter','75%','Ba chế độ','Xanh navy',4),
 ('Akko 5075B Plus',1890000,NULL,'Bàn phím 75%','Mechanical','Akko','Cream Yellow','75%','Ba chế độ','Kem',20),
 ('Akko MOD 007B HE',3290000,3590000,'Hall Effect 75%','Hall Effect','Akko','Magnetic','75%','Ba chế độ','Đen',3),
 ('NuPhy Air75 V2',2990000,3190000,'Low-profile 75%','Low-profile','NuPhy','Aloe','75%','Ba chế độ','Xám',12),
 ('NuPhy Halo75 V2',3490000,NULL,'Bàn phím gasket 75%','Mechanical','NuPhy','Mint','75%','Ba chế độ','Trắng',6),
 ('Keychron C3 Pro',990000,NULL,'Bàn phím cơ TKL có dây','Mechanical','Keychron','Red','TKL','USB-C','Đen',18),
 ('Keychron K2 Pro',2290000,NULL,'Bàn phím cơ nhỏ gọn 75%','Mechanical','Keychron','Gateron Brown','75%','Ba chế độ','Xám',14),
 ('Keychron V1 Max',2590000,NULL,'Bàn phím cơ gasket 75%','Mechanical','Keychron','Gateron Red','75%','Ba chế độ','Đen',11),
 ('Keychron V2 Max',2590000,NULL,'Bàn phím cơ layout 65%','Mechanical','Keychron','Gateron Brown','65%','Ba chế độ','Trắng',9),
 ('Keychron K4 Pro',2690000,NULL,'Bàn phím cơ layout 96%','Mechanical','Keychron','Gateron Red','96%','Ba chế độ','Xám',8),
 ('Keychron K6 Pro',2190000,NULL,'Bàn phím cơ nhỏ gọn 65%','Mechanical','Keychron','Gateron Brown','65%','Ba chế độ','Đen',13),
 ('Keychron K10 Pro',2790000,NULL,'Bàn phím cơ full-size','Mechanical','Keychron','Gateron Red','Full-size','Ba chế độ','Xám',7),
 ('Keychron Q2 Max',4590000,NULL,'Bàn phím nhôm gasket 65%','Mechanical','Keychron','Gateron Jupiter','65%','Ba chế độ','Đen',5),
 ('Keychron Q3 Max',4790000,NULL,'Bàn phím nhôm gasket TKL','Mechanical','Keychron','Gateron Jupiter','TKL','Ba chế độ','Xám',4),
 ('Keychron Q5 Max',4990000,NULL,'Bàn phím nhôm gasket 96%','Mechanical','Keychron','Gateron Jupiter','96%','Ba chế độ','Xanh navy',4),
 ('Keychron Q6 Max',5190000,NULL,'Bàn phím nhôm gasket full-size','Mechanical','Keychron','Gateron Jupiter','Full-size','Ba chế độ','Đen',3),
 ('Keychron Q8 Max',4890000,NULL,'Bàn phím nhôm layout Alice','Mechanical','Keychron','Gateron Jupiter','Alice','Ba chế độ','Xám',3),
 ('Keychron Q10 Max',5190000,NULL,'Bàn phím nhôm layout Alice 75%','Mechanical','Keychron','Gateron Jupiter','Alice 75%','Ba chế độ','Đỏ burgundy',3),
 ('Keychron Q1 HE',5290000,NULL,'Bàn phím Hall Effect 75%','Hall Effect','Keychron','Magnetic','75%','Ba chế độ','Đen',5),
 ('Keychron K1 Pro',2390000,NULL,'Bàn phím cơ low-profile TKL','Low-profile','Keychron','Low-profile Red','TKL','Ba chế độ','Xám',8),
 ('Keychron K3 Max',2490000,NULL,'Bàn phím cơ low-profile 75%','Low-profile','Keychron','Low-profile Brown','75%','Ba chế độ','Trắng',9),
 ('Keychron K7 Max',2590000,NULL,'Bàn phím cơ low-profile 65%','Low-profile','Keychron','Low-profile Red','65%','Ba chế độ','Đen',6),
 ('Akko 3068B Plus',1590000,NULL,'Bàn phím cơ layout 65%','Mechanical','Akko','Cream Blue','65%','Ba chế độ','Trắng',16),
 ('Akko 3084B Plus',1790000,NULL,'Bàn phím cơ layout 75%','Mechanical','Akko','Cream Yellow','75%','Ba chế độ','Xanh pastel',12),
 ('Akko 3098B Plus',1990000,NULL,'Bàn phím cơ layout 96%','Mechanical','Akko','Cream Blue','96%','Ba chế độ','Hồng',10),
 ('Akko 5075S',1490000,NULL,'Bàn phím cơ layout 75% có dây','Mechanical','Akko','Cream Yellow','75%','USB-C','Đen',15),
 ('Akko 5087S',1590000,NULL,'Bàn phím cơ TKL có dây','Mechanical','Akko','Cream Blue','TKL','USB-C','Trắng',10),
 ('Akko 5108S',1890000,NULL,'Bàn phím cơ full-size có dây','Mechanical','Akko','Cream Yellow','Full-size','USB-C','Đen',8),
 ('Akko MOD 007 PC',2790000,NULL,'Bàn phím cơ gasket 75%','Mechanical','Akko','Cream Blue','75%','USB-C','Trong suốt',7),
 ('Akko MOD 008',2990000,NULL,'Bàn phím cơ nhôm layout 65%','Mechanical','Akko','Cream Yellow','65%','USB-C','Bạc',5),
 ('Akko MOD 009',3190000,NULL,'Bàn phím cơ nhôm layout TKL','Mechanical','Akko','Cream Blue','TKL','USB-C','Đen',5),
 ('Akko MOD 007 HE V2',3490000,NULL,'Bàn phím Hall Effect 75%','Hall Effect','Akko','Magnetic','75%','USB-C','Trắng',6),
 ('Akko SPR 75',3290000,NULL,'Bàn phím cơ gasket 75%','Mechanical','Akko','Cream Yellow','75%','USB-C','Xanh navy',4),
 ('Akko MU01',2890000,NULL,'Bàn phím cơ layout 65% vỏ gỗ','Mechanical','Akko','Cream Blue','65%','Ba chế độ','Gỗ walnut',4),
 ('Akko 5075B Plus ISO',1990000,NULL,'Bàn phím cơ 75% chuẩn ISO','Mechanical','Akko','Cream Yellow','75% ISO','Ba chế độ','Kem',7),
 ('Akko 5075S VIA',1690000,NULL,'Bàn phím cơ 75% hỗ trợ VIA','Mechanical','Akko','Cream Blue','75%','USB-C','Xám',8),
 ('Akko 3087 RF',1390000,NULL,'Bàn phím cơ TKL không dây','Mechanical','Akko','Cream Red','TKL','2.4G và USB-C','Đen',9),
 ('Akko 3108 RF',1590000,NULL,'Bàn phím cơ full-size không dây','Mechanical','Akko','Cream Blue','Full-size','2.4G và USB-C','Trắng',6),
 ('NuPhy Air60 V2',2690000,NULL,'Bàn phím cơ low-profile 60%','Low-profile','NuPhy','Aloe','60%','Ba chế độ','Xám',10),
 ('NuPhy Air96 V2',3290000,NULL,'Bàn phím cơ low-profile 96%','Low-profile','NuPhy','Wisteria','96%','Ba chế độ','Trắng',7),
 ('NuPhy Halo65',3190000,NULL,'Bàn phím cơ gasket 65%','Mechanical','NuPhy','Rose Glacier','65%','Ba chế độ','Tím',6),
 ('NuPhy Halo96',3790000,NULL,'Bàn phím cơ gasket 96%','Mechanical','NuPhy','Mint','96%','Ba chế độ','Trắng',5),
 ('NuPhy Field75',3490000,NULL,'Bàn phím cơ layout 75%','Mechanical','NuPhy','Ionic White','75%','Ba chế độ','Xanh lá',6),
 ('NuPhy Gem80',3890000,NULL,'Bàn phím cơ gasket TKL','Mechanical','NuPhy','Mint','TKL','Ba chế độ','Xám',4),
 ('NuPhy Nos75',3690000,NULL,'Bàn phím cơ gasket 75%','Mechanical','NuPhy','Blush','75%','Ba chế độ','Hồng',5),
 ('NuPhy Air75 V2 ISO',3090000,NULL,'Bàn phím cơ low-profile 75% chuẩn ISO','Low-profile','NuPhy','Aloe','75% ISO','Ba chế độ','Xám',4),
 ('NuPhy Air60 V2 ISO',2790000,NULL,'Bàn phím cơ low-profile 60% chuẩn ISO','Low-profile','NuPhy','Wisteria','60% ISO','Ba chế độ','Trắng',3),
 ('NuPhy Air96 V2 ISO',3390000,NULL,'Bàn phím cơ low-profile 96% chuẩn ISO','Low-profile','NuPhy','Moss','96% ISO','Ba chế độ','Đen',3),
 ('NuPhy Halo75 V2 ISO',3590000,NULL,'Bàn phím cơ gasket 75% chuẩn ISO','Mechanical','NuPhy','Mint','75% ISO','Ba chế độ','Trắng',4),
 ('NuPhy Halo96 V2 ISO',3890000,NULL,'Bàn phím cơ gasket 96% chuẩn ISO','Mechanical','NuPhy','Rose Glacier','96% ISO','Ba chế độ','Xám',3),
 ('NuPhy Field75 HE',3990000,NULL,'Bàn phím Hall Effect 75%','Hall Effect','NuPhy','Magnetic','75%','USB-C','Đen',4),
 ('NuPhy Gem80 Barebones',2990000,NULL,'Kit bàn phím cơ gasket TKL chưa kèm switch','Mechanical','NuPhy','Barebones','TKL','USB-C','Bạc',3),
 ('NuPhy Nos75 HE',4190000,NULL,'Bàn phím Hall Effect 75%','Hall Effect','NuPhy','Magnetic','75%','Ba chế độ','Tím',3),
 ('Keychron K5 Max',2790000,NULL,'Bàn phím cơ low-profile full-size','Low-profile','Keychron','Low-profile Brown','Full-size','Ba chế độ','Xám',5),
 ('Akko 5075B HE',3190000,NULL,'Bàn phím Hall Effect 75%','Hall Effect','Akko','Magnetic','75%','Ba chế độ','Đen',4);

INSERT INTO "Products" ("Name","Price","OriginalPrice","Description","CategoryId","BrandId","SwitchType","Layout","ConnectionType","Keycap","Color","IsHotSwap","IsRGB","IsActive","CreatedAt")
SELECT v.name,v.price,v.original_price,v.description,c."Id",b."Id",v.switch_type,v.layout,v.connection_type,'PBT',v.color,true,true,true,NOW()
FROM seed_demo_products v
JOIN "Categories" c ON c."Name" = v.category_name
JOIN "Brands" b ON b."Name" = v.brand_name
WHERE NOT EXISTS (SELECT 1 FROM "Products" p WHERE p."Name" = v.name);

INSERT INTO "Inventories" ("ProductId","StockQuantity","UpdatedAt")
SELECT p."Id", v.stock_quantity, NOW()
FROM seed_demo_products v
JOIN "Products" p ON p."Name" = v.name
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
    IF (SELECT count(*) FROM seed_demo_products v JOIN "Products" p ON p."Name" = v.name) <> 56 THEN
        RAISE EXCEPTION 'Seed requires 56 unique demo products and their categories/brands';
    END IF;
    IF (SELECT count(*) FROM seed_demo_products v
        JOIN "Products" p ON p."Name" = v.name
        JOIN "Inventories" i ON i."ProductId" = p."Id") <> 56 THEN
        RAISE EXCEPTION 'Seed requires inventory for all 56 demo products';
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
