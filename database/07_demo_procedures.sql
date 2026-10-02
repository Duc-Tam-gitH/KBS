-- FIX: Bảo đảm các chuỗi tiếng Việt được đọc theo UTF-8.
SET client_encoding = 'UTF8';

-- Minh họa chủ động ROLLBACK để không làm thay đổi dữ liệu mẫu.
BEGIN;

DO $$
DECLARE
    -- FIX: Đồng bộ biến định danh với các khóa bigserial.
    v_user_id bigint;
    v_product_id bigint;
    v_order_id bigint;
BEGIN
    SELECT "Id" INTO v_user_id FROM "Users" WHERE "Username" = 'minhnguyen';
    SELECT "Id" INTO v_product_id FROM "Products" WHERE "Name" = 'Keychron K2 Pro';

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
    -- FIX: Tăng lại một đơn vị tồn kho chỉ để minh họa khôi phục tồn kho.
    CALL "sp_UpdateStock"(v_product_id, 1);
    RAISE NOTICE 'Moved demo order to SHIPPED and increased product % stock by 1.', v_product_id;
END;
$$;

SELECT * FROM "fn_GetRevenueByDay"(current_date - 180, current_date);
SELECT * FROM "fn_GetTopSellingProducts"(5, current_date - 180, current_date);
SELECT * FROM "fn_GetLowStockProducts"(10);
SELECT * FROM "fn_GetMonthlyRevenue"(EXTRACT(YEAR FROM current_date)::integer);
SELECT * FROM "fn_GetCustomerOrderHistory"((SELECT "Id" FROM "Users" WHERE "Username" = 'minhnguyen'));

ROLLBACK;
