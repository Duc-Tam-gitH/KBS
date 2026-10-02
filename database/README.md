# Cơ sở dữ liệu PostgreSQL KBS88

SQL thuần trong thư mục này là nguồn chuẩn cho schema của KBS88. EF Core chỉ
nên scaffold từ database đã tạo, không tạo một schema song song bằng migration.

## Yêu cầu

- PostgreSQL 14 trở lên và lệnh `psql`.
- Tài khoản PostgreSQL có quyền tạo database và đối tượng trong KBS88.
- Không đưa mật khẩu, file `.env` hoặc connection string có `Password=...` vào Git.

## Thứ tự thực thi

Mở terminal tích hợp của VS Code hoặc PowerShell tại thư mục `database`. Dùng
`-v ON_ERROR_STOP=1` để `psql` dừng ngay khi có lỗi.

```powershell
psql -v ON_ERROR_STOP=1 -h localhost -U postgres -d postgres -f .\01_create_database.sql
psql -v ON_ERROR_STOP=1 -h localhost -U postgres -d KBS88 -f .\02_tables.sql
psql -v ON_ERROR_STOP=1 -h localhost -U postgres -d KBS88 -f .\03_constraints.sql
psql -v ON_ERROR_STOP=1 -h localhost -U postgres -d KBS88 -f .\04_seed_data.sql
psql -v ON_ERROR_STOP=1 -h localhost -U postgres -d KBS88 -f .\05_views.sql
psql -v ON_ERROR_STOP=1 -h localhost -U postgres -d KBS88 -f .\06_procedures.sql
```

`01_create_database.sql` có lệnh dành riêng cho `psql`; hãy chạy file này bằng
`psql`, không dùng trình chạy SQL chung. Script tạo KBS88 khi chưa tồn tại với
UTF-8 và locale `C`, phù hợp giữa các hệ điều hành.

`07_demo_procedures.sql` là tùy chọn và chỉ dùng để kiểm thử:

```powershell
psql -v ON_ERROR_STOP=1 -h localhost -U postgres -d KBS88 -f .\07_demo_procedures.sql
```

Mọi script có thể chạy lặp lại. Demo luôn kết thúc bằng `ROLLBACK`, nên không
lưu đơn hàng thử nghiệm.

## Kiểm tra sau khi chạy

```powershell
psql -h localhost -U postgres -d KBS88
```

Trong phiên `psql`, chạy:
    
```sql
SELECT COUNT(*) AS "Total",
       COUNT(DISTINCT "Name") AS "DistinctNames"
FROM "Products";

SELECT * FROM "vw_LowStockProducts";
SELECT * FROM "fn_GetTopSellingProducts"(5, current_date - 180, current_date);
```

Sau dữ liệu mẫu chuẩn, hai cột đếm sản phẩm đều phải là `40`.

## Nội dung script

- `01_create_database.sql`: tạo KBS88 có điều kiện.
- `02_tables.sql`: tạo các bảng Categories, Brands, Products, Inventories, Roles, Users, Orders và OrderDetails.
- `03_constraints.sql`: tạo ràng buộc, khóa ngoại, kiểm tra dữ liệu và index.
- `04_seed_data.sql`: thêm dữ liệu mẫu bàn phím bằng UTF-8.
- `05_views.sql`: tạo view báo cáo sản phẩm, đơn hàng, doanh thu, tồn kho và thương hiệu.
- `06_procedures.sql`: tạo procedure, function báo cáo và trigger.
- `07_demo_procedures.sql`: kiểm thử API database mà không lưu thay đổi.

## Đối tượng database

Views: `vw_ProductFull`, `vw_OrderSummary`, `vw_RevenueByDay`,
`vw_RevenueByMonth`, `vw_TopSellingProducts`, `vw_LowStockProducts`,
và `vw_ProductByBrand`.

Procedures: `sp_CreateOrder`, `sp_UpdateOrderStatus`, `sp_UpdateStock`,
`sp_RegisterUser` và `sp_CancelOrder`.

Functions: `fn_GetRevenueByDay`, `fn_GetTopSellingProducts`,
`fn_GetLowStockProducts`, `fn_GetMonthlyRevenue` và
`fn_GetCustomerOrderHistory`.

Triggers: `trg_products_auto_inventory`, `trg_inventories_updated_at`,
`trg_orders_validate_status` và `trg_orderdetails_stock_check`.

## Lệnh ví dụ

```sql
CALL "sp_UpdateStock"(1, 5);
CALL "sp_UpdateOrderStatus"(1, 'CONFIRMED');
SELECT * FROM "fn_GetRevenueByDay"(current_date - 30, current_date);
SELECT * FROM "fn_GetLowStockProducts"(10);
```

## Kết nối backend

Chỉ dùng connection string qua biến môi trường hoặc User Secrets. Ví dụ tạm
thời cho một phiên PowerShell:

```powershell
$env:ConnectionStrings__DefaultConnection = "Host=localhost;Port=5432;Database=KBS88;Username=postgres;Password=MAT_KHAU_CUA_BAN"
dotnet run --project ..\backend\KBS.API
```

## EF Core scaffolding

```powershell
dotnet ef dbcontext scaffold "Host=localhost;Database=KBS88;Username=postgres;Password=MAT_KHAU_CUA_BAN" Npgsql.EntityFrameworkCore.PostgreSQL --output-dir Models --context-dir Data --context KbsDbContext --project ..\backend\KBS.DAL --startup-project ..\backend\KBS.API --force
```
