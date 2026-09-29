\set ON_ERROR_STOP on

CREATE OR REPLACE FUNCTION "fn_products_auto_inventory"()
RETURNS trigger
LANGUAGE plpgsql AS $$
BEGIN
    INSERT INTO "Inventories" ("ProductId", "StockQuantity")
    VALUES (NEW."Id", 0)
    ON CONFLICT ("ProductId") DO NOTHING;
    RETURN NEW;
END;
$$;

CREATE OR REPLACE FUNCTION "fn_inventories_set_updated_at"()
RETURNS trigger
LANGUAGE plpgsql AS $$
BEGIN
    NEW."UpdatedAt" := now();
    RETURN NEW;
END;
$$;

CREATE OR REPLACE FUNCTION "fn_orders_validate_status_transition"()
RETURNS trigger
LANGUAGE plpgsql AS $$
BEGIN
    IF NEW."OrderStatus" = OLD."OrderStatus" THEN
        RETURN NEW;
    END IF;

    IF (OLD."OrderStatus" = 'PENDING' AND NEW."OrderStatus" IN ('CONFIRMED', 'CANCELLED'))
       OR (OLD."OrderStatus" = 'CONFIRMED' AND NEW."OrderStatus" IN ('SHIPPED', 'CANCELLED'))
       OR (OLD."OrderStatus" = 'SHIPPED' AND NEW."OrderStatus" IN ('COMPLETED', 'CANCELLED')) THEN
        RETURN NEW;
    END IF;

    RAISE EXCEPTION 'Invalid order status transition from % to %',
        OLD."OrderStatus", NEW."OrderStatus";
END;
$$;

CREATE OR REPLACE FUNCTION "fn_orderdetails_stock_check"()
RETURNS trigger
LANGUAGE plpgsql AS $$
DECLARE
    v_stock integer;
BEGIN
    SELECT i."StockQuantity" INTO v_stock
    FROM "Inventories" i
    WHERE i."ProductId" = NEW."ProductId"
    FOR UPDATE;

    IF NOT FOUND THEN
        RAISE EXCEPTION 'Inventory does not exist for product %', NEW."ProductId";
    END IF;
    IF NEW."Quantity" > v_stock THEN
        RAISE EXCEPTION 'Insufficient stock for product %: requested %, available %',
            NEW."ProductId", NEW."Quantity", v_stock;
    END IF;
    RETURN NEW;
END;
$$;

DROP TRIGGER IF EXISTS "trg_products_auto_inventory" ON "Products";
CREATE TRIGGER "trg_products_auto_inventory"
AFTER INSERT ON "Products"
FOR EACH ROW EXECUTE FUNCTION "fn_products_auto_inventory"();

DROP TRIGGER IF EXISTS "trg_inventories_updated_at" ON "Inventories";
CREATE TRIGGER "trg_inventories_updated_at"
BEFORE UPDATE ON "Inventories"
FOR EACH ROW EXECUTE FUNCTION "fn_inventories_set_updated_at"();

DROP TRIGGER IF EXISTS "trg_orders_validate_status" ON "Orders";
CREATE TRIGGER "trg_orders_validate_status"
BEFORE UPDATE OF "OrderStatus" ON "Orders"
FOR EACH ROW EXECUTE FUNCTION "fn_orders_validate_status_transition"();

DROP TRIGGER IF EXISTS "trg_orderdetails_stock_check" ON "OrderDetails";
CREATE TRIGGER "trg_orderdetails_stock_check"
BEFORE INSERT ON "OrderDetails"
FOR EACH ROW EXECUTE FUNCTION "fn_orderdetails_stock_check"();

CREATE OR REPLACE PROCEDURE "sp_CreateOrder"(
    IN p_user_id integer,
    IN p_customer_name varchar,
    IN p_phone varchar,
    IN p_address text,
    IN p_payment_method varchar,
    IN p_items jsonb,
    OUT p_order_id integer)
LANGUAGE plpgsql AS $$
DECLARE
    v_item jsonb;
    v_product_id integer;
    v_quantity integer;
    v_price numeric(12,2);
    v_stock integer;
    v_total numeric(12,2) := 0;
    v_order_id bigint;
BEGIN
    IF p_items IS NULL OR jsonb_typeof(p_items) <> 'array' OR jsonb_array_length(p_items) = 0 THEN
        RAISE EXCEPTION 'p_items must be a non-empty JSON array.';
    END IF;
    IF upper(p_payment_method) NOT IN ('COD', 'BANK_TRANSFER') THEN
        RAISE EXCEPTION 'Unsupported payment method: %', p_payment_method;
    END IF;
    IF NOT EXISTS (SELECT 1 FROM "Users" WHERE "Id" = p_user_id AND "IsActive") THEN
        RAISE EXCEPTION 'Active user % does not exist.', p_user_id;
    END IF;

    INSERT INTO "Orders" ("UserId", "CustomerName", "PhoneNumber", "ShippingAddress", "TotalPrice", "PaymentMethod")
    VALUES (p_user_id, p_customer_name, p_phone, p_address, 0, upper(p_payment_method))
    RETURNING "Id" INTO v_order_id;

    FOR v_item IN SELECT value FROM jsonb_array_elements(p_items)
    LOOP
        v_product_id := (v_item ->> 'productId')::integer;
        v_quantity := (v_item ->> 'quantity')::integer;
        IF v_quantity IS NULL OR v_quantity <= 0 THEN
            RAISE EXCEPTION 'Quantity for product % must be positive.', v_product_id;
        END IF;

        SELECT p."Price", i."StockQuantity" INTO v_price, v_stock
        FROM "Products" p
        JOIN "Inventories" i ON i."ProductId" = p."Id"
        WHERE p."Id" = v_product_id AND p."IsActive"
        FOR UPDATE OF i;

        IF NOT FOUND THEN
            RAISE EXCEPTION 'Active product % does not exist.', v_product_id;
        END IF;
        IF v_stock < v_quantity THEN
            RAISE EXCEPTION 'Insufficient stock for product %: requested %, available %',
                v_product_id, v_quantity, v_stock;
        END IF;

        INSERT INTO "OrderDetails" ("OrderId", "ProductId", "UnitPrice", "Quantity")
        VALUES (v_order_id, v_product_id, v_price, v_quantity);

        UPDATE "Inventories"
        SET "StockQuantity" = "StockQuantity" - v_quantity
        WHERE "ProductId" = v_product_id;

        v_total := v_total + v_price * v_quantity;
    END LOOP;

    UPDATE "Orders" SET "TotalPrice" = v_total WHERE "Id" = v_order_id;
    p_order_id := v_order_id::integer;
EXCEPTION
    WHEN OTHERS THEN
        RAISE EXCEPTION 'Create order failed: %', SQLERRM;
END;
$$;

CREATE OR REPLACE PROCEDURE "sp_UpdateOrderStatus"(
    IN p_order_id integer,
    IN p_new_status varchar)
LANGUAGE plpgsql AS $$
DECLARE
    v_current_status varchar(20);
    v_detail record;
    v_status varchar(20) := upper(p_new_status);
BEGIN
    SELECT "OrderStatus" INTO v_current_status
    FROM "Orders" WHERE "Id" = p_order_id FOR UPDATE;

    IF NOT FOUND THEN
        RAISE EXCEPTION 'Order % does not exist.', p_order_id;
    END IF;
    IF v_current_status IN ('COMPLETED', 'CANCELLED') THEN
        RAISE EXCEPTION 'Order % is terminal with status %.', p_order_id, v_current_status;
    END IF;
    IF NOT ((v_current_status = 'PENDING' AND v_status IN ('CONFIRMED', 'CANCELLED'))
        OR (v_current_status = 'CONFIRMED' AND v_status IN ('SHIPPED', 'CANCELLED'))
        OR (v_current_status = 'SHIPPED' AND v_status IN ('COMPLETED', 'CANCELLED'))) THEN
        RAISE EXCEPTION 'Invalid order status transition from % to %', v_current_status, v_status;
    END IF;

    UPDATE "Orders" SET "OrderStatus" = v_status WHERE "Id" = p_order_id;

    IF v_status = 'CANCELLED' AND v_current_status IN ('PENDING', 'CONFIRMED') THEN
        FOR v_detail IN
            SELECT "ProductId", "Quantity" FROM "OrderDetails" WHERE "OrderId" = p_order_id
        LOOP
            UPDATE "Inventories"
            SET "StockQuantity" = "StockQuantity" + v_detail."Quantity"
            WHERE "ProductId" = v_detail."ProductId";
        END LOOP;
    END IF;
END;
$$;

CREATE OR REPLACE PROCEDURE "sp_UpdateStock"(
    IN p_product_id integer,
    IN p_delta integer)
LANGUAGE plpgsql AS $$
DECLARE
    v_stock integer;
BEGIN
    SELECT "StockQuantity" INTO v_stock
    FROM "Inventories" WHERE "ProductId" = p_product_id FOR UPDATE;
    IF NOT FOUND THEN
        RAISE EXCEPTION 'Inventory for product % does not exist.', p_product_id;
    END IF;
    IF v_stock + p_delta < 0 THEN
        RAISE EXCEPTION 'Stock update would make product % negative.', p_product_id;
    END IF;
    UPDATE "Inventories"
    SET "StockQuantity" = "StockQuantity" + p_delta, "UpdatedAt" = now()
    WHERE "ProductId" = p_product_id;
END;
$$;

CREATE OR REPLACE PROCEDURE "sp_RegisterUser"(
    IN p_username varchar,
    IN p_email varchar,
    IN p_password_hash varchar,
    IN p_full_name varchar,
    IN p_phone varchar,
    IN p_role_name varchar)
LANGUAGE plpgsql AS $$
DECLARE
    v_role_id bigint;
BEGIN
    SELECT "Id" INTO v_role_id FROM "Roles" WHERE "Name" = p_role_name;
    IF NOT FOUND THEN
        RAISE EXCEPTION 'Role % does not exist.', p_role_name;
    END IF;
    IF EXISTS (SELECT 1 FROM "Users" WHERE "Username" = p_username) THEN
        RAISE EXCEPTION 'Username % is already registered.', p_username;
    END IF;
    IF EXISTS (SELECT 1 FROM "Users" WHERE "Email" = p_email) THEN
        RAISE EXCEPTION 'Email % is already registered.', p_email;
    END IF;
    INSERT INTO "Users" ("Username", "Email", "PasswordHash", "FullName", "PhoneNumber", "RoleId")
    VALUES (p_username, p_email, p_password_hash, p_full_name, p_phone, v_role_id);
END;
$$;

CREATE OR REPLACE PROCEDURE "sp_CancelOrder"(
    IN p_order_id integer,
    IN p_reason text)
LANGUAGE plpgsql AS $$
BEGIN
    IF p_reason IS NULL OR btrim(p_reason) = '' THEN
        RAISE EXCEPTION 'A cancellation reason is required.';
    END IF;
    CALL "sp_UpdateOrderStatus"(p_order_id, 'CANCELLED');
    RAISE NOTICE 'Order % cancelled. Reason: %', p_order_id, p_reason;
END;
$$;

CREATE OR REPLACE FUNCTION "fn_GetRevenueByDay"(p_from date, p_to date)
RETURNS TABLE(order_date date, order_count bigint, revenue numeric)
LANGUAGE sql STABLE AS $$
    SELECT o."CreatedAt"::date, COUNT(*)::bigint, COALESCE(SUM(o."TotalPrice"), 0)
    FROM "Orders" o
    WHERE o."PaymentStatus" = 'PAID'
      AND o."OrderStatus" <> 'CANCELLED'
      AND o."CreatedAt"::date BETWEEN p_from AND p_to
    GROUP BY o."CreatedAt"::date
    ORDER BY o."CreatedAt"::date;
$$;

CREATE OR REPLACE FUNCTION "fn_GetTopSellingProducts"(p_limit integer, p_from date, p_to date)
RETURNS TABLE(product_id integer, product_name varchar, total_quantity bigint, total_revenue numeric)
LANGUAGE sql STABLE AS $$
    SELECT p."Id"::integer, p."Name", SUM(d."Quantity")::bigint,
           COALESCE(SUM(d."Quantity" * d."UnitPrice"), 0)
    FROM "OrderDetails" d
    JOIN "Orders" o ON o."Id" = d."OrderId"
    JOIN "Products" p ON p."Id" = d."ProductId"
    WHERE o."OrderStatus" <> 'CANCELLED'
      AND o."CreatedAt"::date BETWEEN p_from AND p_to
    GROUP BY p."Id", p."Name"
    ORDER BY SUM(d."Quantity") DESC, SUM(d."Quantity" * d."UnitPrice") DESC
    LIMIT GREATEST(p_limit, 0);
$$;

CREATE OR REPLACE FUNCTION "fn_GetLowStockProducts"(p_threshold integer)
RETURNS TABLE(product_id integer, product_name varchar, brand_name varchar, stock_quantity integer)
LANGUAGE sql STABLE AS $$
    SELECT p."Id"::integer, p."Name", b."Name", i."StockQuantity"
    FROM "Inventories" i
    JOIN "Products" p ON p."Id" = i."ProductId"
    JOIN "Brands" b ON b."Id" = p."BrandId"
    WHERE i."StockQuantity" < p_threshold
    ORDER BY i."StockQuantity", p."Name";
$$;

CREATE OR REPLACE FUNCTION "fn_GetMonthlyRevenue"(p_year integer)
RETURNS TABLE(month integer, revenue numeric)
LANGUAGE sql STABLE AS $$
    SELECT EXTRACT(MONTH FROM o."CreatedAt")::integer,
           COALESCE(SUM(o."TotalPrice"), 0)
    FROM "Orders" o
    WHERE EXTRACT(YEAR FROM o."CreatedAt")::integer = p_year
      AND o."PaymentStatus" = 'PAID'
      AND o."OrderStatus" <> 'CANCELLED'
    GROUP BY EXTRACT(MONTH FROM o."CreatedAt")
    ORDER BY EXTRACT(MONTH FROM o."CreatedAt");
$$;

CREATE OR REPLACE FUNCTION "fn_GetCustomerOrderHistory"(p_user_id integer)
RETURNS TABLE(order_id integer, order_date timestamptz, total_price numeric, order_status varchar)
LANGUAGE sql STABLE AS $$
    SELECT o."Id"::integer, o."CreatedAt", o."TotalPrice", o."OrderStatus"
    FROM "Orders" o
    WHERE o."UserId" = p_user_id
    ORDER BY o."CreatedAt" DESC;
$$;
