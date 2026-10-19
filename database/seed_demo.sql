-- Chạy sau EF Core migrations. Dữ liệu chỉ dành cho development/demo.
BEGIN;

INSERT INTO "Brands" ("Name", "Description", "IsActive", "CreatedAt")
SELECT v.name, v.description, true, NOW()
FROM (VALUES ('Keychron','Bàn phím cơ và custom'), ('Akko','Bàn phím và switch'), ('NuPhy','Low-profile không dây')) AS v(name, description)
WHERE NOT EXISTS (SELECT 1 FROM "Brands" b WHERE lower(b."Name") = lower(v.name));

INSERT INTO "Users" ("Username","Email","PasswordHash","FullName","PhoneNumber","RoleId","IsActive","CreatedAt")
SELECT v.username, v.email, 'AQAAAAEAAYagAAAAEG3JTvT3V+IWs2IJ+rZDRE3MSJJYsG7he40/goSG55xZ7mjiPfc9MxPy5gZyUmbDtw==', v.full_name, v.phone, r."Id", true, NOW()
FROM (VALUES ('minh.demo','minh.demo@kbs.local','Nguyễn Minh','0901000001'), ('lan.demo','lan.demo@kbs.local','Trần Lan','0901000002')) AS v(username,email,full_name,phone)
JOIN "Roles" r ON r."Name" = 'Customer'
WHERE NOT EXISTS (SELECT 1 FROM "Users" u WHERE u."Email" = v.email);

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

-- Đơn demo không trừ tồn: mục đích là tạo dữ liệu báo cáo đã chốt.
INSERT INTO "Orders" ("UserId","CustomerName","PhoneNumber","ShippingAddress","TotalPrice","PaymentMethod","PaymentStatus","OrderStatus","CreatedAt")
SELECT u."Id",u."FullName",u."PhoneNumber",'Quận 1, TP. Hồ Chí Minh',2490000,'COD','PAID','COMPLETED',NOW()-INTERVAL '6 days' FROM "Users" u WHERE u."Email"='minh.demo@kbs.local' AND NOT EXISTS (SELECT 1 FROM "Orders" o WHERE o."PhoneNumber"=u."PhoneNumber" AND o."CreatedAt"::date=(NOW()-INTERVAL '6 days')::date);
INSERT INTO "OrderDetails" ("OrderId","ProductId","UnitPrice","Quantity") SELECT o."Id",p."Id",p."Price",1 FROM "Orders" o JOIN "Products" p ON p."Name"='Keychron K8 Pro' WHERE o."PhoneNumber"='0901000001' AND NOT EXISTS (SELECT 1 FROM "OrderDetails" d WHERE d."OrderId"=o."Id");

INSERT INTO "Orders" ("UserId","CustomerName","PhoneNumber","ShippingAddress","TotalPrice","PaymentMethod","PaymentStatus","OrderStatus","CreatedAt")
SELECT u."Id",u."FullName",u."PhoneNumber",'Hải Châu, Đà Nẵng',1890000,'BANKING','PAID','COMPLETED',NOW()-INTERVAL '3 days' FROM "Users" u WHERE u."Email"='lan.demo@kbs.local' AND NOT EXISTS (SELECT 1 FROM "Orders" o WHERE o."PhoneNumber"=u."PhoneNumber" AND o."ShippingAddress"='Hải Châu, Đà Nẵng');
INSERT INTO "OrderDetails" ("OrderId","ProductId","UnitPrice","Quantity") SELECT o."Id",p."Id",p."Price",1 FROM "Orders" o JOIN "Products" p ON p."Name"='Akko 5075B Plus' WHERE o."PhoneNumber"='0901000002' AND o."ShippingAddress"='Hải Châu, Đà Nẵng' AND NOT EXISTS (SELECT 1 FROM "OrderDetails" d WHERE d."OrderId"=o."Id");

INSERT INTO "Orders" ("UserId","CustomerName","PhoneNumber","ShippingAddress","TotalPrice","PaymentMethod","PaymentStatus","OrderStatus","CreatedAt")
SELECT u."Id",u."FullName",u."PhoneNumber",'Cầu Giấy, Hà Nội',2990000,'COD','PENDING','PENDING',NOW()-INTERVAL '1 day' FROM "Users" u WHERE u."Email"='minh.demo@kbs.local' AND NOT EXISTS (SELECT 1 FROM "Orders" o WHERE o."PhoneNumber"=u."PhoneNumber" AND o."ShippingAddress"='Cầu Giấy, Hà Nội');
INSERT INTO "OrderDetails" ("OrderId","ProductId","UnitPrice","Quantity") SELECT o."Id",p."Id",p."Price",1 FROM "Orders" o JOIN "Products" p ON p."Name"='NuPhy Air75 V2' WHERE o."PhoneNumber"='0901000001' AND o."ShippingAddress"='Cầu Giấy, Hà Nội' AND NOT EXISTS (SELECT 1 FROM "OrderDetails" d WHERE d."OrderId"=o."Id");

COMMIT;
