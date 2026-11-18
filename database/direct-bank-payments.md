# Thanh toán QR trực tiếp vào ngân hàng

- Khách đặt đơn BANKING, KBS hiển thị VietQR ngay tại trang thanh toán; không chuyển sang SePay Gateway.
- Số tiền lấy từ Orders.TotalPrice; nội dung KBS + mã đơn. BankPayments lưu tài khoản nhận của từng QR.
- Admin → Thanh toán: nhập ngân hàng MB Bank, BIN 970422, số tài khoản và tên chủ tài khoản thật.
- Nhập thêm ID tài khoản MB Bank từ SePay API v2 vào trường ID tài khoản SePay để đối soát tự động.
- ID được lưu nguyên dạng chuỗi (hỗ trợ UUID và ID có tiền tố); phải khớp chính xác `bank_account_id` trong giao dịch API trả về. Lưu thành công chưa xác minh liên kết ngân hàng.
- Đơn Gateway/Sandbox cũ không chuyển thành đơn QR thật; tạo đơn mới. Không coi thanh toán mô phỏng là tiền ngân hàng.

## Bật đối soát tự động

Nếu biến môi trường không được backend nhận, chạy `./Configure-SePay.ps1` từ thư mục KBS.
Script nhập token kín (hỗ trợ PowerShell 5.1/7), lưu vào `appsettings.Local.json` đã được Git bỏ qua;
backend Development tự đọc lại cấu hình. Không commit hoặc chia sẻ file này.

API token phải là token truy cập giao dịch ngân hàng của tài khoản SePay Live, không phải merchant secret spsk_test.
Không gửi token vào chat hoặc đưa vào frontend/Git. Tại PowerShell 7, nhập token ẩn rồi chạy backend trong cùng terminal:

```powershell
$env:Payments__Gateway__Enabled = 'false'
$env:Payments__SePay__ApiToken = Read-Host 'SePay Live API token' -MaskInput
dotnet run --project .\backend\KBS.API --launch-profile https
```

- Backend gọi SePay API v2 mỗi 30 giây khi còn đơn cần đối soát; máy cần Internet, không cần IPN/tunnel.
- Chỉ ghi PAID khi giao dịch tiền vào khớp ID ngân hàng, tài khoản, số tiền, mã đơn và thời điểm tạo QR.
- Giao dịch trùng không được dùng lại; sai/thiếu tiền hoặc sai nội dung cần nhân viên kiểm tra.
- Chưa có token/ID: không tự xác nhận thanh toán. Nhân viên chỉ xác nhận thủ công sau khi đối chiếu tiền nhận.
- Thiết lập token và ID trước khi tạo đơn thử mới; QR cũ giữ cấu hình tại thời điểm tạo.
- Bật AI tự xác nhận trong Admin nếu muốn tự duyệt đơn đã trả tiền dưới ngưỡng; từ 10.000.000 VND trở lên luôn cần người duyệt.

## Kiểm tra

1. Tạo đơn mới, chọn chuyển khoản; đối chiếu ngân hàng, người nhận, số tiền và nội dung trước khi quét.
2. Chuyển khoản là tiền thật. KBS không tự thực hiện giao dịch thay khách.
3. Trang hỏi trạng thái mỗi 5 giây; backend đối soát mỗi 30 giây, chưa kể độ trễ đồng bộ ngân hàng.
4. Không đánh dấu thành công từ ảnh QR, nút khách bấm hoặc URL redirect; chỉ dựa trên dữ liệu backend.

Tài liệu: https://developer.sepay.vn/vi/sepay-api/v2/giao-dich/danh-sach
