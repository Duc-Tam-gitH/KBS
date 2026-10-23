# KBS — Keyboard Beyond Standard

Ứng dụng thương mại điện tử bàn phím dùng React/Vite, ASP.NET Core 9, EF Core 9 và PostgreSQL 16.

## Chạy từ đầu

Cần .NET SDK 9, Node.js 20+, PostgreSQL 16 và `psql`.

```powershell
createdb -U postgres KBS_DB
dotnet user-secrets set "ConnectionStrings:KBSConnection" "Host=localhost;Port=5432;Database=KBS_DB;Username=postgres;Password=10052005" --project .\backend\KBS.API
dotnet ef database update --project .\backend\KBS.DAL --startup-project .\backend\KBS.API
psql -U postgres -d KBS_DB -f .\database\seed_demo.sql
psql -U postgres -d KBS_DB -f .\database\reporting.sql
dotnet run --project .\backend\KBS.API --launch-profile https

# terminal khác
Copy-Item .\frontend\.env.example .\frontend\.env.local
npm install --prefix .\frontend
npm run dev --prefix .\frontend
```

Frontend: `http://localhost:5173`. OpenAPI: `https://localhost:7013/openapi/v1.json` (môi trường Development); import URL này vào Postman/Insomnia để thử endpoint.

## Tài khoản demo

| Vai trò | Tài khoản | Mật khẩu |
|---|---|---|
| Admin | `admin` | `Admin@123` |
| Customer | `minh.demo`, `lan.demo` | `Demo@123` |

Đổi AdminBootstrap/JWT bằng User Secrets hoặc biến môi trường trước khi deploy; tài khoản demo chỉ dùng local.

## API chính

| Endpoint | Quyền |
|---|---|
| `POST /api/auth/register`, `POST /api/auth/login` | Public |
| `GET /api/products`, `GET /api/products/{id}` | Public |
| `POST/PUT /api/products` | Admin |
| `POST /api/orders`, `GET /api/orders/mine` | Đăng nhập |
| `GET /api/orders`, `PUT /api/orders/{id}/status` | Admin |
| `GET /api/admin/dashboard` | Admin |

Login trả access token; gửi `Authorization: Bearer <token>`. Ảnh nằm trong mảng `images: [{ imageUrl, altText, sortOrder }]` của request tạo/sửa product. Trạng thái đơn hợp lệ: `PENDING → CONFIRMED → SHIPPING → COMPLETED`, hoặc hủy ở `PENDING`/`CONFIRMED`; hủy đơn hoàn tồn kho.

## Kiểm tra và kịch bản demo

```powershell
dotnet test .\backend\KBS.Tests
npm run build --prefix .\frontend
```

1. Mở frontend ở desktop/mobile: tìm kiếm, lọc, loading, empty/error state.
2. Đăng nhập Customer trên OpenAPI, tạo đơn một sản phẩm; xác nhận `totalPrice` do server tính.
3. Tạo quantity lớn hơn tồn kho và xác nhận `409 Conflict`, tồn kho không đổi.
4. Với token Customer gọi dashboard để nhận `403`; token Admin có thể xem dashboard và chuyển trạng thái đơn.
5. Chạy `SELECT * FROM vw_daily_revenue;` và `SELECT * FROM vw_low_inventory;` để demo báo cáo.

Xem [tài liệu Quản trị dữ liệu](database/README.md) để có ERD, 3NF, dữ liệu mẫu, backup/restore và quyền DB.
