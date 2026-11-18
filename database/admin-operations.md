# Vận hành Admin sau khi bỏ Staff

| UI | API/DTO | Nguồn DB | Rỗng/null |
|---|---|---|---|
| Danh sách đơn, lọc trạng thái | GET /api/orders, OrderResponseDto | Orders.Id/CustomerName/PhoneNumber/TotalPrice/OrderStatus | Không có đơn phù hợp |
| Chi tiết và sản phẩm | OrderResponseDto.OrderDetails | Orders, OrderDetails, Products | Chọn đơn; thông báo khi không có chi tiết |
| Cập nhật trạng thái | PUT /api/orders/{id}/status | Orders.OrderStatus; Inventories và InventoryMovements khi hủy | Chỉ hiện trạng thái kế tiếp hợp lệ |
| Mã vận đơn, ghi chú | /api/staff/orders/{id}/operations, /tracking, /notes | Orders.TrackingNumber, OrderNotes, Users | Chưa có mã/ghi chú |
| Danh sách chat | GET /api/chat/conversations | ChatConversations, ChatMessages, Users | Không có hội thoại phù hợp |
| Lịch sử, gửi, đọc tin | /api/chat/conversations/{id}/messages, /read | ChatMessages.Content/SenderId/CreatedAt/ReadAt | Chọn hội thoại hoặc chưa có tin |

API vận hành chỉ chấp nhận Admin. Chat chấp nhận Admin và Customer; khách chỉ được truy cập hội thoại của mình.
Admin xem và trả lời được mọi hội thoại, kể cả hội thoại một Admin khác đã nhận.
Tên cột StaffId được giữ cho tương thích lịch sử; người tiếp nhận mới là Admin.
Không tạo tin nhắn, đơn hàng hoặc tài khoản mẫu trong luồng ứng dụng.

Migration RetireStaffRole chuyển Staff/Nhân viên sang Customer, bỏ phân công chat cũ,
kết thúc ca đang mở, xóa role cũ. Lịch sử tin nhắn và tác giả ghi chú không đổi.
Migration chạy trong transaction; Down yêu cầu khôi phục backup vì không thể suy ra phân công cũ.

Kiểm thử: StaffWorkflowTests kiểm tra Admin quản lý đơn của nhiều khách, chuyển trạng thái hợp lệ,
chat và quyền riêng tư; AuthenticationTests chặn Staff cũ; AccountManagementTests chặn cấp lại Staff.
OrderServiceTests kiểm tra hoàn kho khi hủy và không hoàn kho trùng.
Frontend smoke kiểm tra menu, khôi phục trang Admin orders/chat và loại bỏ route Staff cũ.
