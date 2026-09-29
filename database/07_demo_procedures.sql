\set ON_ERROR_STOP on

-- The demo deliberately rolls back so it does not alter seed data.
BEGIN;

DO $$
DECLARE
    v_user_id integer;
    v_product_id integer;
    v_order_id integer;
BEGIN
    SELECT "Id"::integer INTO v_user_id FROM "Users" WHERE "Username" = 'minhnguyen';
    SELECT "Id"::integer INTO v_product_id FROM "Products" WHERE "Name" = 'Keychron K2 Pro';

    CALL "sp_CreateOrder"(
        v_user_id,
        'Nguyễn Minh',
        '0901000002',
        'Demo procedure address - rollback only',
        'COD',
        jsonb_build_array(jsonb_build_object('productId', v_product_id, 'quantity', 1)),
        v_order_id);

    RAISE NOTICE 'sp_CreateOrder returned order id %', v_order_id;
    CALL "sp_UpdateOrderStatus"(v_order_id, 'CONFIRMED');
    CALL "sp_UpdateOrderStatus"(v_order_id, 'SHIPPED');
    CALL "sp_UpdateStock"(v_product_id, 1);
    RAISE NOTICE 'Moved demo order to SHIPPED and increased product % stock by 1.', v_product_id;
END;
$$;

SELECT * FROM "fn_GetRevenueByDay"(current_date - 180, current_date);
SELECT * FROM "fn_GetTopSellingProducts"(5, current_date - 180, current_date);
SELECT * FROM "fn_GetLowStockProducts"(10);
SELECT * FROM "fn_GetMonthlyRevenue"(EXTRACT(YEAR FROM current_date)::integer);
SELECT * FROM "fn_GetCustomerOrderHistory"((SELECT "Id"::integer FROM "Users" WHERE "Username" = 'minhnguyen'));

ROLLBACK;
