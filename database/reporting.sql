-- Views, function và trigger phục vụ báo cáo/quản trị dữ liệu.
CREATE OR REPLACE VIEW vw_daily_revenue AS
SELECT o."CreatedAt"::date AS report_date, COUNT(*) AS order_count, COALESCE(SUM(o."TotalPrice"),0) AS revenue
FROM "Orders" o WHERE o."PaymentStatus"='PAID' AND o."OrderStatus" <> 'CANCELLED'
GROUP BY o."CreatedAt"::date;

CREATE OR REPLACE VIEW vw_top_products AS
SELECT p."Id" AS product_id,p."Name" AS product_name,b."Name" AS brand_name,SUM(d."Quantity") AS units_sold,SUM(d."Quantity"*d."UnitPrice") AS revenue
FROM "OrderDetails" d JOIN "Orders" o ON o."Id"=d."OrderId" JOIN "Products" p ON p."Id"=d."ProductId" JOIN "Brands" b ON b."Id"=p."BrandId"
WHERE o."PaymentStatus"='PAID' AND o."OrderStatus" <> 'CANCELLED'
GROUP BY p."Id",p."Name",b."Name";

CREATE OR REPLACE VIEW vw_low_inventory AS
SELECT p."Id" AS product_id,p."Name",i."StockQuantity",i."UpdatedAt"
FROM "Inventories" i JOIN "Products" p ON p."Id"=i."ProductId" WHERE i."StockQuantity" <= 5;

CREATE OR REPLACE VIEW vw_customer_purchase_history AS
SELECT u."Id" AS user_id,u."FullName",u."Email",o."Id" AS order_id,o."CreatedAt",o."OrderStatus",o."PaymentStatus",o."TotalPrice"
FROM "Users" u JOIN "Orders" o ON o."UserId"=u."Id";

-- Hàm tạo đơn thay thế an toàn cho client SQL trực tiếp. API hiện có transaction tương đương;
-- chỉ chọn MỘT đường tạo đơn trong production để tránh trừ tồn hai lần.
CREATE OR REPLACE FUNCTION fn_create_order(p_user_id bigint,p_customer_name text,p_phone text,p_address text,p_payment_method text,p_items jsonb) RETURNS bigint LANGUAGE plpgsql AS $$
DECLARE v_order_id bigint; v_item jsonb; v_product_id bigint; v_quantity integer; v_price numeric(18,2);
BEGIN
  IF jsonb_typeof(p_items) <> 'array' OR jsonb_array_length(p_items)=0 THEN RAISE EXCEPTION 'Đơn hàng phải có sản phẩm'; END IF;
  INSERT INTO "Orders" ("UserId","CustomerName","PhoneNumber","ShippingAddress","TotalPrice","PaymentMethod","PaymentStatus","OrderStatus","CreatedAt") VALUES (p_user_id,p_customer_name,p_phone,p_address,0,p_payment_method,'PENDING','PENDING',NOW()) RETURNING "Id" INTO v_order_id;
  FOR v_item IN SELECT * FROM jsonb_array_elements(p_items) LOOP
    v_product_id := (v_item->>'productId')::bigint; v_quantity := (v_item->>'quantity')::integer;
    IF v_quantity IS NULL OR v_quantity<=0 THEN RAISE EXCEPTION 'Số lượng không hợp lệ'; END IF;
    SELECT "Price" INTO v_price FROM "Products" WHERE "Id"=v_product_id AND "IsActive" FOR UPDATE;
    IF NOT FOUND THEN RAISE EXCEPTION 'Sản phẩm % không tồn tại/đã ngừng bán',v_product_id; END IF;
    UPDATE "Inventories" SET "StockQuantity"="StockQuantity"-v_quantity,"UpdatedAt"=NOW() WHERE "ProductId"=v_product_id AND "StockQuantity">=v_quantity;
    IF NOT FOUND THEN RAISE EXCEPTION 'Sản phẩm % không đủ tồn kho',v_product_id; END IF;
    INSERT INTO "OrderDetails" ("OrderId","ProductId","UnitPrice","Quantity") VALUES (v_order_id,v_product_id,v_price,v_quantity);
  END LOOP;
  UPDATE "Orders" SET "TotalPrice"=(SELECT SUM("UnitPrice"*"Quantity") FROM "OrderDetails" WHERE "OrderId"=v_order_id) WHERE "Id"=v_order_id;
  RETURN v_order_id;
END $$;

CREATE OR REPLACE FUNCTION fn_inventory_non_negative() RETURNS trigger LANGUAGE plpgsql AS $$ BEGIN IF NEW."StockQuantity" < 0 THEN RAISE EXCEPTION 'Tồn kho không thể âm'; END IF; RETURN NEW; END $$;
DROP TRIGGER IF EXISTS trg_inventory_non_negative ON "Inventories";
CREATE TRIGGER trg_inventory_non_negative BEFORE INSERT OR UPDATE OF "StockQuantity" ON "Inventories" FOR EACH ROW EXECUTE FUNCTION fn_inventory_non_negative();

-- Truy vấn minh chứng báo cáo
-- SELECT * FROM vw_daily_revenue WHERE report_date BETWEEN CURRENT_DATE-30 AND CURRENT_DATE ORDER BY report_date;
-- SELECT * FROM vw_top_products ORDER BY units_sold DESC, revenue DESC LIMIT 10;
-- SELECT * FROM vw_low_inventory ORDER BY "StockQuantity", "Name";
-- SELECT * FROM vw_customer_purchase_history WHERE user_id = 1 ORDER BY "CreatedAt" DESC;

-- Quyền tối thiểu; thay mật khẩu bằng secrets. Chủ sở hữu schema/migration không dùng role ứng dụng.
-- CREATE ROLE kbs_app LOGIN PASSWORD 'REPLACE_WITH_SECRET';
-- CREATE ROLE kbs_readonly LOGIN PASSWORD 'REPLACE_WITH_SECRET';
-- GRANT USAGE ON SCHEMA public TO kbs_app, kbs_readonly;
-- GRANT SELECT, INSERT, UPDATE, DELETE ON ALL TABLES IN SCHEMA public TO kbs_app;
-- GRANT USAGE, SELECT ON ALL SEQUENCES IN SCHEMA public TO kbs_app;
-- GRANT SELECT ON vw_daily_revenue, vw_top_products, vw_low_inventory, vw_customer_purchase_history TO kbs_readonly;
-- ALTER DEFAULT PRIVILEGES IN SCHEMA public GRANT SELECT, INSERT, UPDATE, DELETE ON TABLES TO kbs_app;
