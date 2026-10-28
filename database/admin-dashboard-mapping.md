# Dashboard quản trị

## Thiết kế và API

- `AdminWorkspace.jsx`: sidebar trái có lời chào và menu mở từng file Dashboard,
  AccountManagement, ProductManagement, CategoryManagement, InventoryManagement,
  ShopManagement, AIManagement. Sáu file quản lý hiện chỉ có thông báo chưa triển khai.
- `Dashboard.jsx`: Tổng quan, Doanh thu, Đơn hàng là bộ chọn dữ liệu trong vùng giữa;
  sidebar phải hiển thị chỉ số/ngày/đơn được chọn.
- Admin đăng nhập vào thẳng workspace, không render giao diện customer/nhân viên,
  không tải giỏ hàng. Chỉ có đăng xuất, không có nút về cửa hàng.
- `GET /api/admin/dashboard` (Admin): dùng nguyên `DashboardResponseDto` gồm
  `revenue`, `totalOrders`, `pendingOrders`, `totalCustomers`, `lowStockProducts`,
  `revenueLast7Days: [{ date, revenue, orders }]`.
- `GET /api/orders` (Admin/Staff): dùng nguyên `OrderResponseDto[]`, chỉ tải khi mở tab Đơn hàng.
- Tái sử dụng luồng Controller → BLL service → KbsDbContext → entities; không thêm bảng/cột,
  DTO hay lớp DAL mới. Client gửi Bearer token qua service API hiện có.
- Biểu đồ giữ cách tính của API: nhóm theo ngày tạo đơn UTC, doanh thu chỉ gồm đơn PAID;
  đây không phải ngày thực nhận thanh toán. Ngày không có bản ghi không tự sinh số liệu.

## Ánh xạ dữ liệu

| Vị trí UI | Nguồn DB | API / cách tính |
|---|---|---|
| Lời chào | Users.FullName / Users.Username | user của phiên đăng nhập |
| Doanh thu | Orders.TotalPrice, Orders.PaymentStatus | revenue: SUM cho PAID, toàn thời gian |
| Tổng đơn | Orders.Id | totalOrders: COUNT |
| Đơn chờ | Orders.OrderStatus | pendingOrders: COUNT cho PENDING |
| Khách hàng | Users.Id, Users.RoleId → Roles.Id, Roles.Name | totalCustomers: COUNT vai trò Customer |
| Sắp hết hàng | Inventories.ProductId, StockQuantity, LowStockThreshold | lowStockProducts: COUNT tồn <= ngưỡng |
| Ngày biểu đồ / chi tiết ngày | Orders.CreatedAt | revenueLast7Days.date, UTC |
| Doanh thu ngày | Orders.TotalPrice, PaymentStatus, CreatedAt | revenueLast7Days.revenue |
| Số đơn ngày | Orders.Id, CreatedAt | revenueLast7Days.orders |
| Mã đơn / lựa chọn | Orders.Id (PK) | id |
| Người nhận, điện thoại, địa chỉ | Orders.CustomerName, PhoneNumber, ShippingAddress | customerName, phoneNumber, shippingAddress |
| Ngày đặt | Orders.CreatedAt | createdAt |
| Tổng tiền | Orders.TotalPrice | totalPrice |
| Trạng thái | Orders.OrderStatus, PaymentStatus | orderStatus, paymentStatus |
| Phương thức thanh toán | Orders.PaymentMethod | paymentMethod |
| Sản phẩm trong chi tiết | OrderDetails.ProductId → Products.Id, Products.Name | orderDetails.productName |
| Số lượng / đơn giá | OrderDetails.Quantity, UnitPrice | orderDetails.quantity, unitPrice |

Orders.UserId → Users.Id; OrderDetails.OrderId → Orders.Id;
Inventories.ProductId → Products.Id là quan hệ một-một. OrderDetails.Id là PK từng dòng hàng.
Không hiển thị avatar, ví, thẻ tín dụng, chuyển khoản cá nhân, lợi nhuận, followers,
projects hay Sage vì schema/API chưa cung cấp các dữ liệu này.

## Tiêu chí kiểm tra (trước triển khai)

- JSX hợp lệ; route admin sử dụng Dashboard mới; chỉ Admin được tải dữ liệu.
- Loading, lỗi có thử lại, danh sách rỗng có thông báo; lỗi không biến thành số 0.
- Đổi tab và chọn chỉ số/ngày/đơn cập nhật nội dung cùng chi tiết bên phải.
- Bỏ phản hồi API cũ sau đổi tab, làm mới hoặc rời trang.
- CSS chỉ dùng token palette hiện có; ba cột desktop, xếp dọc trên mobile.
- Không còn dữ liệu mẫu / ảnh placeholder; build, lint và smoke test chạy được.
