# Chạy AI tự xác nhận đơn của KBS

`ai_auto.py` ở đây là bản chuyển đổi từ `T:/KeyBoardShop88_AI/ai_auto.py` cho API/schema của KBS.
File nguồn cũ được giữ nguyên. Worker dùng thư viện chuẩn Python, không cần pip install.
Không chạy script SQL demo hay script Python cũ cùng database KBS.

## Khởi động local

Yêu cầu Python 3, .NET 9, PostgreSQL và cấu hình KBS/User Secrets hiện có.
Đã bổ sung migration `20261029043203_AddAiManagement`; với database khác, áp dụng bằng:

```powershell
dotnet ef database update --project backend/KBS.DAL --startup-project backend/KBS.API
```

Tắt instance backend đang chiếm cổng 5231 rồi chạy từ thư mục project:

```powershell
pwsh -File ai/start-local.ps1 -Python python
```

Nếu `python` trỏ tới Windows Store alias, truyền đường dẫn Python thực tế vào `-Python`.
Trên máy hiện tại có thể dùng:

```powershell
pwsh -File ai/start-local.ps1 -Python C:/Users/mytaa/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe
```

Script sinh credential ngẫu nhiên trong environment, chia sẻ cho API và worker, không in hoặc lưu vào repo.
API dùng profile HTTP local cổng 5231; frontend cần `VITE_API_URL=http://localhost:5231` khi khởi động Vite.
Worker chạy nền, ghi log tại `ai/worker.log` và `ai/worker-error.log` (gitignored).
Dừng backend bằng Ctrl+C sẽ dừng worker do script này khởi động.

Đăng nhập Admin → Quản lý AI → chọn “Tự động xác nhận đơn” → bật công tắc.
Mặc định mọi module tắt. Bật sẽ xử lý cả các đơn PENDING đang tồn, không chỉ đơn mới.
Các module chưa tích hợp chỉ hiển thị metadata thật; công tắc disabled.

## Bật AI nhưng đơn vẫn chờ xác nhận

- `ENABLED` / “Đã bật xử lý tự động” chỉ ghi nhận bật cấu hình, chưa phải xác nhận đơn.
- Xác nhận thành công phải có sự kiện `CONFIRMED` gắn với `OrderId`, đồng thời
  `Orders.OrderStatus` chuyển sang `CONFIRMED` trong cùng giao dịch.
- Nếu module bật nhưng mất kết nối, kiểm tra tiến trình `ai/ai_auto.py` và heartbeat.
  Chạy `dotnet run` riêng không khởi động worker. Dùng `start-local.ps1` như hướng dẫn trên.
- API và worker phải dùng cùng credential và cùng backend. Với script local HTTP,
  đặt `VITE_API_URL=http://localhost:5231` và khởi động lại Vite.
- Lỗi worker nằm ở `ai/worker.log` / `ai/worker-error.log`, không nằm trong console trình duyệt.
- Trang đơn hàng của khách hiện cập nhật khi mở trang hoặc bấm “Tải lại”.

## Chạy hai tiến trình riêng / triển khai

- Backend: đặt `Ai__WorkerKey` (hoặc `Ai:WorkerKey` trong User Secrets) tối thiểu 32 ký tự ngẫu nhiên.
- Worker: đặt cùng giá trị vào `KBS_AI_WORKER_KEY` và đặt `KBS_API_URL` trỏ tới backend, rồi chạy
  `python ai/ai_auto.py`. Không dùng token Admin cho worker.
- Dùng HTTPS khi kết nối ngoài loopback; chứng chỉ phải được Python tin cậy, không tắt xác minh TLS.
- HTTPS dev certificate của .NET có thể không nằm trong trust store của Python. Local dùng profile HTTP
  như trên; production cấu hình chứng chỉ hợp lệ.
- Không đặt worker key trong biến `VITE_*`, mã frontend, source control hoặc URL.

Worker poll mỗi 5 giây (tối đa 50 đơn/lượt), timeout HTTP 15 giây; lỗi được retry ở vòng tiếp theo.
Nút tắt chặn quyết định mới phía server; heartbeat vẫn hoạt động. Mất heartbeat trên 30 giây hiển thị mất kết nối.
Thông báo nghiệp vụ/bật tắt lưu trong AiEvents. Lỗi mạng phía worker ghi log cục bộ vì lúc đó không thể gửi vào DB.

## Quy tắc

Chỉ đơn `PENDING` có `Orders.TotalPrice < 10000000` mới chuyển `CONFIRMED`.
Đúng 10 triệu hoặc cao hơn giữ `PENDING`, ghi một thông báo cần staff xác nhận.
Server kiểm tra lại DB khi nhận quyết định; không thay đổi thanh toán/tồn kho.
Giao dịch Serializable và khóa module bảo vệ thao tác bật/tắt/worker đồng thời;
tranh chấp với staff/customer trả 409, worker đọc lại trong lượt tiếp theo.

## Kiểm tra

```powershell
python -m unittest discover -s ai
dotnet test backend/KBS.Tests/KBS.Tests.csproj --filter "FullyQualifiedName~AiManagementTests|FullyQualifiedName~OrderServiceTests"
npm --prefix frontend run test:smoke
npm --prefix frontend run build
```

Các test API mặc định dùng EF InMemory, không thay thế kiểm thử khóa/giao dịch đồng thời trên PostgreSQL.
Thiết kế bảng, ánh xạ UI và contracts: `database/ai-management-design.md`.
