# KBS frontend

Ứng dụng đang chạy trong src, kết hợp bố cục TypeScript/Tailwind từ Bolt với các trang và API của KBS.
Source cũ trong src_backup được giữ nguyên để dự phòng; ứng dụng không import hoặc tải CSS từ backup.

## Chạy local

Trong thư mục frontend:

```powershell
npm.cmd install
npm.cmd run dev
```

Đặt VITE_API_URL trong .env.local để trỏ tới ASP.NET Core API đang chạy.
Trang chủ và danh mục cho phép khách truy cập. Giỏ hàng, thanh toán, đơn hàng và tài khoản yêu cầu đăng nhập.
Dashboard chỉ dành cho Admin. Access token được giữ trong bộ nhớ; tải lại trang cần đăng nhập lại.

## Cấu trúc

- src/pages/Home.tsx và src/components: landing page, thanh điều hướng nổi, footer.
- src/Storefront.jsx: điều hướng, danh mục, chi tiết, thanh toán, đơn hàng, dashboard và state giỏ hàng.
- src/pages/auth, src/pages/customer/Cart.jsx: các trang được chuyển từ bản cũ.
- src/services/api.js: kết nối API và thông báo thay đổi phiên đăng nhập.
- src/index.css: palette duy nhất; src/store.css: phong cách Tailwind cho các trang chức năng.

## Kiểm tra

```powershell
npm.cmd run build
npm.cmd run typecheck
npm.cmd run lint
npm.cmd run test:smoke
```

Smoke test kiểm tra render các trang, menu theo vai trò, độ tương phản các cặp màu chính,
và request/phiên đăng nhập bằng fetch giả lập. Không thay thế kiểm thử mua hàng với PostgreSQL thật.
Các file quản lý sản phẩm/người dùng trong bản backup còn trống; bản ghép khôi phục dashboard đã có.
Ảnh landing và Google Fonts cần kết nối internet.
