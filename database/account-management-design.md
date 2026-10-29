# Quản lý tài khoản

## Mapping giao diện

| Nội dung | Nguồn |
|---|---|
| ID, tên đăng nhập, họ tên | Users.Id, Username, FullName |
| Avatar chữ | Ký tự đầu FullName hoặc Username; schema chưa có ảnh |
| Email, điện thoại | Users.Email, PhoneNumber |
| Vai trò | Users.RoleId → Roles.Id, Name |
| Hoạt động / bị khóa | Users.IsActive |
| Ngày tạo | Users.CreatedAt |
| Tổng số / tìm kiếm / lọc / phân trang | Danh sách từ GET /api/users |

Không có trường xác thực trong schema: không hiển thị trạng thái “Chờ xác thực”.
Vai trò lấy từ Roles, không thêm role “Quản lý” theo bản HTML mẫu.

## API (Admin)

- GET /api/users → UserResponseDto[] (giữ contract cũ).
- GET /api/users/roles → [{ id, name }].
- POST /api/users → 201 UserResponseDto; CreateUserDto gồm username, email, fullName?,
  phoneNumber?, roleId, isActive, password, confirmPassword.
- PUT /api/users/{id} → UserResponseDto; SaveUserDto gồm các trường trên, trừ mật khẩu.
- PUT /api/users/{id}/status → UserResponseDto; { isActive: boolean } bắt buộc.
- PUT /api/users/{id}/role → UserResponseDto; { roleId: positive integer }.
- DELETE /api/users/{id} → 204; 404 nếu không tồn tại; 409 nếu có quan hệ dữ liệu hoặc bị bảo vệ.

DTO dùng DataAnnotations; BLL UserService kiểm tra trùng username/email/phone, băm mật khẩu
bằng PasswordHasher hiện có và bảo vệ Admin. DAL giữ nguyên schema. Controller giới hạn quyền Admin.
Không trả PasswordHash. Mật khẩu không được sửa qua form hồ sơ.

Không cho tự khóa, tự đổi vai trò hoặc tự xóa. Không làm mất Admin hoạt động cuối cùng.
Thao tác cập nhật/xóa dùng transaction Serializable; tranh chấp trả lỗi để tải lại.
Xóa bị chặn nếu có đơn hàng, giỏ hàng, yêu thích, đánh giá, ca trực, ghi chú đơn,
biến động kho, hội thoại, tin nhắn hoặc sự kiện AI liên quan; không cascade dữ liệu người dùng.

## Giao diện và kiểm thử

Lọc/sắp xếp/phân trang trên client từ dữ liệu thật của API hiện có; khi số lượng tài khoản
tăng lớn, chuyển GET danh sách sang phân trang server bằng contract riêng.
Thao tác hàng loạt xử lý từng tài khoản, báo số thành công và lỗi từng tài khoản; không cam kết atomic cả lô.
Tải lại dữ liệu sau thay đổi, clear lựa chọn khi lọc/chuyển trang; có empty/loading/error và xác nhận xóa.

Kiểm thử: AccountManagementTests, AuthenticationTests; node frontend/scripts/account-smoke.mjs.
EF InMemory không chứng minh hành vi tranh chấp trên PostgreSQL.
