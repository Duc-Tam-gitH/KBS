# Địa chỉ và thanh toán

## Phạm vi đã chốt
- Chỉ hỗ trợ COD và chuyển khoản ngân hàng (BANKING).
- Ba dropdown tỉnh/huyện/xã: provinces.open-api.vn v1 theo yêu cầu người dùng.
- VietQR Quick Link, nhân viên xác nhận chuyển khoản (thay yêu cầu đối soát tự động).
- Không tạo bảng/cột; không triển khai PaymentTransactions hoặc webhook ngân hàng.

## Mapping và contracts
| UI / dữ liệu | Nguồn |
|---|---|
| Tỉnh, huyện, xã | Open API v1; ngoại lệ nguồn ngoài DB do người dùng yêu cầu |
| Địa chỉ hoàn chỉnh | Số nhà/đường + tên xã/huyện/tỉnh → Orders.ShippingAddress |
| Số tiền thanh toán | Orders.TotalPrice, tính từ Products.Price trong OrderService |
| Mã nội dung chuyển khoản | KBS + Orders.Id |
| Ngân hàng, tài khoản, tên thụ hưởng | Payments:Bank trong User Secrets do chủ cửa hàng cung cấp |
| Phương thức, trạng thái | Orders.PaymentMethod / PaymentStatus |
| Nhật ký nhân viên xác nhận | OrderNotes.OrderId / StaffId / Content / CreatedAt |

- GET /api/payments/options: chỉ trả cờ sẵn sàng, không trả secret.
- POST /api/payments/orders/{id}: chủ đơn tạo thông tin thanh toán, không nhận số tiền từ client.
- GET /api/payments/orders/{id}: chủ đơn xem trạng thái từ DB.
- PUT /api/payments/orders/{id}/bank-confirmation: Admin/Staff; ghi PAID và OrderNotes cùng transaction.

- POST /api/orders: chỉ nhận COD/BANKING, kiểm tra cấu hình ngân hàng trước khi tạo đơn BANKING.

Models chứa DTO/options; BLL truy vấn DAL và tạo Quick Link; Controller kiểm tra quyền.
React hiển thị trạng thái từ server.

## Tiêu chí kiểm thử trước triển khai
- Đổi tỉnh/huyện xóa giá trị cấp dưới; chỉ fetch cấp tiếp theo khi đã chọn cha; abort phản hồi cũ.
- Không submit khi địa chỉ thiếu cấp, API lỗi, hoặc thiếu cấu hình phương thức thanh toán.
- QR dùng số tiền DB và đúng mã đơn; người khác không xem/tạo thanh toán của đơn.
- Xác nhận ngân hàng chỉ đúng phương thức/trạng thái, có xác nhận UI và audit nhân viên;
  gọi lại không ghi nhật ký lần hai, không thay đổi kho.
- Checkout chỉ có hai phương thức; API từ chối phương thức không hỗ trợ.
- Test bằng DB InMemory; không thực hiện giao dịch thật.

## Tài liệu nguồn
- https://provinces.open-api.vn/
- https://www.vietqr.io/qr-dong

## Cấu hình và chạy

Thông tin tài khoản MBBank người dùng cung cấp đã lưu trong User Secrets trên máy phát triển,
không commit số tài khoản vào appsettings. API cần khởi động lại để nạp cấu hình mới.
Quick Link dùng ảnh SVG theo tài liệu VietQR hiện tại; dữ liệu tài khoản/số tiền/mã đơn được gửi
tới img.vietqr.io khi khách mở ảnh QR. QR không phải bằng chứng đã chuyển tiền.

Khi chạy trên máy khác, cấu hình Payments:Bank:BankBin, BankName, AccountNumber và AccountName.

Luồng kiểm tra sau cấu hình:
1. Checkout: chọn tỉnh → huyện → xã, nhập địa chỉ cụ thể.
2. BANKING: đặt đơn → QR đúng số tiền/nội dung KBS{id}.
3. Staff → Quản lý đơn hàng → chọn đơn → kiểm tra tài khoản ngân hàng → xác nhận nhận chuyển khoản.
   Admin cũng xác nhận tại Dashboard → Đơn hàng → chi tiết đơn. Lịch sử xác nhận nằm trong ghi chú nội bộ.
4. Khách bấm Kiểm tra trạng thái để tải trạng thái từ DB.
5. Có thể mở lại thanh toán tại Đơn hàng của tôi → Thông tin giao nhận và thanh toán → Tiếp tục thanh toán.

Chưa có chức năng hoàn tiền hay sổ giao dịch riêng; không tạo bảng PaymentTransactions.
