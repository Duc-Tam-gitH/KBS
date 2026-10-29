# Thiết kế AI Management — schema đã được duyệt

## Phạm vi đã duyệt

- Quản trị AI trong `AIManagement.jsx`, quyền Admin.
- Danh sách AI ở giữa phía trên, thông báo ALL AI / AI được chọn ở giữa phía dưới.
- Sidebar phải hiển thị chi tiết AI được chọn. Mobile xếp thành một cột.
- Mỗi AI có công tắc riêng. Module chưa tích hợp hiển thị công tắc disabled và lý do.
- Tích hợp đầu tiên: `ai_auto.py`. Chỉ đơn PENDING và tổng tiền dưới 10.000.000 VND được tự xác nhận.
- Đơn từ 10.000.000 VND giữ PENDING để staff xử lý; không thêm trạng thái đơn mới.

## Kết quả khảo sát

Nguồn Python: `T:/KeyBoardShop88_AI`.

| File thực tế | Chức năng đọc từ mã | Giai đoạn này |
|---|---|---|
| ai_auto.py | Duyệt đơn theo tổng tiền | Kết nối |
| ai_anti.py | Phát hiện mua gom trên 100 sản phẩm trong 24 giờ | Chưa tích hợp |
| ai_security.py | Cảnh báo đơn giá trị bất thường | Chưa tích hợp |
| ai_reviewnpl.py | Lọc từ khóa đánh giá và phát hiện spam | Chưa tích hợp |
| ai_recommender.py | Gợi ý sản phẩm theo kịch bản | Chưa tích hợp |

`ai_mockdata.py` là công cụ chèn dữ liệu demo, không đưa vào danh sách AI vận hành, không chạy.
Các dòng console nói gửi thông báo/email trong script không chứng minh đã có tích hợp gửi thật.

`ai_auto.py` hiện dùng ngưỡng 20 triệu, ghi trực tiếp schema `shop` và dùng trạng thái
`Approved` / `Pending_Manual_Review`. KBS dùng EF migrations và trạng thái PENDING / CONFIRMED.
Không chạy script gốc trên DB project. Không sao chép thông tin kết nối hard-code.
DbContext/migrations KBS hiện chưa có cấu hình hoặc lịch sử hoạt động AI.

### Đối chiếu đầy đủ ba file SQL nguồn

- `KBS88.sql`: tạo 9 bảng nghiệp vụ trong schema `shop`, role PostgreSQL và RLS cho Orders.
  Không có bảng cấu hình AI hoặc lịch sử thông báo. Tên SQL không đặt trong dấu nháy kép nên
  PostgreSQL chuyển về chữ thường; không thể dùng nguyên tên này thay cho bảng EF của KBS.
- `Shop_ai.sql`: tạo schema `shop_ai`, hàm `notify_abnormal_order` và trigger `trg_monitor_orders`
  AFTER INSERT trên `shop.Orders`, phát kênh `order_security_channel`. Không tạo bảng lưu thông báo,
  không có trigger phát `auto_approve_channel` mà `ai_auto.py` đang LISTEN.
- `Demotest.sql`: chèn dữ liệu demo và dọn dữ liệu bằng DELETE không có WHERE, reset sequence.
  Không phải migration tích hợp. Có tham chiếu `shop.Reviews` và `Users.CanReview` chưa được định nghĩa
  trong `KBS88.sql`. Kịch bản 65 triệu mô tả cảnh báo đỏ cũng không khớp ngưỡng >100 triệu trong ai_security.py.
- Không thực thi các script này. Chúng chưa cung cấp hai cấu trúc cần cho công tắc và bảng thông báo.
  Giữ đề xuất AiModules/AiEvents và dùng schema EF hiện tại; không tạo lại 9 bảng nghiệp vụ.

## Schema đã triển khai

### AiModules

| Cột | Kiểu | Quy tắc |
|---|---|---|
| Id | varchar(50) | PK, mã module như ai_auto |
| Name | varchar(150) | Bắt buộc, tên chức năng |
| Description | varchar(1000) | Bắt buộc, mô tả theo module thực tế |
| SourceFile | varchar(100) | Tên file, không lưu đường dẫn bí mật |
| IsIntegrated | boolean | Chỉ ai_auto là true trong giai đoạn này |
| IsEnabled | boolean | Mặc định false; không bật module chưa tích hợp |
| ApprovalLimit | numeric(18,2), nullable | ai_auto = 10000000; module khác null; chưa có API sửa ngưỡng |
| LastHeartbeatAt | timestamptz, nullable | Backend ghi khi worker xác thực kết nối |
| UpdatedAt | timestamptz | Thời điểm cập nhật cấu hình |

Ràng buộc: IsEnabled chỉ true khi IsIntegrated true; ApprovalLimit null hoặc > 0.
Khởi tạo 5 bản ghi metadata từ các file đã khảo sát, không tạo lịch sử/thống kê giả.

### AiEvents

| Cột | Kiểu | Quy tắc |
|---|---|---|
| Id | bigint identity | PK |
| ModuleId | varchar(50) | FK → AiModules.Id, RESTRICT |
| OrderId | bigint, nullable | FK → Orders.Id, RESTRICT; null cho bật/tắt hoặc lỗi kết nối |
| ActorId | bigint, nullable | FK → Users.Id, RESTRICT; người bật/tắt, null cho worker |
| Kind | varchar(40) | ENABLED, DISABLED, CONFIRMED, MANUAL_REVIEW, ERROR |
| Message | varchar(1000) | Thông báo sự kiện thật do server tạo; không chứa secret |
| CreatedAt | timestamptz | Thời gian UTC từ backend |

Index: (ModuleId, Id), (OrderId); unique (ModuleId, OrderId, Kind) cho sự kiện theo đơn
để tránh lặp thông báo CONFIRMED/MANUAL_REVIEW. OrderId null cho phép nhiều sự kiện cấu hình/lỗi.
Không thay đổi schema Orders, Users hoặc các bảng nghiệp vụ khác.

## Ánh xạ giao diện

| Vị trí | Nguồn |
|---|---|
| Tên, mô tả, file module | AiModules.Name / Description / SourceFile |
| Công tắc và khả năng bật | AiModules.IsEnabled / IsIntegrated |
| Tình trạng kết nối | Suy ra từ AiModules.LastHeartbeatAt; chưa nhận heartbeat thì chưa kết nối |
| Quy tắc ngưỡng ai_auto | AiModules.ApprovalLimit; điều kiện Orders.OrderStatus = PENDING |
| Thời gian liên lạc / cấu hình | AiModules.LastHeartbeatAt / UpdatedAt |
| Bảng thông báo ALL AI | AiEvents JOIN AiModules theo ModuleId |
| Thông báo AI riêng | Cùng truy vấn, lọc ModuleId |
| Cột thời gian / nội dung / kết quả | AiEvents.CreatedAt / Message / Kind |
| Đơn liên quan | AiEvents.OrderId → Orders.Id |

Không chọn AI: sidebar hướng dẫn chọn. Không có sự kiện: “Chưa có thông báo”.
Tab ALL AI là bộ lọc giao diện; không cần bản ghi module giả.
Giữ sidebar quản trị bên trái; AIManagement trả về ad-main và ad-details như các trang quản trị đầy đủ.
Dùng token màu hiện có, nút có trạng thái đang xử lý, lỗi và hỗ trợ bàn phím.

## API contracts đề xuất

### Xem AI — JWT role Admin hoặc Staff; bật/tắt chỉ Admin

Staff có tab AI chỉ đọc trong khu vực làm việc, dùng chung dữ liệu và mapping giao diện bên trên.
Cho phép chọn module, lọc/phân trang thông báo và làm mới; trạng thái bật/tắt hiển thị bằng nhãn.
Hai API GET bên dưới cho phép Admin,Staff. API PUT yêu cầu Admin và trả 403 cho Staff.
DTO, BLL và DAL được tái sử dụng; không đổi schema. Giao diện tự cập nhật mỗi 5 giây.

- GET `/api/admin/ai/modules` → AiModuleResponseDto[]:
  id, name, description, sourceFile, isIntegrated, isEnabled, approvalLimit,
  lastHeartbeatAt, updatedAt, connectionStatus (NOT_INTEGRATED / NEVER_CONNECTED / ONLINE / OFFLINE).
- PUT `/api/admin/ai/modules/{id}/enabled`, body `{ isEnabled: boolean }`
  → AiModuleResponseDto. Ghi sự kiện cấu hình khi giá trị thật sự đổi.
- GET `/api/admin/ai/events?moduleId=ai_auto&beforeId=123&limit=50`
  → `{ items: AiEventResponseDto[], nextBeforeId: number|null }`.
  Bỏ moduleId để lấy ALL; limit giới hạn tối đa 100. DTO gồm id, moduleId, moduleName,
  orderId, kind, message, createdAt. Sắp xếp Id giảm dần.

### Worker ai_auto — credential riêng, giới hạn module

- POST `/api/ai-worker/auto/poll` → `{ isEnabled, approvalLimit, orders: [{ id, totalPrice }] }`.
  Ghi heartbeat dù module đang tắt. Khi tắt trả danh sách rỗng.
  Chỉ lấy tối đa 50 đơn PENDING chưa có quyết định cho module, theo Id tăng dần.
- POST `/api/ai-worker/auto/orders/{id}/decision`, body `{ decision: "CONFIRM"|"MANUAL_REVIEW" }`
  → `{ orderId, result: "CONFIRMED"|"MANUAL_REVIEW"|"SKIPPED" }`.
  Server đọc lại IsEnabled, trạng thái và tổng tiền từ DB; không tin giá từ worker.
  Sai quyết định/ngưỡng trả 409; request lặp không ghi lịch sử hoặc chuyển trạng thái lần hai.

Worker dùng secret riêng từ môi trường, server đọc secret từ cấu hình/User Secrets;
không dùng token Admin, không đưa secret vào frontend hoặc source control.
401 khi credential sai, 403 cho customer/staff tại API quản trị, 404 module/đơn không tồn tại.

## Ranh giới và cách tích hợp

- Models: hai entity và DTO nêu trên.
- DAL: KbsDbContext, mapping FK/index/check constraints, migration sau khi duyệt.
- BLL: AiManagementService đọc cấu hình/sự kiện, xử lý bật/tắt, heartbeat và quyết định.
- OrderService: tái sử dụng logic chuyển trạng thái PENDING → CONFIRMED; xác nhận và ghi sự kiện
  trong cùng transaction với kiểm tra điều kiện. Không gọi lồng transaction hiện có.
- Controller: xác thực, phân quyền, kiểm tra request và ánh xạ mã lỗi.
- Python: bản chuyển đổi của ai_auto trong `T:/KBS/ai/ai_auto.py`, lưu cùng project;
  dùng urllib/json/decimal của stdlib để gọi API, thay listener PostgreSQL cũ.
  Giữ nguyên file nguồn ngoài workspace. Thay ngưỡng cũ bằng cấu hình từ API.
- Poll mỗi 5 giây, có timeout và retry có giới hạn tốc độ; tắt dừng xử lý nghiệp vụ nhưng vẫn heartbeat.
  Quá 30 giây không heartbeat thì UI hiển thị OFFLINE. UI tự làm mới khi đang mở trang.
- Không tự chạy các module còn lại. Công tắc của chúng disabled cho tới khi tích hợp thật.

## Tính nhất quán

- Kiểm tra bật/tắt và quyết định trong transaction Serializable; tắt module cạnh tranh với quyết định
  phải được tuần tự hóa bằng cùng bản ghi module. Sau khi lệnh tắt hoàn tất không có quyết định mới.
- Chỉ cập nhật đơn đang PENDING, chống ghi đè customer huỷ hoặc staff xác nhận.
- Lỗi tranh chấp trả 409/retry; rollback cả đơn và sự kiện. Không thay đổi thanh toán hoặc tồn kho khi xác nhận.
- Đơn lớn giữ PENDING, chỉ ghi một sự kiện MANUAL_REVIEW; vẫn xuất hiện trong danh sách staff hiện có.
- Mất worker/API: giữ đơn chờ. Khi phục hồi, poll xử lý đơn còn tồn, không phụ thuộc notification đã mất.

## Tiêu chí kiểm thử trước triển khai

1. PENDING có tổng 9.999.999 → CONFIRMED và đúng một sự kiện.
2. Tổng 10.000.000 hoặc lớn hơn → PENDING + MANUAL_REVIEW một lần.
3. Đã CANCELLED/CONFIRMED/SHIPPING/COMPLETED → không đổi.
4. Module tắt/chưa tích hợp hoặc credential sai → không xác nhận.
5. Gọi lặp/retry → không nhân đôi sự kiện; không đổi thanh toán/tồn kho.
6. Customer huỷ/staff xác nhận/tắt module cùng lúc → không ghi đè trạng thái đã chốt.
7. ALL/AI riêng phân trang đúng; quyền Admin; sidebar theo lựa chọn; empty/loading/error.
8. Heartbeat cũ/mất kết nối → OFFLINE; worker phục hồi xử lý đơn đang chờ.
9. Unit/API test, Python unittest, frontend smoke/build; kiểm tra transaction tranh chấp trên PostgreSQL
   khi có database kiểm thử, báo rõ nếu chỉ chạy EF InMemory.

## Thứ tự thực hiện sau duyệt schema

1. Entity/mapping/migration và tests nghiệp vụ/API.
2. API + worker ai_auto tương thích KBS, tài liệu biến môi trường và lệnh chạy.
3. UI danh sách/công tắc, bảng thông báo và sidebar; kiểm tra tích hợp.

Migration `20261029043203_AddAiManagement` đã được tạo và áp dụng vào database KBS sau khi người dùng duyệt.
Hướng dẫn chạy API và worker: `ai/README.md`.
