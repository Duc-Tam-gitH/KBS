# Quản lý kho Admin

## Mapping

| UI | Nguồn |
|---|---|
| Mã sản phẩm, tên | Products.Id, Name |
| Danh mục | Products.CategoryId → Categories.Id, Name |
| Đang bán/ngừng bán | Products.IsActive |
| Tồn khả dụng, ngưỡng, cập nhật | Inventories.StockQuantity, LowStockThreshold, UpdatedAt |
| Đã đặt | SUM OrderDetails.Quantity với Orders.OrderStatus thuộc PENDING, CONFIRMED, SHIPPING |
| Thống kê | Đếm danh sách tồn kho API trả về, chia hết hàng (0), sắp hết (0 < tồn <= ngưỡng), còn hàng (tồn > ngưỡng) |
| Lịch sử | InventoryMovements.Delta, QuantityBefore, QuantityAfter, Reason, CreatedAt, OrderId |
| Người thao tác | InventoryMovements.ActorId → Users.Id, FullName/Username |

Mã sản phẩm thay SKU/mã vạch theo phạm vi đã duyệt. Không có giá nhập hoặc kho theo địa điểm trong schema,
không hiển thị các trường này. Không xóa tồn kho hay lịch sử.
Đơn hàng đã trừ tồn khi tạo: cột đã đặt chỉ tham khảo, không trừ lần hai.

## API / trách nhiệm

Tái sử dụng API /api/staff với quyền Admin,Staff:
- GET /inventory → StaffInventoryDto[]; bổ sung categoryId, categoryName, orderedQuantity (long).
- GET /inventory/{id}/movements → InventoryMovementDto[].
- POST /inventory/{id}/adjustments: { delta, expectedQuantity, reason } → 204.
- PUT /inventory/{id}/threshold: { lowStockThreshold } → 204.

StaffService đọc dữ liệu và xử lý nghiệp vụ; Controller giữ xác thực/validation.
DAL/schema không đổi. Frontend dùng api.staff hiện có, lọc/phân trang từ danh sách thật.
Nhập/xuất dùng delta dương/âm; điều chỉnh tồn thực tế tính delta = mục tiêu - tồn đọc được.
Backend kiểm tra expectedQuantity, chặn âm/tràn số, ghi tồn và lịch sử trong transaction Serializable.
409 khi dữ liệu cũ/tranh chấp; khách hàng không có quyền đọc/ghi.

Hàng loạt: xác nhận số trước/sau, xử lý từng sản phẩm, báo từng lỗi; không atomic cả lô.
CSV xuất danh sách đã lọc (mọi trang), UTF-8 BOM, quote/escape trường và vô hiệu hóa công thức spreadsheet.
Không thêm package hoặc dữ liệu demo vào UI.

## Kiểm thử

StaffWorkflowTests: Admin đọc danh mục/số đã đặt đúng; nhập/xuất ghi actor, trước/sau;
chặn xuất âm và request tồn cũ; OrderServiceTests kiểm tra trừ/hoàn tồn theo đơn.
node frontend/scripts/inventory-smoke.mjs: bộ lọc, trạng thái/ngưỡng, form, số nguyên/overflow,
điều chỉnh, CSV, empty/loading và palette. Tests backend dùng EF InMemory,
không thay thế kiểm thử tranh chấp PostgreSQL.
