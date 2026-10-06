using KBS.DAL.Data;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KBS.DAL.Migrations
{
    /// <inheritdoc />
    public partial class AddSeedViewsAndProcedures : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Nạp seed data
            migrationBuilder.Sql(SqlResourceReader.Read("KBS.DAL.Sql.seed_data.sql"));

            // Tạo view
            migrationBuilder.Sql(SqlResourceReader.Read("KBS.DAL.Sql.views.sql"));

            // Tạo procedure/function/trigger
            //
            // Trigger được tạo sau cùng để không can thiệp vào các câu INSERT
            // của seed data ở phía trên.
            migrationBuilder.Sql(SqlResourceReader.Read("KBS.DAL.Sql.procedures.sql"));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // CẢNH BÁO: hạ cấp migration này sẽ xóa toàn bộ dữ liệu trong tám bảng
            // dữ liệu mẫu (OrderDetails, Orders, Inventories, Products, Users, Roles,
            // Categories, Brands). Hãy sao lưu trước khi chạy Down.
            //
            // Thứ tự thực hiện: gỡ trigger trước hàm, rồi procedure và view;
            // sau đó xóa dữ liệu theo chiều khóa ngoại để không vi phạm ràng buộc.
            migrationBuilder.Sql("""
                DROP TRIGGER IF EXISTS "trg_products_auto_inventory" ON "Products";
                DROP TRIGGER IF EXISTS "trg_inventories_updated_at" ON "Inventories";
                DROP TRIGGER IF EXISTS "trg_orders_validate_status" ON "Orders";
                DROP TRIGGER IF EXISTS "trg_orderdetails_stock_check" ON "OrderDetails";

                DROP FUNCTION IF EXISTS "fn_products_auto_inventory"();
                DROP FUNCTION IF EXISTS "fn_inventories_set_updated_at"();
                DROP FUNCTION IF EXISTS "fn_orders_validate_status_transition"();
                DROP FUNCTION IF EXISTS "fn_orderdetails_stock_check"();

                DROP PROCEDURE IF EXISTS "sp_CreateOrder"(bigint, varchar, varchar, text, varchar, jsonb);
                DROP PROCEDURE IF EXISTS "sp_UpdateOrderStatus"(bigint, varchar);
                DROP PROCEDURE IF EXISTS "sp_UpdateStock"(bigint, integer);
                DROP PROCEDURE IF EXISTS "sp_RegisterUser"(varchar, varchar, varchar, varchar, varchar, varchar);
                DROP PROCEDURE IF EXISTS "sp_CancelOrder"(integer, text);

                DROP FUNCTION IF EXISTS "fn_GetRevenueByDay"(date, date);
                DROP FUNCTION IF EXISTS "fn_GetTopSellingProducts"(integer, date, date);
                DROP FUNCTION IF EXISTS "fn_GetLowStockProducts"(integer);
                DROP FUNCTION IF EXISTS "fn_GetMonthlyRevenue"(integer);
                DROP FUNCTION IF EXISTS "fn_GetCustomerOrderHistory"(bigint);

                DROP VIEW IF EXISTS "vw_ProductFull";
                DROP VIEW IF EXISTS "vw_OrderSummary";
                DROP VIEW IF EXISTS "vw_RevenueByDay";
                DROP VIEW IF EXISTS "vw_RevenueByMonth";
                DROP VIEW IF EXISTS "vw_TopSellingProducts";
                DROP VIEW IF EXISTS "vw_LowStockProducts";
                DROP VIEW IF EXISTS "vw_ProductByBrand";

                DELETE FROM "OrderDetails";
                DELETE FROM "Orders";
                DELETE FROM "Inventories";
                DELETE FROM "Products";
                DELETE FROM "Users";
                DELETE FROM "Roles";
                DELETE FROM "Categories";
                DELETE FROM "Brands";
                """);
        }
    }
}
