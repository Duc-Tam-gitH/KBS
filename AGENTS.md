# Quy Tắc Làm Việc (Agent Rules)

## 1. Context Engineering
Tối ưu hóa và tái sử dụng ngữ cảnh dự án để tránh lặp lại giữa các phiên làm việc.
- Kích hoạt ngay khi: bắt đầu session mới, chuyển đổi task, hoặc AI bắt đầu trả lời sai hướng.
- **Ưu tiên cao nhất** để tiết kiệm dung lượng ngữ cảnh (context window).

## 2. Incremental Implementation
Chia nhỏ tính năng thành các module/hàm độc lập.
- Hoàn thiện và kiểm tra chạy thử nghiệm từng phần một.
- **Tuyệt đối không** sinh toàn bộ mã nguồn của một hệ thống lớn trong một lần phản hồi để tránh phải sửa lỗi hàng loạt.

## 3. Debugging & Error Recovery
Luôn phân tích log và tìm ra nguyên nhân gốc rễ (root cause) trước khi đề xuất code sửa lỗi.
- **Nghiêm cấm** đoán mò hoặc thử sai (trial-and-error) khiến code bị rác và làm dài lịch sử hội thoại.

## 4. Test-Driven Development
Đặt ra các tiêu chí kiểm thử (test cases/acceptance criteria) rõ ràng **trước khi** viết code.
- Bắt buộc áp dụng khi: triển khai logic nghiệp vụ (business logic), thiết kế API, hoặc sửa bug — để đảm bảo lỗi không tái phát.

## 5. Code Simplification / Ponytail
Mặc định chọn giải pháp ngắn gọn, đơn giản và trực diện nhất.
- Không lạm dụng tính trừu tượng (abstraction).
- Không thêm thư viện (packages) ngoài.
- Không áp dụng các mẫu kiến trúc phức tạp nếu không thực sự cần thiết.
- Áp dụng triệt để khi refactor hoặc review code.

## 6. Planning & API Design
Phải có bản thiết kế trước khi thực hiện các tính năng lớn.
- Định nghĩa rõ các Data Transfer Objects (DTO), API contracts (request/response).
- Xác định ranh giới trách nhiệm giữa các tầng: Models → Data Access Layer (DAL) → Business Logic Layer (BLL) → API Controller **trước khi** bắt tay vào viết code thực thi.


# Quy tắc format code C#

## Xuống dòng
- Không viết nhiều attribute trên cùng một dòng. Mỗi `[HttpGet]`, `[Authorize]`,... đặt trên dòng riêng.
- Method có từ 3 tham số trở lên: mỗi tham số một dòng.
- Dòng code không vượt quá 120 ký tự. Nếu dài hơn, xuống dòng tại vị trí hợp lý (sau dấu phẩy, trước toán tử, hoặc sau dấu `=>`).
- Không viết `try { ... } catch { ... }` trên một dòng; mỗi block `{ }` phải mở và đóng rõ ràng.

## Ví dụ
Không viết:
`[Authorize(Roles = "Admin")][HttpPost] public async Task<...> Create(...) { try { ... } catch { ... } }`

Mà viết:
```csharp
[Authorize(Roles = "Admin")]
[HttpPost]
public async Task<ActionResult<ProductResponseDto>> Create(
    ProductCreateDto dto,
    CancellationToken ct)
{
    try
    {
        // ...
    }
    catch (ArgumentException e)
    {
        // ...
    }
}

# Quy tắc màu sắc project (Project Color Palette)

## Bắt buộc sử dụng đúng bảng màu
Mọi thành phần UI/giao diện trong project phải chỉ sử dụng các màu trong bảng màu dưới đây. Không tự ý thêm màu mới ngoài danh sách này (trừ màu trắng #FFFFFF, đen #000000 cho text thuần nếu thực sự cần, và màu trong suốt transparent).

| Mã màu  | Tên            | Vai trò thực tế trong UI                                                                 |
|---------|----------------|------------------------------------------------------------------------------------------|
| #050512 | Void Black     | Nền tảng chính (App background). Đen tuyền pha chút xanh tím sẫm, tạo không gian sâu như màn đêm đô thị. |
| #121226 | Cyber Metal    | Nền nổi (Card sản phẩm, Modal, Header). Tách biệt sắc nét khỏi nền chính để làm bệ đỡ làm nổi bật hình ảnh bàn phím. |
| #2D2E54 | Grid Blue      | Đường viền (Border), dải phân cách. Định hình khung giao diện sắc sảo mang cảm giác của các bảng mạch điện tử. |
| #FF2A6D | Neon Pink      | Nút CTA chính (Thêm giỏ hàng, Mua ngay), Badge giảm giá. Màu sắc rực rỡ đóng vai trò như ánh đèn neon đập ngay vào mắt người dùng. |
| #05D9E8 | Electric Cyan  | Trạng thái Hover, Link, Icon, Checkbox. Bổ trợ và tương phản chéo với Neon Pink, tạo ra cặp màu Cyberpunk kinh điển. |
| #8A9BB3 | Steel Gray     | Văn bản phụ (Mô tả ngắn, placeholder, ngày tháng). Xám thép lạnh giúp cân bằng thị giác, không tranh giành sự chú ý với hình ảnh sản phẩm. |
| #E0F2FE | Tech White     | Văn bản chính, Tiêu đề. Trắng ngả xanh dương băng giá (Ice white) mang đậm chất kỹ thuật số thay vì màu trắng ngà ấm áp. |

## Quy tắc áp dụng

1. Định nghĩa tập trung: Khai báo toàn bộ palette tại một nơi duy nhất (CSS variables / theme file / design tokens). Ví dụ:

    :root {
      --color-bg-deep:    #050512; /* Void Black */
      --color-bg-elevated:#121226; /* Cyber Metal */
      --color-border:     #2D2E54; /* Grid Blue */
      --color-primary:    #FF2A6D; /* Neon Pink */
      --color-accent:     #05D9E8; /* Electric Cyan */
      --color-text-muted: #8A9BB3; /* Steel Gray */
      --color-text-light: #E0F2FE; /* Tech White */
    }

   Không hard-code mã hex rải rác trong code — luôn dùng biến/token.

2. Độ tương phản: Đảm bảo text đạt chuẩn WCAG AA (tương phản >= 4.5:1). Ưu tiên cặp:
   - Text #E0F2FE trên nền #050512 / #121226 / #2D2E54
   - Text #050512 trên nền #FF2A6D / #05D9E8 (dùng cho button/CTA rực sáng)
   - Text #8A9BB3 chỉ dùng cho text phụ trên nền #050512 / #121226 (kiểm tra lại nếu đặt trên nền sáng)
   - TUYỆT ĐỐI KHÔNG dùng chữ trắng #FFFFFF trên nền #FF2A6D hoặc #05D9E8 (không đạt AA — xem bảng kiểm tra bên dưới).

3. Phân cấp thị giác: Dùng màu nền đậm→nhạt theo thứ tự: #050512 → #121226 → #2D2E54. Hai màu neon (#FF2A6D, #05D9E8) chỉ dùng cho CTA / hover / link / icon / accent — KHÔNG dùng làm nền lớn (tránh chói và mất cân bằng).

4. Trạng thái (states):
   - Hover: chuyển sang #05D9E8 (Electric Cyan) cho link/icon; CTA hover có thể dùng #FF2A6D + opacity hoặc glow cyan viền ngoài.
   - Active/Pressed: #FF2A6D (giữ nguyên CTA chính) hoặc #05D9E8 cho control phụ.
   - Disabled: dùng màu nền + opacity giảm (không tạo màu xám mới).

5. Không dùng gradient màu ngoài palette. Nếu cần gradient, chỉ gradient giữa các màu trong bảng. Ví dụ hợp lệ:
   - Nền hero: linear-gradient(135deg, #050512 0%, #121226 60%, #2D2E54 100%)
   - CTA neon: linear-gradient(135deg, #FF2A6D 0%, #05D9E8 100%) — cặp Cyberpunk kinh điển
   - Glow viền card: linear-gradient(135deg, #FF2A6D, #05D9E8)
   KHÔNG dùng gradient với màu ngoài palette.

6. Light mode (nếu có): đảo vai trò — nền #E0F2FE, text #050512, accent #FF2A6D / #05D9E8. Vẫn tuyệt đối không thêm màu ngoài palette.

## Bảng kiểm tra tương phản (WCAG AA >= 4.5:1)

| Cặp màu                          | Tỷ lệ (xấp xỉ) | Kết luận       |
|----------------------------------|----------------|----------------|
| #E0F2FE trên #050512             | ~17.5:1        | AAA            |
| #E0F2FE trên #121226             | ~15.0:1        | AAA            |
| #E0F2FE trên #2D2E54             | ~8.5:1         | AAA            |
| #8A9BB3 trên #050512             | ~8.0:1         | AAA            |
| #8A9BB3 trên #121226             | ~6.8:1         | AA             |
| #FF2A6D trên #050512             | ~6.5:1         | AA             |
| #05D9E8 trên #050512             | ~13.0:1        | AAA            |
| #050512 trên #FF2A6D             | ~6.5:1         | AA             |
| #050512 trên #05D9E8             | ~13.0:1        | AAA            |
| #FFFFFF trên #FF2A6D             | ~3.0:1         | KHÔNG DÙNG     |
| #FFFFFF trên #05D9E8             | ~1.5:1         | KHÔNG DÙNG     |

→ Quy tắc vàng: chữ trên nền neon (#FF2A6D / #05D9E8) luôn là #050512 (Void Black), không bao giờ trắng.

## Ánh xạ palette cũ → mới (dùng khi refactor codebase)

| Màu cũ  | Màu mới  |
|---------|----------|
| #051F20 | #050512 |
| #0B2B26 | #121226 |
| #163832 | #2D2E54 |
| #235347 | #FF2A6D |
| #8EB69B | #05D9E8 |
| #DAF1DE | #E0F2FE |

(Phụ lục tạm thời — xóa sau khi refactor xong toàn bộ codebase.)

# Agent Rules — Nhận diện giao diện & Kết nối Database

## 1. Nguyên tắc cốt lõi
Mọi dữ liệu hiển thị trên giao diện **phải đến từ database thật** của dự án.
AI **không được phép** tự tạo, bịa, hoặc giữ lại dữ liệu mẫu (mock/placeholder/hard-code) trong bất kỳ trường hợp nào.

## 2. Quy trình bắt buộc khi nhận một giao diện mới

### Bước 1 — Đọc schema database trước
Trước khi viết bất kỳ dòng code nào, AI phải:
1. Đọc danh sách bảng (tables) và cột (columns) từ schema/migration/ERD của dự án.
2. Xác định khóa chính (PK), khóa ngoại (FK), và quan hệ giữa các bảng.
3. Nếu chưa có schema, **dừng lại và yêu cầu người dùng cung cấp**, không được đoán.

### Bước 2 — Lập bảng ánh xạ (Mapping Table)
Với mỗi phần tử dữ liệu trên giao diện, AI phải lập bảng ánh xạ rõ ràng:

| Vị trí trên UI | Nguồn dữ liệu (bảng.cột) | Ghi chú |
|---|---|---|
| Tên user (sidebar) | `users.full_name` | |
| Avatar user | `users.avatar_url` | |
| Menu trạng thái đơn | `orders.status` (enum) | |
| Tên shop trong đơn | `shops.name` (JOIN qua `orders.shop_id`) | |
| Tên sản phẩm | `order_items.product_name` | Snapshot tại thời điểm mua |
| Giá sản phẩm | `order_items.price` | |
| Phân loại (size, màu) | `order_items.variant` | |
| Số lượng | `order_items.quantity` | |
| Thành tiền | `orders.total_amount` | |
| Ảnh sản phẩm | `products.image_url` | |

**Quy tắc:**
- Không có dòng nào trong bảng ánh xạ được để trống nguồn dữ liệu.
- Nếu một phần tử UI không tìm thấy cột tương ứng → **báo lại cho người dùng**, không tự tạo cột mới và không tự bịa giá trị.

### Bước 3 — Loại bỏ toàn bộ dữ liệu mẫu
Trước khi trả kết quả, quét toàn bộ code và **xóa/thay thế** mọi thứ sau:
- Tên người, email, SĐT, địa chỉ bịa đặt.
- Ảnh placeholder (`via.placeholder.com`, `placehold.co`, `dummyimage.com`, `picsum.photos`...).
- Giá tiền, số lượng, thống kê tự chế.
- Chuỗi `lorem ipsum`, `Sản phẩm mẫu`, `Nguyễn Văn A`...
- Hard-code text trạng thái (`Chờ giao hàng`, `Hoàn thành`...) nếu trạng thái đó phải render từ enum trong DB.

**Thay bằng:**
- Biến bind từ DB (ví dụ: `user.full_name`, `order.status`).
- Hoặc placeholder có đánh dấu rõ: `{{ user.full_name }}`.
- Hoặc empty state hợp lệ: `Chưa có dữ liệu`, `--`.

### Bước 4 — Xử lý trạng thái rỗng (Empty State)
Mọi danh sách (đơn hàng, sản phẩm, thông báo...) phải có xử lý khi DB trả về rỗng:
- Hiển thị text trung tính: `Bạn chưa có đơn hàng nào`, `Chưa có sản phẩm`.
- Không được dùng dữ liệu giả để "lấp chỗ trống".

### Bước 5 — Kiểm tra trước khi bàn giao
Checklist bắt buộc:
- [ ] Đã đọc schema DB.
- [ ] Đã lập bảng ánh xạ UI → DB cho mọi trường dữ liệu.
- [ ] Không còn bất kỳ mock/placeholder data nào trong code.
- [ ] Không còn ảnh placeholder từ dịch vụ bên ngoài.
- [ ] Các trường không map được đã được báo lại cho người dùng.
- [ ] Có xử lý empty state cho mọi danh sách.
- [ ] Không tự tạo bảng/cột mới khi chưa được duyệt.

## 3. Ngoại lệ
Chỉ được dùng dữ liệu mẫu khi người dùng **yêu cầu rõ ràng** (ví dụ: *"tạo mock data để test UI"*). Khi đó phải:
- Ghi chú rõ trong code: `// MOCK DATA — sẽ thay bằng DB`.
- Tách biệt mock data khỏi phần code kết nối DB thật.

## 4. Hành vi khi thiếu thông tin
- Thiếu schema DB → **dừng lại, hỏi người dùng**.
- Thiếu cột cho một trường UI → **báo lại, không tự bịa**.
- Yêu cầu mơ hồ → **hỏi lại trước khi code**.

## 5. Ví dụ

### ❌ Sai
```html
<div class="user-card__name">Nguyễn Văn A</div>
<img src="https://via.placeholder.com/60" alt="Avatar">
<span class="price">60.700đ</span>
<span class="status">Chờ giao hàng</span>

