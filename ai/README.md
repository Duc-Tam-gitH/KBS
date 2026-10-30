# Chạy và quản lý các AI của KBS

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
.\start-ai.cmd
```

Không cần PowerShell 7. File này dùng Windows PowerShell có sẵn để chạy backend.
Bạn cũng có thể chạy backend trực tiếp: `dotnet run --project backend/KBS.API --launch-profile http`.
Backend tìm Python trong runtime Codex của tài khoản hiện tại, nếu không có thì dùng `python` trong PATH.
Để chỉ định Python khác:

```powershell
.\start-ai.cmd -Python "C:\path\to\python.exe"
```

Backend tự sinh credential ngẫu nhiên trong bộ nhớ nếu chưa cấu hình `Ai:WorkerKey`,
truyền cho worker qua environment, không in hoặc lưu vào repo.
API dùng profile HTTP local cổng 5231; frontend cần `VITE_API_URL=http://localhost:5231` khi khởi động Vite.
Worker chạy nền, stdout/stderr được ghi vào log backend với tên module.
Dừng backend bằng Ctrl+C sẽ dừng các worker do backend này khởi động.

Đăng nhập Admin → Quản lý AI → chọn “Tự động xác nhận đơn” → bật công tắc.
Backend kiểm tra trạng thái mỗi 2 giây: ON khởi động worker, OFF dừng worker tương ứng.
Chờ khoảng 5–10 giây để giao diện hiển thị “Đã kết nối”.
Module đang ON được khởi động lại khi backend khởi động lại.
Mặc định mọi module tắt. Bật sẽ xử lý cả các đơn PENDING đang tồn, không chỉ đơn mới.
Các module chưa tích hợp chỉ hiển thị metadata thật; công tắc disabled.

## Thêm AI khác

Hiện chỉ `ai_auto` đã tích hợp. Các module còn lại chưa có worker/API nghiệp vụ trong repo.
Sau khi triển khai worker và API cho module mới:

1. Đặt file `ai_<tên>.py` trong thư mục `ai/`. Build/publish tự sao chép các file `ai_*.py`.
2. Đăng ký module ID và tên file trong `Ai:Workers` của `backend/KBS.API/appsettings.json`.
   Ví dụ thêm `"ai_anti": "ai_anti.py"` sau khi đã triển khai AI phát hiện mua gom.
3. Tích hợp API riêng: xác thực worker, cập nhật heartbeat của module, kiểm tra `IsEnabled`
   phía server trước mỗi thay đổi dữ liệu, ghi sự kiện vào `AiEvents`.
   Filter/key hiện tại chỉ áp dụng cho endpoint `ai_auto`; AI mới cần được nối vào API phù hợp.
4. Đánh dấu `AiModules.IsIntegrated = true` bằng migration/luồng tích hợp, build và khởi động lại backend.
5. Bật ON trên web. Không cần thêm script khởi động riêng.

Worker nhận `KBS_API_URL` và `KBS_AI_WORKER_KEY` từ backend.
Tên file thực thi lấy từ cấu hình server, không lấy từ `SourceFile` trong database hay input trình duyệt.
Không chạy thêm worker thủ công khi đang dùng chế độ quản lý tiến trình tự động.

## Bật AI nhưng đơn vẫn chờ xác nhận

- `ENABLED` / “Đã bật xử lý tự động” chỉ ghi nhận bật cấu hình, chưa phải xác nhận đơn.
- Xác nhận thành công phải có sự kiện `CONFIRMED` gắn với `OrderId`, đồng thời
  `Orders.OrderStatus` chuyển sang `CONFIRMED` trong cùng giao dịch.
- Nếu module bật nhưng mất kết nối, kiểm tra tiến trình `ai/ai_auto.py` và heartbeat.
  Xem sự kiện `ERROR` trong Thông báo hoạt động và log backend.
  Nếu worker không khởi động được hoặc tự thoát, sửa lỗi rồi OFF/ON để thử lại.
- API và worker phải dùng cùng credential và cùng backend. Với script local HTTP,
  đặt `VITE_API_URL=http://localhost:5231` và khởi động lại Vite.
- Lỗi worker nằm trong log backend, không nằm trong console trình duyệt.
- Trang đơn hàng của khách hiện cập nhật khi mở trang hoặc bấm “Tải lại”.

## Chạy hai tiến trình riêng / triển khai

- Mặc định `Ai:ManageProcesses = true`: dùng một instance backend quản lý các worker cục bộ.
  Khi chạy nhiều instance backend hoặc dùng hệ thống quản lý worker bên ngoài,
  đặt `Ai__ManageProcesses=false` để tránh mỗi backend khởi động thêm worker.
- Cấu hình tùy chọn: `Ai__PythonPath` (Python executable), `Ai__WorkerDirectory` (thư mục worker),
  `Ai__ApiUrl` (URL backend dành cho worker). Máy triển khai cần Python và các dependency của từng AI.
- Backend tự chọn địa chỉ HTTP đang lắng nghe và kết nối qua localhost. Endpoint worker trên loopback
  không chuyển hướng HTTPS nhưng vẫn kiểm tra `X-AI-Key`. Nếu chỉ có HTTPS, cấu hình `Ai__ApiUrl`
  với chứng chỉ được Python tin cậy; không tắt xác minh TLS.
- Khi tắt quản lý tiến trình tự động, ON/OFF chỉ bật/tắt xử lý; bạn phải chạy worker bên ngoài.
- Backend: đặt `Ai__WorkerKey` (hoặc `Ai:WorkerKey` trong User Secrets) tối thiểu 32 ký tự ngẫu nhiên.
- Worker: đặt cùng giá trị vào `KBS_AI_WORKER_KEY` và đặt `KBS_API_URL` trỏ tới backend, rồi chạy
  `python ai/ai_auto.py`. Không dùng token Admin cho worker.
- Dùng HTTPS khi kết nối ngoài loopback; chứng chỉ phải được Python tin cậy, không tắt xác minh TLS.
- HTTPS dev certificate của .NET có thể không nằm trong trust store của Python. Local dùng profile HTTP
  như trên; production cấu hình chứng chỉ hợp lệ.
- Không đặt worker key trong biến `VITE_*`, mã frontend, source control hoặc URL.

Worker poll mỗi 5 giây (tối đa 50 đơn/lượt), timeout HTTP 15 giây; lỗi được retry ở vòng tiếp theo.
Nút tắt chặn quyết định mới phía server và supervisor dừng worker ở lượt kiểm tra tiếp theo.
Mất heartbeat trên 30 giây hiển thị mất kết nối khi module đang bật.
Thông báo nghiệp vụ/bật tắt/lỗi khởi động lưu trong AiEvents. Lỗi mạng phía worker ghi vào log backend.

## Quy tắc

Chỉ đơn `PENDING` có `Orders.TotalPrice < 10000000` mới chuyển `CONFIRMED`.
Đúng 10 triệu hoặc cao hơn giữ `PENDING`, ghi một thông báo cần staff xác nhận.
Server kiểm tra lại DB khi nhận quyết định; không thay đổi thanh toán/tồn kho.
Giao dịch Serializable và khóa module bảo vệ thao tác bật/tắt/worker đồng thời;
tranh chấp với staff/customer trả 409, worker đọc lại trong lượt tiếp theo.

## Kiểm tra

```powershell
python -m unittest discover -s ai
dotnet test backend/KBS.Tests/KBS.Tests.csproj --filter "FullyQualifiedName~AiManagementTests|FullyQualifiedName~AiWorkerSupervisorTests|FullyQualifiedName~OrderServiceTests"
npm --prefix frontend run test:smoke
npm --prefix frontend run build
```

Các test API mặc định dùng EF InMemory, không thay thế kiểm thử khóa/giao dịch đồng thời trên PostgreSQL.
Test supervisor cần Python 3, chạy worker thử nghiệm trong thư mục tạm và kiểm tra vòng đời tiến trình.
Thiết kế bảng, ánh xạ UI và contracts: `database/ai-management-design.md`.
