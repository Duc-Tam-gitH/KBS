-- FIX: Bảo đảm các chuỗi tiếng Việt được đọc theo UTF-8.
SET client_encoding = 'UTF8';

-- Các mã băm mật khẩu chỉ là giá trị thay thế; tầng ứng dụng thực hiện bcrypt thực tế.
INSERT INTO "Categories" ("Name", "Description") VALUES
    ('Văn phòng', 'Bàn phím cho công việc hằng ngày'),
    ('Gaming', 'Bàn phím tối ưu cho chơi game'),
    ('Cơ bản', 'Lựa chọn dễ tiếp cận'),
    ('Cao cấp', 'Vật liệu và hoàn thiện cao cấp'),
    ('Custom', 'Bàn phím cho người thích tùy biến'),
    ('60%', 'Layout 60 phần trăm'),
    ('65%', 'Layout 65 phần trăm'),
    ('75%', 'Layout 75 phần trăm'),
    ('TKL', 'Tenkeyless không có numpad'),
    ('Fullsize', 'Layout đầy đủ numpad'),
    ('Mini', 'Bàn phím nhỏ gọn'),
    ('Layout đặc biệt', 'Alice, split và layout độc đáo')
ON CONFLICT ("Name") DO NOTHING;

INSERT INTO "Brands" ("Name", "Country", "Description") VALUES
    ('Keychron', 'Hong Kong', 'Keyboard productivity and custom brand'),
    ('Akko', 'China', 'Mechanical keyboard and keycap brand'),
    ('Leopold', 'Korea', 'Premium Korean keyboard manufacturer'),
    ('Ducky', 'Taiwan', 'Taiwanese gaming keyboard brand'),
    ('Varmilo', 'China', 'Premium themed keyboard manufacturer'),
    ('NuPhy', 'United States', 'Low-profile wireless keyboard brand'),
    ('FL-Esports', 'China', 'Custom-oriented keyboard brand'),
    ('Razer', 'Singapore', 'Gaming peripheral manufacturer'),
    ('Logitech', 'Switzerland', 'Consumer electronics manufacturer'),
    ('Corsair', 'United States', 'Gaming hardware manufacturer'),
    ('HyperX', 'United States', 'Gaming accessory brand'),
    ('Royal Kludge', 'China', 'Affordable wireless keyboard brand'),
    ('Xiaomi', 'China', 'Consumer electronics manufacturer'),
    ('Monsgeek', 'China', 'Aluminium custom keyboard brand'),
    ('Epomaker', 'China', 'Keyboard and switch brand')
ON CONFLICT ("Name") DO NOTHING;

WITH product_seed("Name", "Price", "OriginalPrice", "Description", "CategoryName", "BrandName", "SwitchType", "Layout", "ConnectionType", "Keycap", "Color", "IsHotSwap", "IsRGB") AS (
    VALUES
    ('Keychron K2 Pro', 2190000, 2490000, 'Wireless 75 percent keyboard', '75%', 'Keychron', 'Gateron Brown', '75%', 'Tri-mode', 'Double-shot PBT', 'Dark Gray', true, true),
    ('Keychron K6 Pro', 1990000, 2290000, 'Compact hot-swap keyboard', '65%', 'Keychron', 'Gateron Red', '65%', 'Tri-mode', 'Double-shot PBT', 'Black', true, true),
    ('Keychron Q1 Max', 4490000, NULL, 'CNC aluminium custom keyboard', 'Cao cấp', 'Keychron', 'Gateron Jupiter Banana', '75%', 'Tri-mode', 'Double-shot PBT', 'Carbon Black', true, true),
    ('Akko 5075B Plus', 1790000, 1990000, 'Affordable tri-mode keyboard', '75%', 'Akko', 'Akko CS Jelly Pink', '75%', 'Tri-mode', 'PBT dye-sub', 'Cream Yellow', true, true),
    ('Akko 3068B Plus', 1490000, 1690000, '65 percent wireless keyboard', '65%', 'Akko', 'Akko CS Silver', '65%', 'Tri-mode', 'PBT dye-sub', 'Black Gold', true, true),
    ('Akko MOD 007B HE', 3290000, 3590000, 'Hall effect aluminium keyboard', 'Gaming', 'Akko', 'Akko Cream Yellow Magnetic', '75%', 'Wired', 'Double-shot PBT', 'Black', true, true),
    ('Leopold FC750R PD', 2890000, NULL, 'Premium TKL office keyboard', 'TKL', 'Leopold', 'Cherry MX Brown', 'TKL', 'Wired', 'PBT dye-sub', 'Navy Gray', false, false),
    ('Leopold FC660M PD', 2790000, NULL, 'Compact premium 65 percent keyboard', '65%', 'Leopold', 'Cherry MX Red', '65%', 'Wired', 'PBT dye-sub', 'White Blue', false, false),
    ('Ducky One 3 TKL', 2590000, 2890000, 'Hot-swap gaming TKL', 'Gaming', 'Ducky', 'Cherry MX Red', 'TKL', 'Wired', 'Double-shot PBT', 'Daybreak', true, true),
    ('Ducky One 3 Mini', 2390000, 2690000, '60 percent RGB keyboard', '60%', 'Ducky', 'Cherry MX Blue', '60%', 'Wired', 'Double-shot PBT', 'White', true, true),
    ('Varmilo VA87M Sakura', 3190000, NULL, 'Sakura themed TKL keyboard', 'Cao cấp', 'Varmilo', 'Cherry MX Brown', 'TKL', 'Wired', 'PBT dye-sub', 'Pink', false, false),
    ('Varmilo Minilo 75', 3490000, 3790000, 'Wireless premium compact keyboard', '75%', 'Varmilo', 'Kailh Box White', '75%', 'Tri-mode', 'PBT dye-sub', 'Eucalyptus', true, true),
    ('NuPhy Air75 V2', 2890000, 3190000, 'Low profile wireless keyboard', 'Văn phòng', 'NuPhy', 'Gateron Low Profile Red', '75%', 'Tri-mode', 'Double-shot PBT', 'Lunar Gray', true, true),
    ('NuPhy Halo75 V2', 3590000, 3890000, 'Wireless gasket mount keyboard', 'Cao cấp', 'NuPhy', 'Baby Raccoon', '75%', 'Tri-mode', 'Double-shot PBT', 'Ionic White', true, true),
    ('FL-Esports CMK75', 2490000, 2790000, 'Gasket mount 75 percent keyboard', 'Custom', 'FL-Esports', 'Kailh Box White', '75%', 'Tri-mode', 'PBT dye-sub', 'Blue', true, true),
    ('FL-Esports FL680', 1890000, 2190000, 'Wireless 65 percent keyboard', '65%', 'FL-Esports', 'Gateron Yellow', '65%', 'Tri-mode', 'PBT', 'White', true, true),
    ('Razer BlackWidow V4', 3290000, 3590000, 'Fullsize RGB gaming keyboard', 'Fullsize', 'Razer', 'Razer Green', 'Fullsize', 'Wired', 'ABS', 'Black', false, true),
    ('Razer Huntsman Mini', 2190000, 2490000, 'Optical switch 60 percent keyboard', '60%', 'Razer', 'Razer Linear Optical', '60%', 'Wired', 'Double-shot PBT', 'Mercury White', false, true),
    ('Logitech G Pro X TKL', 3990000, 4290000, 'Wireless esports keyboard', 'Gaming', 'Logitech', 'GX Red', 'TKL', 'Wireless 2.4G', 'Double-shot PBT', 'Black', true, true),
    ('Logitech G715', 3790000, 4090000, 'Wireless RGB TKL keyboard', 'TKL', 'Logitech', 'GX Brown', 'TKL', 'Wireless 2.4G', 'Double-shot PBT', 'White', false, true),
    ('Corsair K70 RGB Pro', 2990000, 3290000, 'Fullsize tournament keyboard', 'Fullsize', 'Corsair', 'Cherry MX Red', 'Fullsize', 'Wired', 'Double-shot PBT', 'Black', false, true),
    ('Corsair K65 RGB Mini', 2190000, 2490000, '60 percent gaming keyboard', '60%', 'Corsair', 'Cherry MX Speed', '60%', 'Wired', 'PBT', 'Black', false, true),
    ('HyperX Alloy Origins 65', 1990000, 2290000, 'Compact aluminium gaming keyboard', '65%', 'HyperX', 'HyperX Red', '65%', 'Wired', 'PBT', 'Black Red', false, true),
    ('HyperX Alloy FPS Pro', 1590000, 1890000, 'Reliable TKL mechanical keyboard', 'TKL', 'HyperX', 'Cherry MX Blue', 'TKL', 'Wired', 'ABS', 'Black', false, true),
    ('Royal Kludge RK84', 1190000, 1390000, 'Budget 75 percent tri-mode keyboard', '75%', 'Royal Kludge', 'RK Brown', '75%', 'Tri-mode', 'PBT', 'White Blue', true, true),
    ('Royal Kludge R75', 1290000, 1490000, 'Budget gasket mount keyboard', '75%', 'Royal Kludge', 'RK Cream', '75%', 'Wired', 'PBT', 'Sky Cyan', true, true),
    ('Xiaomi MIIIW ART Z870', 890000, 990000, 'Slim office mechanical keyboard', 'Văn phòng', 'Xiaomi', 'TTC Red', 'TKL', 'Wireless 2.4G', 'ABS', 'Gray', false, false),
    ('Xiaomi Yuemi MK01', 990000, 1090000, 'Minimal fullsize keyboard', 'Cơ bản', 'Xiaomi', 'TTC Brown', 'Fullsize', 'Wired', 'ABS', 'White', false, false),
    ('Monsgeek M1W', 3490000, 3790000, 'Wireless aluminium barebone keyboard', 'Custom', 'Monsgeek', 'Akko Cream Yellow', '75%', 'Tri-mode', 'Double-shot PBT', 'Black', true, true),
    ('Monsgeek M3W', 3890000, 4190000, 'TKL aluminium custom keyboard', 'Custom', 'Monsgeek', 'Akko V3 Cream Blue', 'TKL', 'Tri-mode', 'Double-shot PBT', 'Silver', true, true),
    ('Epomaker TH80 Pro', 1690000, 1990000, 'Wireless RGB 75 percent keyboard', '75%', 'Epomaker', 'Gateron Pro Yellow', '75%', 'Tri-mode', 'PBT', 'Black Purple', true, true),
    ('Epomaker EK68', 1390000, 1590000, 'Compact wireless keyboard', '65%', 'Epomaker', 'Gateron Red', '65%', 'Tri-mode', 'PBT', 'Purple', true, true),
    ('Epomaker RT100', 2490000, 2790000, 'Retro keyboard with smart display', 'Layout đặc biệt', 'Epomaker', 'Kailh Pink', '95%', 'Tri-mode', 'PBT dye-sub', 'Retro White', true, true),
    ('Keychron K10 Pro', 2490000, 2790000, 'Fullsize wireless keyboard', 'Fullsize', 'Keychron', 'Gateron Brown', 'Fullsize', 'Tri-mode', 'Double-shot PBT', 'Black', true, true),
    ('Akko Alice Plus', 2290000, 2590000, 'Ergonomic Alice layout keyboard', 'Layout đặc biệt', 'Akko', 'Akko CS Piano Pro', 'Alice', 'Tri-mode', 'PBT dye-sub', 'White Pink', true, true),
    ('Ducky Zero 6108', 2790000, 2990000, 'Wireless fullsize keyboard', 'Fullsize', 'Ducky', 'Cherry MX Brown', 'Fullsize', 'Tri-mode', 'Double-shot PBT', 'Classic White', true, true),
    ('Varmilo VXT Model V', 2990000, 3290000, 'Wireless 65 percent themed keyboard', '65%', 'Varmilo', 'Varmilo EC V2', '65%', 'Tri-mode', 'PBT dye-sub', 'Iris', true, true),
    ('NuPhy Field75 HE', 4190000, 4500000, 'Hall effect gaming keyboard', 'Gaming', 'NuPhy', 'Magnetic Jade', '75%', 'Wired', 'Double-shot PBT', 'Obsidian Black', true, true),
    ('FL-Esports GP87', 2090000, 2390000, 'Custom TKL gasket keyboard', 'TKL', 'FL-Esports', 'Gateron Yellow', 'TKL', 'Wired', 'PBT', 'Cream', true, true),
    ('Royal Kludge RK61', 990000, 1190000, 'Entry 60 percent mechanical keyboard', 'Cơ bản', 'Royal Kludge', 'RK Red', '60%', 'Wireless 2.4G', 'ABS', 'White', true, true)
)
INSERT INTO "Products" ("Name", "Price", "OriginalPrice", "Description", "CategoryId", "BrandId", "SwitchType", "Layout", "ConnectionType", "Keycap", "Color", "IsHotSwap", "IsRGB")
SELECT s."Name", s."Price", s."OriginalPrice", s."Description", c."Id", b."Id", s."SwitchType", s."Layout", s."ConnectionType", s."Keycap", s."Color", s."IsHotSwap", s."IsRGB"
FROM product_seed s
JOIN "Categories" c ON c."Name" = s."CategoryName"
JOIN "Brands" b ON b."Name" = s."BrandName"
WHERE NOT EXISTS (SELECT 1 FROM "Products" p WHERE p."Name" = s."Name")
-- FIX: Xác định xung đột theo tên sản phẩm duy nhất.
ON CONFLICT ("Name") DO NOTHING;

INSERT INTO "Inventories" ("ProductId", "StockQuantity")
SELECT p."Id",
       CASE WHEN row_number() OVER (ORDER BY p."Id") % 11 = 0 THEN 4
            WHEN row_number() OVER (ORDER BY p."Id") % 7 = 0 THEN 8
            ELSE 20 + (row_number() OVER (ORDER BY p."Id") * 13 % 181)::integer END
FROM "Products" p
ON CONFLICT ("ProductId") DO UPDATE
SET "StockQuantity" = EXCLUDED."StockQuantity";

INSERT INTO "Roles" ("Name", "Description") VALUES
    ('Admin', 'System administrator'),
    ('Customer', 'Store customer'),
    ('Staff', 'Store staff')
ON CONFLICT ("Name") DO NOTHING;

INSERT INTO "Users" ("Username", "Email", "PasswordHash", "FullName", "PhoneNumber", "RoleId") VALUES
    ('admin', 'admin@kbs88.vn', '$2a$10$kbs88AdminPlaceholderHash000000000000000000000000000000000', 'Quản trị viên KBS', '0901000001', (SELECT "Id" FROM "Roles" WHERE "Name" = 'Admin')),
    ('minhnguyen', 'minh.nguyen@example.com', '$2a$10$kbs88CustomerHash00000000000000000000000000000000000001', 'Nguyễn Minh', '0901000002', (SELECT "Id" FROM "Roles" WHERE "Name" = 'Customer')),
    ('lantran', 'lan.tran@example.com', '$2a$10$kbs88CustomerHash00000000000000000000000000000000000002', 'Trần Thu Lan', '0901000003', (SELECT "Id" FROM "Roles" WHERE "Name" = 'Customer')),
    ('hoangpham', 'hoang.pham@example.com', '$2a$10$kbs88CustomerHash00000000000000000000000000000000000003', 'Phạm Hoàng', '0901000004', (SELECT "Id" FROM "Roles" WHERE "Name" = 'Customer')),
    ('maianh', 'mai.anh@example.com', '$2a$10$kbs88CustomerHash00000000000000000000000000000000000004', 'Lê Mai Anh', '0901000005', (SELECT "Id" FROM "Roles" WHERE "Name" = 'Customer'))
ON CONFLICT ("Username") DO NOTHING;

WITH order_seed("Username", "CustomerName", "PhoneNumber", "ShippingAddress", "PaymentMethod", "PaymentStatus", "OrderStatus", "CreatedAt") AS (
    VALUES
    ('minhnguyen', 'Nguyễn Minh', '0901000002', '12 Nguyễn Huệ, Quận 1, TP.HCM', 'COD', 'PENDING', 'PENDING', current_date - interval '15 days'),
    ('lantran', 'Trần Thu Lan', '0901000003', '45 Lê Lợi, Hải Châu, Đà Nẵng', 'BANK_TRANSFER', 'PAID', 'CONFIRMED', current_date - interval '29 days'),
    ('hoangpham', 'Phạm Hoàng', '0901000004', '88 Trần Duy Hưng, Cầu Giấy, Hà Nội', 'COD', 'PAID', 'SHIPPED', current_date - interval '47 days'),
    ('maianh', 'Lê Mai Anh', '0901000005', '22 Võ Văn Tần, Quận 3, TP.HCM', 'BANK_TRANSFER', 'PAID', 'COMPLETED', current_date - interval '71 days'),
    ('minhnguyen', 'Nguyễn Minh', '0901000002', '105 Nguyễn Văn Linh, Hải Châu, Đà Nẵng', 'COD', 'PENDING', 'CANCELLED', current_date - interval '96 days'),
    ('lantran', 'Trần Thu Lan', '0901000003', '16 Hùng Vương, Nha Trang, Khánh Hòa', 'BANK_TRANSFER', 'PAID', 'COMPLETED', current_date - interval '121 days'),
    ('hoangpham', 'Phạm Hoàng', '0901000004', '9 Lý Thường Kiệt, Hoàn Kiếm, Hà Nội', 'COD', 'PAID', 'COMPLETED', current_date - interval '146 days'),
    ('maianh', 'Lê Mai Anh', '0901000005', '31 Điện Biên Phủ, Bình Thạnh, TP.HCM', 'BANK_TRANSFER', 'PENDING', 'PENDING', current_date - interval '168 days'),
    ('minhnguyen', 'Nguyễn Minh', '0901000002', '67 Phan Chu Trinh, Hội An, Quảng Nam', 'COD', 'PAID', 'CANCELLED', current_date - interval '179 days')
)
INSERT INTO "Orders" ("UserId", "CustomerName", "PhoneNumber", "ShippingAddress", "TotalPrice", "PaymentMethod", "PaymentStatus", "OrderStatus", "CreatedAt")
SELECT u."Id", s."CustomerName", s."PhoneNumber", s."ShippingAddress", 0, s."PaymentMethod", s."PaymentStatus", s."OrderStatus", s."CreatedAt"
FROM order_seed s JOIN "Users" u ON u."Username" = s."Username"
-- FIX: Chống trùng dữ liệu mẫu theo cả người dùng và địa chỉ giao hàng.
WHERE NOT EXISTS (
    SELECT 1 FROM "Orders" o
    WHERE o."UserId" = u."Id" AND o."ShippingAddress" = s."ShippingAddress"
)
ON CONFLICT DO NOTHING;

WITH detail_seed("ShippingAddress", "ProductName", "Quantity") AS (
    VALUES
    ('12 Nguyễn Huệ, Quận 1, TP.HCM', 'Keychron K2 Pro', 1), ('12 Nguyễn Huệ, Quận 1, TP.HCM', 'Akko 3068B Plus', 1),
    ('45 Lê Lợi, Hải Châu, Đà Nẵng', 'Leopold FC750R PD', 1), ('45 Lê Lợi, Hải Châu, Đà Nẵng', 'NuPhy Air75 V2', 1),
    ('88 Trần Duy Hưng, Cầu Giấy, Hà Nội', 'Razer BlackWidow V4', 1), ('88 Trần Duy Hưng, Cầu Giấy, Hà Nội', 'HyperX Alloy Origins 65', 2),
    ('22 Võ Văn Tần, Quận 3, TP.HCM', 'Keychron Q1 Max', 1), ('22 Võ Văn Tần, Quận 3, TP.HCM', 'Monsgeek M1W', 1),
    ('105 Nguyễn Văn Linh, Hải Châu, Đà Nẵng', 'Royal Kludge RK84', 1), ('105 Nguyễn Văn Linh, Hải Châu, Đà Nẵng', 'Xiaomi Yuemi MK01', 1),
    ('16 Hùng Vương, Nha Trang, Khánh Hòa', 'Varmilo Minilo 75', 1), ('16 Hùng Vương, Nha Trang, Khánh Hòa', 'Ducky One 3 TKL', 1),
    ('9 Lý Thường Kiệt, Hoàn Kiếm, Hà Nội', 'Logitech G Pro X TKL', 1), ('9 Lý Thường Kiệt, Hoàn Kiếm, Hà Nội', 'Corsair K70 RGB Pro', 1),
    ('31 Điện Biên Phủ, Bình Thạnh, TP.HCM', 'Epomaker TH80 Pro', 1), ('31 Điện Biên Phủ, Bình Thạnh, TP.HCM', 'FL-Esports CMK75', 1),
    ('67 Phan Chu Trinh, Hội An, Quảng Nam', 'Akko Alice Plus', 1), ('67 Phan Chu Trinh, Hội An, Quảng Nam', 'NuPhy Field75 HE', 1),
    ('67 Phan Chu Trinh, Hội An, Quảng Nam', 'Royal Kludge RK61', 2), ('12 Nguyễn Huệ, Quận 1, TP.HCM', 'Epomaker EK68', 1),
    ('45 Lê Lợi, Hải Châu, Đà Nẵng', 'Keychron K10 Pro', 1), ('88 Trần Duy Hưng, Cầu Giấy, Hà Nội', 'Ducky One 3 Mini', 1)
)
INSERT INTO "OrderDetails" ("OrderId", "ProductId", "UnitPrice", "Quantity")
SELECT o."Id", p."Id", p."Price", s."Quantity"
FROM detail_seed s
JOIN "Orders" o ON o."ShippingAddress" = s."ShippingAddress"
JOIN "Products" p ON p."Name" = s."ProductName"
WHERE NOT EXISTS (
    SELECT 1 FROM "OrderDetails" d
    WHERE d."OrderId" = o."Id" AND d."ProductId" = p."Id"
)
ON CONFLICT DO NOTHING;

UPDATE "Orders" o
SET "TotalPrice" = COALESCE((
    SELECT SUM(d."UnitPrice" * d."Quantity")
    FROM "OrderDetails" d
    WHERE d."OrderId" = o."Id"
), 0);
