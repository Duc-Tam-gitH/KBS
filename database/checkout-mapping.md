# Checkout KBS

Mẫu: 2bbab3cd-8eab-4377-b491-e6be91faf1a3.zip. Giữ bố cục hai cột, các phần đánh số,
thẻ chọn thanh toán, tóm tắt đơn sticky, tiến trình và màn hình xác nhận.

| UI | Nguồn / nơi lưu |
|---|---|
| Tên, điện thoại điền sẵn; email tài khoản chỉ đọc | Users.FullName, PhoneNumber, Email qua UserProfileDto |
| Thông tin người nhận nhập khi đặt | Orders.CustomerName, PhoneNumber, ShippingAddress |
| Sản phẩm, màu, layout, giá | Products.Name, Color, Layout, Price; CartItems.ProductId FK → Products.Id |
| Ảnh đầu tiên | ProductImages.ImageUrl, SortOrder, Id; ProductId FK → Products.Id |
| Số lượng | CartItems.Quantity; UserId FK → Users.Id |
| Tạm tính | SUM(Products.Price × CartItems.Quantity) |
| Phương thức | Orders.PaymentMethod; COD/BANKING theo CreateOrderDto |
| Mã đơn, ngày đặt, tổng tiền xác nhận | Orders.Id, CreatedAt, TotalPrice từ POST /api/orders |
| Chi tiết đơn đã lưu | OrderDetails.OrderId FK → Orders.Id, ProductId FK → Products.Id, UnitPrice, Quantity |

Không có schema voucher, phí/đơn vị vận chuyển, đặt cọc, ghi chú khách hàng hay cấu hình cổng thanh toán.
Không thêm các trường này, không sao chép phí/khuyến mãi/chính sách từ mẫu. Địa chỉ dùng trường đầy đủ hiện có.
Chuyển khoản tạo đơn chờ thanh toán và VietQR theo cấu hình tài khoản nhận tiền; nhân viên xác nhận PAID.
Địa chỉ ba cấp và API thanh toán được mô tả tại [payment-integration.md](payment-integration.md).

## Contract / trách nhiệm
- GET /api/cart và các mutation giỏ hàng: mở rộng CartItemResponseDto với ImageUrl?, Color?, Layout?.
  CartService đọc qua KbsDbContext và map; không đổi schema hay route/phân quyền.
- POST /api/orders giữ CreateOrderDto và OrderResponseDto; BLL kiểm tra tồn, tính giá từ DB,
  ghi đơn/chi tiết, trừ kho, cập nhật giỏ trong transaction. Không nhận giá từ client.
- Checkout nhận cart/user/loading/error từ Storefront; hiển thị, validate input, chống gửi trùng,
  chỉ xóa giỏ ở client khi server trả đơn thành công.

## Acceptance
- Không có mock/fake voucher/ảnh ngoài database trong UI.
- Cart API trả đúng ảnh đầu tiên theo SortOrder/Id và nullable khi không có ảnh; giữ giá/số lượng.
- Giỏ đang tải/lỗi/rỗng không thể đặt hàng; có đường quay lại giỏ/cửa hàng.
- Có tổng tiền, số lượng, tên/ảnh/thông số; giao diện responsive, focus và radio native.
- Lỗi đặt hàng giữ input, cho thử lại; thành công hiển thị ID/tổng tiền thật từ response.
- Build, typecheck, smoke render, cart/order backend tests.
