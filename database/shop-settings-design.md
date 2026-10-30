# Cấu hình Home và ngân hàng

## Schema đã duyệt

ShopSettings có một bản ghi Id=1, ràng buộc CHECK và không seed dữ liệu mẫu.
Name (150), Slogan (100), Description (2000), Hotline (30), Email (254), WarehouseAddress (500).
BankName (100), BankBin (6), AccountNumber (19), AccountName (150).
LogoData/BannerData là bytea; LogoContentType/BannerContentType lưu loại ảnh; UpdatedAt là UTC timestamptz.
Không có bảng quan hệ mới hoặc khóa ngoại. Chưa lưu cấu hình trả các trường null.

## Mapping UI

| Vị trí | Nguồn |
|---|---|
| Tên Home, Navbar, Footer | ShopSettings.Name |
| Slogan và giới thiệu Home | ShopSettings.Slogan, Description |
| Logo/banner | ShopSettings.LogoData/LogoContentType, BannerData/BannerContentType |
| Hotline/email/địa chỉ kho Footer | ShopSettings.Hotline, Email, WarehouseAddress |
| Thời gian cập nhật | ShopSettings.UpdatedAt |
| Ngân hàng Admin và thanh toán | ShopSettings.BankName, BankBin, AccountNumber, AccountName |

## API

- GET /api/shop (public): PublicShopDto chỉ thông tin Home và URL ảnh; không có thông tin ngân hàng.
- GET /api/shop/media/{logo|banner} (public): ảnh hoặc 404, Content-Type PNG/JPEG và nosniff.
- GET /api/admin/shop (Admin): { shop: PublicShopDto, bankName, bankBin, accountNumber, accountName }.
- PUT /api/admin/shop (Admin): SaveShopSettingsDto, trả dữ liệu đã lưu; tên bắt buộc,
  email/phone/giới hạn ký tự được validation. Ngân hàng phải đủ cả bốn trường hoặc trống cả bốn.
- PUT /api/admin/shop/media/{logo|banner} (Admin): multipart file; logo <=2 MB, banner <=5 MB,
  kiểm tra MIME và chữ ký PNG/JPEG; lưu ảnh ngay. Không lưu tên file do client gửi.
- DELETE /api/admin/shop/media/{logo|banner} (Admin): xóa ảnh ngay, không xóa nội dung form.

BLL ShopSettingsService xử lý đọc/lưu/validation; controller phân quyền, giới hạn upload;
DAL dùng migration 20261029133528_AddShopSettings. Ảnh lưu trong DB phù hợp quy mô đề tài,
không cần thư viện hoặc kho lưu trữ bên ngoài.

Home dùng ShopProvider, tải lại khi mở, focus, sau khi Admin lưu trong cùng tab,
và mỗi 30 giây nếu tab đang hiển thị. Không thay nội dung trống bằng thông tin mẫu.

## Thanh toán

PaymentService đọc ngân hàng từ ShopSettings. Cấu hình Payments:Bank trong User Secrets không còn được dùng.
Sau migration, Admin cần nhập thông tin ngân hàng thật; chưa có cấu hình thì chỉ COD khả dụng.
Không tự sao chép số tài khoản mẫu hoặc tự xác nhận chuyển khoản.
QR cho đơn thuộc người dùng lấy số tiền/nội dung từ đơn và ngân hàng hiện đang lưu.
Thay đổi ngân hàng ảnh hưởng QR tạo sau đó, kể cả đơn chưa thanh toán; ảnh QR đã gửi trước đó không thể thu hồi.
Nhân viên tiếp tục đối chiếu giao dịch trước khi xác nhận đã nhận tiền.

## Kiểm tra

ShopSettingsTests: public empty, quyền Admin/Staff/Customer, validation, dữ liệu công khai không lộ ngân hàng,
QR theo tài khoản DB, tắt BANKING khi xóa thông tin ngân hàng, upload/đọc/xóa ảnh, chặn ảnh sai/oversize.
PaymentServiceTests: COD/BANKING, quyền sở hữu đơn, số tiền QR, xác nhận thủ công, đơn hủy.
Frontend: smoke-render, typecheck và build.

## Home cũ và đóng/mở website (2026-10-30)

- Khôi phục bố cục và nội dung Home ở commit 12628fd theo yêu cầu người dùng.
- Nội dung gốc nằm trong frontend/src/components/homeDefaults.ts; không seed dữ liệu Home vào DB.
- Tab Quản lý trang Home sửa chữ/ảnh hiện có ở Hero, dải chữ chạy, bộ sưu tập, switch và Footer.
- Các thay đổi lưu tại ShopSettings.HomeContentJson qua PUT /api/admin/shop (homeContent).
  Không có override thì dùng nội dung gốc; chuỗi rỗng là nội dung do admin chủ động xóa.
- Ảnh Home dùng URL HTTPS. Logo/banner cũ vẫn được giữ trong DB/API nhưng không thay bố cục Home cũ.
- PUT /api/admin/shop/availability với { isOpen: true | false } chỉ dành cho Admin.
  Trạng thái lưu tại ShopSettings.IsOpen; lưu form nội dung không đổi trạng thái này.
- Khi đóng, khách và staff nhận HTTP 503 trên API nghiệp vụ; /api/shop và /api/auth/login vẫn mở.
  Admin đã đăng nhập được truy cập API quản trị và mở lại website.
- Frontend hiển thị trang bảo trì, có nút đăng nhập quản trị. Tab đang mở cập nhật tối đa 30 giây;
  backend chặn yêu cầu mới ngay khi trạng thái đóng được lưu.
- Migration 20261030022228_AddHomeContentAndAvailability mặc định IsOpen=true và HomeContentJson={}.
- Kiểm thử bổ sung: quyền đóng/mở, đăng nhập khi đóng, API khách bị chặn, admin mở lại,
  lưu nội dung không tự mở web, roundtrip nội dung và chặn URL ảnh không HTTPS.
