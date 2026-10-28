# Quản lý sản phẩm

UI tham chiếu: ProductManager/ProductToolbar/ProductTable/ProductFormDrawer trong ZIP người dùng cung cấp.
Giữ bố cục, dùng palette tập trung ở frontend/src/index.css; không nhập dữ liệu mẫu từ ZIP.

| UI | Nguồn DB | Ghi chú |
|---|---|---|
| ID, tên | Products.Id, Products.Name | Id là PK; dùng ID thay SKU vì schema không có SKU |
| Danh mục và bộ lọc | Categories.Id, Categories.Name | Products.CategoryId là FK |
| Thương hiệu | Brands.Id, Brands.Name | Products.BrandId là FK |
| Giá bán, giá gốc | Products.Price, Products.OriginalPrice | VND |
| Mô tả | Products.Description | Nullable |
| Switch, layout, kết nối, keycap, màu | Products.SwitchType, Layout, ConnectionType, Keycap, Color | Nullable |
| Hot swap, RGB | Products.IsHotSwap, Products.IsRGB | Boolean |
| Đang bán/ngừng bán | Products.IsActive | Nhãn dịch từ boolean, không phải dữ liệu mẫu |
| Ngày tạo | Products.CreatedAt | Không có ngày cập nhật sản phẩm |
| Tồn kho, số sản phẩm hết hàng | Inventories.StockQuantity | Inventories.ProductId FK unique → Products.Id; chỉ đọc |
| Ảnh được giữ khi sửa | ProductImages.ImageUrl, AltText, SortOrder | ProductImages.ProductId FK → Products.Id |
| Tổng số, phân trang | COUNT/filter danh sách Products từ API | Không có số liệu hard-code |

## Hợp đồng và trách nhiệm

- GET /api/products/management: Admin; trả ProductResponseDto[] gồm cả sản phẩm ngừng bán.
- GET /api/categories, GET /api/brands: dữ liệu lựa chọn có sẵn.
- POST /api/products: ProductCreateDto → ProductResponseDto; kho ban đầu do BLL tạo bằng 0.
- PUT /api/products/{id}: ProductUpdateDto → ProductResponseDto; giữ các trường ảnh/thông số.
- Ngừng bán/mở bán dùng PUT IsActive. Không có DELETE nên không hiển thị xóa vĩnh viễn.
- Entities/schema giữ nguyên; DAL dùng KbsDbContext; BLL truy vấn và map DTO;
  Controller phân quyền; frontend lọc/phân trang danh sách và gọi API hiện có.
- Không sao chép ngưỡng sắp hết hàng hard-code trong ZIP; hiển thị hết hàng theo StockQuantity = 0.

## Tiêu chí nghiệm thu

- Admin đọc được cả hai trạng thái; endpoint công khai vẫn chỉ có sản phẩm đang bán.
- Khách/chưa đăng nhập không đọc được endpoint quản trị.
- Tìm tên/ID, lọc danh mục/trạng thái, phân trang đúng và có empty state.
- Có loading/error/retry; không dùng dữ liệu giả khi API lỗi.
- Lưu lỗi giữ form; chống gửi trùng; cập nhật thành công mới đổi danh sách.
- Sửa và đổi trạng thái giữ ảnh, mô tả, thông số hiện có.
- Drawer hỗ trợ focus, Escape, trap focus bằng dialog native; layout responsive.
- Build, lint, typecheck, smoke render và test backend liên quan.
