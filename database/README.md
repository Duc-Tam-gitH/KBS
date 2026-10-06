# Cơ sở dữ liệu PostgreSQL KBS88

## Nguồn chuẩn của schema

Toàn bộ schema, dữ liệu mẫu và đối tượng báo cáo được áp dụng **duy nhất qua EF Core
migration**. Không chạy tay các tệp `.sql` trong thư mục này để tạo schema, vì như vậy
sẽ tạo ra một schema song song với lịch sử migration.

- Bảng, ràng buộc, chỉ mục và khóa ngoại: khai báo trong `KbsDbContext.OnModelCreating`
  (`backend/KBS.DAL/Data/KbsDbContext.cs`) và sinh ra bởi migration.
- Dữ liệu mẫu, view, procedure, function và trigger: SQL thuần trong thư mục này,
  được nhúng vào `KBS.DAL` và thực thi bởi migration `AddSeedViewsAndProcedures`.

## Danh sách migration

| Thứ tự | Migration | Nội dung |
| --- | --- | --- |
| 1 | `20261006073730_InitialCreate` | 8 bảng cùng ràng buộc, chỉ mục, khóa ngoại |
| 2 | `20261006074411_AddSeedViewsAndProcedures` | Dữ liệu mẫu, 7 view, procedure/function/trigger |

Hai migration phải chạy theo đúng thứ tự này, vì migration thứ hai cần các bảng đã tồn tại.

## Yêu cầu

- PostgreSQL 14 trở lên.
- .NET SDK 9 và `dotnet-ef` 9.0.4 (`dotnet tool install --global dotnet-ef`).
- Không đưa mật khẩu, file `.env` hoặc connection string có `Password=...` vào Git.
  Dùng biến môi trường hoặc User Secrets.

## Cách áp dụng migration

Đặt connection string qua biến môi trường rồi cập nhật database:

```powershell
$env:ConnectionStrings__DefaultConnection = "Host=localhost;Port=5432;Database=KBS_DB;Username=postgres;Password=MAT_KHAU_CUA_BAN"
dotnet ef database update --project backend/KBS.DAL --startup-project backend/KBS.API
```

`KBS.API` cũng tự gọi `Database.Migrate()` khi khởi động, nên chỉ cần chạy API là
database được tạo và cập nhật:

```powershell
dotnet run --project backend/KBS.API
```

Xem trước SQL sẽ chạy mà không cần kết nối tới server:

```powershell
dotnet ef migrations script --project backend/KBS.DAL --startup-project backend/KBS.API --output migrate.sql
```

Thêm migration mới khi mô hình EF thay đổi:

```powershell
dotnet ef migrations add TenMigration --project backend/KBS.DAL --startup-project backend/KBS.API --output-dir Migrations
```

## Nội dung thư mục này

- `seed_data.sql`: dữ liệu mẫu — 12 danh mục, 15 thương hiệu, 40 sản phẩm, 40 dòng tồn
  kho, 3 vai trò, 5 người dùng, 9 đơn hàng và 22 chi tiết đơn hàng. Mọi câu lệnh đều
  có `ON CONFLICT`/`WHERE NOT EXISTS` nên chạy lặp lại không sinh dữ liệu trùng.
- `views.sql`: 7 view báo cáo.
- `procedures.sql`: procedure, function báo cáo và 4 trigger.

Ba tệp `.sql` được nhúng vào `KBS.DAL` bằng `EmbeddedResource` (xem
`backend/KBS.DAL/KBS.DAL.csproj`) và được đọc trong migration qua
`SqlResourceReader.Read("KBS.DAL.Sql.<tên tệp>")`
(`backend/KBS.DAL/Data/SqlResourceReader.cs`). Vì nội dung được nhúng lúc build,
**sửa `.sql` xong phải build lại** thì migration mới thấy nội dung mới.
Migration đã áp dụng là bất biến: muốn đổi dữ liệu mẫu hay đối tượng báo cáo sau khi
đã chạy, hãy tạo một migration mới thay vì sửa migration cũ.

Bộ tệp `01_create_database.sql` … `07_demo_procedures.sql` trước đây đã được thay thế:
việc tạo database và bảng nay do migration đảm nhiệm, còn view/procedure được gộp vào
`views.sql` và `procedures.sql`. `07_demo_procedures.sql` cũ chỉ dùng để kiểm thử và
kết thúc bằng `ROLLBACK`; nó không được đưa vào migration.

## Kiểm tra sau khi chạy

```sql
SELECT COUNT(*) AS "Total",
       COUNT(DISTINCT "Name") AS "DistinctNames"
FROM "Products";

SELECT * FROM "vw_LowStockProducts";
SELECT * FROM "fn_GetTopSellingProducts"(5, current_date - 180, current_date);
```

Sau dữ liệu mẫu chuẩn, hai cột đếm sản phẩm đều phải là `40`, và phải có `7` view,
`9` function, `5` procedure cùng `4` trigger.

## Đối tượng database

Views: `vw_ProductFull`, `vw_OrderSummary`, `vw_RevenueByDay`, `vw_RevenueByMonth`,
`vw_TopSellingProducts`, `vw_LowStockProducts`, `vw_ProductByBrand`.

Procedures: `sp_CreateOrder`, `sp_UpdateOrderStatus`, `sp_UpdateStock`,
`sp_RegisterUser`, `sp_CancelOrder`.

Functions báo cáo: `fn_GetRevenueByDay`, `fn_GetTopSellingProducts`,
`fn_GetLowStockProducts`, `fn_GetMonthlyRevenue`, `fn_GetCustomerOrderHistory`.

Functions cho trigger: `fn_products_auto_inventory`, `fn_inventories_set_updated_at`,
`fn_orders_validate_status_transition`, `fn_orderdetails_stock_check`.

Triggers: `trg_products_auto_inventory`, `trg_inventories_updated_at`,
`trg_orders_validate_status`, `trg_orderdetails_stock_check`.

Trong `procedures.sql`, các câu `DROP PROCEDURE`/`DROP FUNCTION` dùng kiểu `integer`
là có chủ đích: chúng gỡ phiên bản cũ của procedure trước khi tạo lại với khóa `bigint`,
tránh sinh ra overload. `NOTICE ... does not exist, skipping` khi chạy lần đầu là bình
thường.

## Lệnh ví dụ

```sql
CALL "sp_UpdateStock"(1, 5);
CALL "sp_UpdateOrderStatus"(1, 'CONFIRMED');
SELECT * FROM "fn_GetRevenueByDay"(current_date - 30, current_date);
SELECT * FROM "fn_GetLowStockProducts"(10);
```

## Hạ cấp migration

`Down()` của `AddSeedViewsAndProcedures` gỡ toàn bộ view, procedure, function, trigger
và **xóa sạch dữ liệu** trong 8 bảng dữ liệu mẫu; `Down()` của `InitialCreate` sau đó
xóa 8 bảng. Hãy sao lưu trước khi chạy `dotnet ef database update 0`.
