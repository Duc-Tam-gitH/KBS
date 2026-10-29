# Quản trị dữ liệu — KBS

Tài liệu này mô tả CSDL PostgreSQL của hệ thống bán bàn phím KBS. Schema gốc được EF Core quản lý trong `backend/KBS.DAL/Migrations`; các script ở thư mục này là dữ liệu demo và đối tượng báo cáo, không thay thế migration ứng dụng.

## ERD và quan hệ

```mermaid
erDiagram
    ROLES ||--o{ USERS : "RoleId"
    USERS ||--o{ ORDERS : "UserId"
    CATEGORIES ||--o{ PRODUCTS : "CategoryId"
    BRANDS ||--o{ PRODUCTS : "BrandId"
    PRODUCTS ||--|| INVENTORIES : "ProductId (unique)"
    ORDERS ||--o{ ORDER_DETAILS : "OrderId"
    PRODUCTS ||--o{ ORDER_DETAILS : "ProductId"

    ROLES { bigint Id PK \n text Name UK }
    USERS { bigint Id PK \n bigint RoleId FK \n text Username UK \n text Email UK }
    CATEGORIES { bigint Id PK }
    BRANDS { bigint Id PK }
    PRODUCTS { bigint Id PK \n bigint CategoryId FK \n bigint BrandId FK \n numeric Price }
    INVENTORIES { bigint Id PK \n bigint ProductId FK_UK \n int StockQuantity }
    ORDERS { bigint Id PK \n bigint UserId FK \n numeric TotalPrice }
    ORDER_DETAILS { bigint Id PK \n bigint OrderId FK \n bigint ProductId FK \n numeric UnitPrice \n int Quantity }
```

| Bảng | PK | FK / ràng buộc chính | Vai trò |
|---|---|---|---|
| `Roles` | `Id` | `Name` unique | Danh sách vai trò. |
| `Users` | `Id` | `RoleId → Roles`; `Username`, `Email`, `PhoneNumber` unique | Tài khoản khách/quản trị. |
| `Categories`, `Brands` | `Id` | — | Danh mục và thương hiệu. |
| `Products` | `Id` | `CategoryId`, `BrandId` | Thông tin và giá sản phẩm. |
| `ProductReviews` | `Id` | `ProductId`, `UserId`; unique `(UserId, ProductId)`; `Rating` 1–5 | Đánh giá sau mua. |
| `Inventories` | `Id` | `ProductId → Products` unique | Một bản ghi tồn kho cho một sản phẩm. |
| `Orders` | `Id` | `UserId → Users` | Header đơn hàng, snapshot giao nhận/thanh toán. |
| `OrderDetails` | `Id` | `OrderId → Orders`, `ProductId → Products` | Dòng hàng, snapshot `UnitPrice` và số lượng. |

Các FK lịch sử đơn hàng dùng `RESTRICT` để bảo toàn lịch sử bán hàng.
`ProductReviews.ProductId` dùng `CASCADE`, `UserId` dùng `RESTRICT`; nội dung tối đa 2000 ký tự.
Migration `AddProductReviews` tạo bảng và index `(ProductId, CreatedAt)` phục vụ phân trang đánh giá.
Có thể dùng `add_product_reviews.sql` thay cho EF update nếu DB đã áp dụng `AddLikelist`;
script có kiểm tra lịch sử migration để không tạo lại bảng khi chạy lần hai.

## Chuẩn hóa đến 3NF

- **1NF:** Mỗi cột là giá trị nguyên tử; sản phẩm trong đơn được tách thành một dòng `OrderDetails`, không lưu danh sách trong `Orders`.
- **2NF:** Các bảng dùng khóa đơn `Id`; thuộc tính mô tả sản phẩm, khách hàng và thương hiệu phụ thuộc hoàn toàn vào khóa của chính bảng đó. `UnitPrice` nằm tại `OrderDetails` vì phụ thuộc vào giao dịch dòng hàng.
- **3NF:** Role, category và brand được tách khỏi `Users`/`Products`, loại bỏ phụ thuộc bắc cầu kiểu `Product → BrandName`. `Orders.TotalPrice` là giá trị dẫn xuất có chủ đích để chốt chứng từ/lịch sử; nguồn tính là các dòng `OrderDetails` tại thời điểm tạo đơn, không phải giá sản phẩm hiện tại.

`CustomerName`, `PhoneNumber`, `ShippingAddress` trong `Orders` cũng là snapshot nghiệp vụ: khách đổi hồ sơ sau này không làm sai chứng từ cũ.

## Chạy dữ liệu và báo cáo

AI Management bổ sung migration `20261029043203_AddAiManagement` với hai bảng `AiModules`, `AiEvents`.
Thiết kế và ánh xạ UI: [ai-management-design.md](ai-management-design.md).
Hướng dẫn khởi động worker: [ai/README.md](../ai/README.md).

Module Staff bổ sung migration `AddStaffOperations` (5 bảng, 2 cột, đã được duyệt).
Chi tiết PK/FK, ánh xạ UI và request/response nằm tại [staff-mapping.md](staff-mapping.md).
`add_staff_operations.sql` là script idempotent từ migration `AddProductReviews` đến `AddStaffOperations`;
chỉ dùng khi DB đã có toàn bộ migrations trước đó. Có thể dùng `dotnet ef database update` thay cho script.
Sau migration, chạy lại `reporting.sql` nếu đã sử dụng các view/function báo cáo.

Sau khi chạy EF migrations, thực hiện theo thứ tự:

```powershell
psql -d KBS_DB -f .\database\seed_demo.sql
psql -d KBS_DB -f .\database\reporting.sql
```

`seed_demo.sql` tạo thương hiệu, 6 sản phẩm, tồn kho, 2 khách và 3 đơn demo. Mật khẩu hai khách demo là `Demo@123`; chỉ dùng cho môi trường phát triển. Script không ghi đè dữ liệu đã có cùng username/email hoặc tên sản phẩm.

## Backup, restore và quyền DB

Sao lưu logic hằng ngày (custom format, có thể restore chọn lọc):

```powershell
$env:PGPASSWORD = '<mat-khau-backup>'
pg_dump -h localhost -U kbs_backup -d KBS_DB -Fc -f .\backups\kbs_$(Get-Date -Format yyyyMMdd_HHmm).dump
pg_restore -l .\backups\kbs_20261019_0100.dump
pg_restore -h localhost -U kbs_owner -d KBS_DB --clean --if-exists .\backups\kbs_20261019_0100.dump
```

Giữ bản daily 14 ngày, weekly 8 tuần và monthly 12 tháng; mã hóa kho backup, kiểm tra restore vào DB tạm mỗi tháng. Không commit backup hoặc mật khẩu vào Git.

Các lệnh tạo role mẫu nằm cuối `reporting.sql`: ứng dụng dùng `kbs_app`, đọc báo cáo dùng `kbs_readonly`, còn migration/backup dùng role riêng có quyền cao hơn. Thay toàn bộ mật khẩu mẫu bằng secret của môi trường trước khi chạy.
