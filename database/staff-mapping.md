# Staff: ánh xạ UI và schema

## Dữ liệu hiện có

| UI | Nguồn | Quan hệ / cách tính |
|---|---|---|
| Nhân viên đăng nhập | Users.Id, FullName, Username, RoleId; Roles.Name | Users.RoleId → Roles.Id |
| Đơn chờ xử lý / đang xử lý | Orders.OrderStatus | Đếm PENDING / CONFIRMED |
| Đơn hàng / giao nhận / thanh toán | Orders.Id, CreatedAt, CustomerName, PhoneNumber, ShippingAddress, TotalPrice, PaymentMethod, PaymentStatus, OrderStatus | Orders.UserId → Users.Id |
| Dòng hàng / hóa đơn | OrderDetails.ProductId, Quantity, UnitPrice; Products.Name | OrderId → Orders.Id, ProductId → Products.Id; đơn giá lúc mua |
| Sản phẩm / tồn kho | Products.Id, Name, Description, Price, IsActive; Inventories.StockQuantity, UpdatedAt | Inventories.ProductId → Products.Id, unique |
| Khách hàng | Users.Id, Username, FullName, Email, PhoneNumber, CreatedAt, IsActive | Chỉ Roles.Name = Customer |
| Lịch sử mua / chi tiêu | Orders.UserId, OrderStatus, TotalPrice | Chi tiêu = tổng đơn COMPLETED; không cộng đơn đã hủy |
| Đánh giá của khách | ProductReviews.UserId, ProductId, Rating, Comment, CreatedAt | FK đến Users, Products |

Tên và ảnh sản phẩm là dữ liệu hiện tại. Giá và thông tin giao nhận là snapshot trên đơn.
Không bịa SKU, avatar, địa chỉ tài khoản, hiện diện online, lịch ca cố định hoặc trạng thái hoàn trả.
Các nhãn tiếng Việt chỉ dịch mã trạng thái; giá trị và bộ đếm lấy từ database.

## Schema mở rộng đã được người dùng duyệt

| UI | Bảng/cột đề xuất | Ràng buộc |
|---|---|---|
| Lịch sử ca đã lưu (không còn dùng trên Dashboard) | StaffShifts: Id, StaffId, StartedAt, EndedAt | Giữ dữ liệu hiện có; không còn API bắt đầu/kết thúc ca |
| Mã vận đơn | Orders.TrackingNumber | Nullable; tối đa 100 ký tự |
| Ghi chú đơn nội bộ | OrderNotes: Id, OrderId, StaffId, Content, CreatedAt | FK Orders và Users; chỉ Admin/Staff đọc |
| Cảnh báo tồn thấp | Inventories.LowStockThreshold | Không âm; mặc định 5 theo ngưỡng backend hiện có |
| Biến động kho | InventoryMovements: Id, ProductId, ActorId, OrderId, Delta, QuantityBefore, QuantityAfter, Reason, CreatedAt | FK Products, Users, Orders; lưu cùng transaction thay đổi tồn |
| Hội thoại 1-1 | ChatConversations: Id, CustomerId, StaffId, CreatedAt | FK Users; một hội thoại/khách; nhân viên được phân công hoặc Admin truy cập |
| Nội dung / chưa đọc | ChatMessages: Id, ConversationId, SenderId, Content, CreatedAt, ReadAt | FK hội thoại, Users; không được tự nhận là người gửi khác |
| Tin chưa đọc | ChatMessages.ReadAt, SenderId + ChatConversations.StaffId, CustomerId | Đếm tin khách chưa đọc trong hội thoại được giao cho nhân viên |

Người dùng đã duyệt toàn bộ schema trên trong phiên làm việc ngày 24/10/2026.

## Ranh giới triển khai

- Models: entity và DTO; không chứa truy cập HTTP hoặc trạng thái UI.
- DAL: DbContext, quan hệ FK, unique/check constraints, migration PostgreSQL.
- BLL: kiểm tra chủ thể, quyền sở hữu hội thoại, chuyển trạng thái, transaction tồn kho + log.
- Controller: JWT/role, validation request, HTTP status; không tin ActorId/SenderId từ client.
- React: hiển thị response, trạng thái tải/lỗi/rỗng, form; không tự tạo bản ghi giả.

## API contract

Tất cả endpoint bên dưới cần JWT. `/api/staff/*` chỉ Admin/Staff; `/api/chat/*` kiểm tra vai trò và người tham gia.
GET thành công trả 200; mutation không có body response trả 204; open chat trả 200 `{id}`.
400: dữ liệu sai; 401: chưa đăng nhập/tài khoản khóa; 403: sai quyền; 404: không tồn tại;
409: xung đột dữ liệu, hội thoại đã được nhận hoặc tồn kho đã thay đổi.

| Endpoint | Request | Response / nguồn |
|---|---|---|
| GET staff/summary | — | newOrdersCount, processingOrdersCount, lowStockCount, unreadMessagesCount, unassignedConversations, recentMovements |
| GET staff/inventory | — | productId, name, description, price, isActive, stockQuantity, lowStockThreshold, updatedAt |
| GET staff/inventory/{id}/movements | — | id, actorId, actorName, orderId, delta, quantityBefore, quantityAfter, reason, createdAt |
| POST staff/inventory/{id}/adjustments | StockAdjustmentRequest: delta != 0, expectedQuantity >= 0, reason 1..500 ký tự | Transaction tồn kho + log; chống cập nhật trên số tồn cũ |
| PUT staff/inventory/{id}/threshold | ThresholdRequest: lowStockThreshold >= 0 | Ngưỡng lưu tại Inventories |
| GET staff/customers | — | id, username, fullName, email, phoneNumber, createdAt, isActive, totalSpent, orderCount |
| GET staff/customers/{id}/orders | — | OrderResponseDto[]; chỉ khách Customer |
| GET staff/customers/{id}/reviews | — | id, productId, rating, comment, createdAt |
| PUT staff/customers/{id}/status | UpdateUserStatusDto: isActive | Không được khóa Admin/Staff |
| GET staff/orders/{id}/operations | — | trackingNumber, notes {id,content,createdAt,staffName}[]; không đưa ghi chú vào API khách |
| PUT staff/orders/{id}/tracking | TrackingRequest: trackingNumber nullable, <=100 ký tự | Orders.TrackingNumber |
| POST staff/orders/{id}/notes | TextRequest: content 1..2000 ký tự | Thêm ghi chú; không ghi đè lịch sử |
| GET chat/conversations | — | id, customerId, customerName, staffId, staffName, lastMessage, lastMessageAt, unread |
| POST chat/conversations | Query customerId cho Staff/Admin; khách chỉ mở hội thoại của mình | {id}; dùng lại hội thoại đã có |
| PUT chat/conversations/{id}/claim | — | Nhân viên tiếp nhận hội thoại chưa có người phụ trách |
| GET chat/conversations/{id}/messages | Query beforeId hoặc afterId tùy chọn | Tối đa 100 tin tăng dần; afterId lấy tin mới không bỏ khoảng giữa; id,senderId,senderName,content,createdAt,readAt |
| POST chat/conversations/{id}/messages | ChatSendRequest: content 1..4000 ký tự | SenderId từ JWT |
| PUT chat/conversations/{id}/read | ChatReadRequest: throughMessageId > 0 | Chỉ đánh dấu tin bên kia đã thực sự tải đến cursor |

Chat dùng polling 5 giây, dừng khi rời trang, không thêm package. In hóa đơn dùng print CSS và dữ liệu đơn thật.
Phiếu in này là bản đơn hàng bán hàng, không phải tích hợp phát hành hóa đơn điện tử.

## Tiêu chí kiểm thử trước khi hoàn thiện từng module

1. Dashboard: đếm tin khách chưa đọc trong hội thoại được giao, không phụ thuộc ca trực; API bắt đầu/kết thúc ca trả 404.
2. Đơn: ghi chú không xuất hiện trong API khách; chuyển sai trạng thái bị từ chối; hủy đơn hoàn kho và ghi log một lần.
3. Kho: xuất quá tồn/zero delta/lý do trống bị từ chối; expectedQuantity cũ trả conflict; tồn và log cùng transaction.
4. Khách: Staff chỉ khóa Customer; Customer không gọi được API Staff; khóa chặn token hiện có.
5. Chat: khách không đọc/gửi/đánh dấu đã đọc hội thoại người khác; Staff không truy cập hội thoại đã giao người khác;
   claim không ghi đè nhân viên; gửi trống bị từ chối; cursor đã đọc không làm mất tin mới chưa tải.
6. Frontend: cả 5 trang build được; không còn TS trong JSX/mock/router chưa cài; có loading/error/retry/empty;
   đổi lựa chọn không trộn dữ liệu, gửi lỗi giữ nội dung; mobile và print CSS dùng palette token.
7. Migration: model khớp snapshot; script SQL tạo đúng 5 bảng/2 cột và không seed dữ liệu nghiệp vụ.
