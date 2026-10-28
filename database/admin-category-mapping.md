# Quản lý danh mục

| UI | Bảng.cột | Cách lấy |
|---|---|---|
| ID, tên, mô tả | Categories.Id, Name, Description | API categories |
| Trạng thái, bộ lọc | Categories.IsActive | boolean → Hoạt động / Đang ẩn |
| Ngày tạo | Categories.CreatedAt | Định dạng vi-VN |
| Số sản phẩm | Products.Id, CategoryId | COUNT qua FK Products.CategoryId → Categories.Id |
| Thống kê tổng / hoạt động / ẩn | Categories.Id, IsActive | Đếm danh sách API trả về |
| Phân trang, CSV | Các trường trên | Từ danh sách đã tìm/lọc, 10 dòng/trang |

Categories.Id là PK. FK Products.CategoryId dùng DeleteBehavior.Restrict.
Không có ParentId: bỏ cột danh mục cha, không thêm schema.

## Contract và trách nhiệm

- GET /api/categories và GET /api/categories/{id}: DTO hiện có + productCount (int).
- POST /api/categories: { name, description, isActive } → 201 và CategoryResponseDto.
- PUT /api/categories/{id}: cùng payload → 200 DTO, 404 nếu không tồn tại.
- DELETE /api/categories/{id}: chỉ Admin; 204 thành công, 404 không tồn tại,
  409 nếu đã có sản phẩm. FK vẫn bảo vệ khi có sản phẩm thêm đồng thời.
- Tên rỗng → 400; tên trùng không phân biệt hoa thường → 409 (logic hiện có).
- UI gọi api.admin; controller kiểm tra quyền và chuyển lỗi; CategoryService truy vấn,
  kiểm tra nghiệp vụ; KbsDbContext giữ quan hệ DB. Không thêm package hay migration.
- Sidebar trái của AdminWorkspace giữ nguyên; CategoryManagement quản lý vùng giữa
  và form chi tiết bên phải. CSV gồm toàn bộ kết quả đang lọc, có chống công thức Excel.

## Tiêu chí kiểm thử trước triển khai

- Hiển thị dữ liệu thật và productCount; không còn HTML nguyên trang / dữ liệu mẫu.
- Tìm/lọc đặt lại trang, xóa dòng cuối không để trang vượt phạm vi.
- Tạo/sửa xác thực tên; chặn gửi trùng; hiển thị lỗi API, giữ form khi lưu lỗi.
- Xóa yêu cầu xác nhận; không xóa danh mục có sản phẩm; xử lý 404 và 409.
- Tải thất bại không hiện số 0 giả; có thử lại và trạng thái rỗng.
- CSS dùng palette, liquid glass hiện có, responsive, label và focus rõ ràng.
