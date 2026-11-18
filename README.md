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
| `GET /api/products/{id}/reviews?rating=5&page=1` | Public; bỏ `rating` để lấy mọi mức sao |
| `POST /api/products/{id}/reviews` | Đăng nhập, đã mua trong đơn `COMPLETED`, một đánh giá/sản phẩm |
| `POST/PUT /api/products` | Admin |
| `POST /api/orders`, `GET /api/orders/mine` | Đăng nhập |
| `PUT /api/orders/{id}/receipt` | Chủ đơn; xác nhận `SHIPPING → COMPLETED`, gọi lại không thay đổi đơn |
| `GET /api/orders`, `PUT /api/orders/{id}/status` | Admin |
| `GET /api/admin/dashboard` | Admin |

Login trả access token; gửi `Authorization: Bearer <token>`. Ảnh nằm trong mảng `images: [{ imageUrl, altText, sortOrder }]` của request tạo/sửa product. Trạng thái đơn hợp lệ: `PENDING → CONFIRMED → SHIPPING → COMPLETED`, hoặc hủy ở `PENDING`/`CONFIRMED`; hủy đơn hoàn tồn kho.

## Kiểm tra và kịch bản demo

### Vận hành trong khu vực Admin

Admin → **Quản lý đơn hàng** để xem tất cả đơn, tìm kiếm/lọc, cập nhật trạng thái,
quản lý mã vận đơn, ghi chú nội bộ và in hóa đơn.
Admin → **Chat khách hàng** để xem và trả lời các hội thoại; khách vẫn sử dụng mục **Hỗ trợ**.
Chat tải lịch sử và tin mới bằng polling 5 giây, ngừng khi rời trang.

Role Staff đã bị loại khỏi quyền vận hành và danh sách cấp role.
Áp dụng migration `20261118040000_RetireStaffRole` bằng lệnh cập nhật DB ở trên:
tài khoản Staff/Nhân viên cũ chuyển thành Customer, token cũ mất hiệu lực,
hội thoại được trả về hàng chờ Admin và lịch sử tin nhắn/ghi chú được giữ nguyên.
Migration không tự nâng tài khoản thành Admin; muốn khôi phục phân công cũ cần bản backup.
Các tên kỹ thuật `StaffId`, `api/staff` được giữ để tương thích dữ liệu/API, nhưng API vận hành chỉ cho Admin.
Xem [ánh xạ dữ liệu và kiểm thử](database/admin-operations.md).

Trang chi tiết hỗ trợ liên kết `/#product=ID`, dùng ảnh/thông số/giá/tồn kho từ API,
yêu thích, giỏ hàng và sản phẩm cùng danh mục. `stockQuantity` được bổ sung vào phản hồi sản phẩm.
Đánh giá nhận `{ "rating": 5, "comment": "Nhận xét" }`, giới hạn 1–5 sao và 2000 ký tự;
GET trả điểm trung bình, số lượng theo sao và danh sách phân trang 10 đánh giá/trang.
Không seed đánh giá hoặc nội dung mẫu cho trang này.

Trang `orderProduct.jsx` thay trang lịch sử đơn cũ: lấy dữ liệu qua `GET /api/orders/mine`,
lọc trạng thái từ các đơn thực tế, xem giao nhận, xác nhận nhận hàng, mua lại và mở phần đánh giá.
API chi tiết đơn bổ sung `imageUrl` và `productIsActive` từ sản phẩm hiện tại; không thêm bảng/cột.
Mua lại dùng giá và tồn kho hiện tại, còn lịch sử hiển thị `OrderDetails.UnitPrice` lúc mua.
Xác nhận nhận hàng không tự thay đổi trạng thái thanh toán.

Ánh xạ dữ liệu trang lịch sử đơn:

| Vị trí UI | Nguồn DB | Ghi chú |
|---|---|---|
| Tên tài khoản | `Users.FullName`, `Users.Username` | Từ phiên đăng nhập do API trả về |
| Mã / ngày đơn | `Orders.Id`, `Orders.CreatedAt` | Ngày được định dạng theo locale |
| Trạng thái / menu lọc / số lượng đơn | `Orders.OrderStatus` | Dịch mã trạng thái; đếm trên đơn của người dùng |
| Bộ lọc thanh toán | `Orders.PaymentStatus` | Chỉ lấy các trạng thái có trong kết quả API |
| Tên sản phẩm | `Products.Name` qua `OrderDetails.ProductId` | Tên hiện tại, chưa có snapshot tên lúc mua |
| Ảnh sản phẩm | `ProductImages.ImageUrl`, `SortOrder` | Ảnh hiện tại; không có ảnh thì hiển thị empty state |
| Khả năng mở / mua lại / đánh giá | `Products.IsActive`, `Orders.OrderStatus` | Mua lại kiểm tra thêm tồn kho qua API |
| Đơn giá / số lượng / tiền dòng | `OrderDetails.UnitPrice`, `Quantity` | Tiền dòng = đơn giá lúc mua × số lượng |
| Thành tiền | `Orders.TotalPrice` | Tổng tiền lưu trên đơn |
| Người nhận / điện thoại / địa chỉ | `Orders.CustomerName`, `PhoneNumber`, `ShippingAddress` | Snapshot giao nhận |
| Phương thức / trạng thái thanh toán | `Orders.PaymentMethod`, `PaymentStatus` | Dịch mã từ DB |
| Xác nhận nhận hàng | `Orders.UserId`, `OrderStatus` | Backend kiểm tra chủ đơn và trạng thái |

Không hiển thị avatar, tên shop riêng, biến thể/giá gốc lịch sử, chat hoặc hoàn tiền vì schema chưa có nguồn dữ liệu.

Trước khi sử dụng phần đánh giá, cấu hình kết nối DB rồi áp dụng migration `AddProductReviews`:

```powershell
dotnet ef database update --project .\backend\KBS.DAL --startup-project .\backend\KBS.API
```

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
