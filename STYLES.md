## PROJECT COLOR PALETTE

### Bắt buộc sử dụng đúng bảng màu
Mọi thành phần UI/giao diện trong project phải chỉ sử dụng các màu trong bảng màu dưới đây. Không tự ý thêm màu mới ngoài danh sách này (trừ màu trắng #FFFFFF, đen #000000 cho text thuần nếu thực sự cần, và màu trong suốt transparent).

| Mã màu  | Tên            | Vai trò thực tế trong UI |
|---------|----------------|--------------------------|
| #050505 | Void Black     | Nền tảng chính (App background). Đen tuyền tĩnh lặng, tạo không gian bí ẩn và tập trung cao độ của các sòng bài VIP ngầm. |
| #171717 | Velvet Dark    | Nền nổi (Card, Modal, Header). Đen nhám gợi cảm giác mặt bàn nỉ hoặc da bọc cao cấp, làm bệ đỡ để tôn sắc đỏ. |
| #4D0000 | Bloodline      | Đường viền (Border), dải phân cách. Đỏ sẫm như màu rượu vang, định hình khung giao diện kịch tính nhưng không làm chói mắt. |
| #CC0000 | Jackpot Red    | Nút CTA chính (Mua ngay, Đặt cược), Badge. Đỏ nguyên bản của bàn roulette, hối thúc hành động, kích thích sự liều lĩnh và hưng phấn. |
| #D4AF37 | Casino Gold    | Trạng thái Hover, Link, Icon, Checkbox. Vàng ánh kim. Chấm phá bắt buộc để trung hòa sắc đỏ - đen, đại diện cho chip cược, tiền tài và chiến thắng. |
| #9CA3AF | Ash Gray       | Văn bản phụ (Mô tả, placeholder). Xám tro trầm tĩnh, làm nền nhường toàn bộ spotlight cho các điểm nhấn đỏ và vàng. |
| #F3F4F6 | Bone White     | Văn bản chính, Tiêu đề. Trắng ngà sắc nét, đảm bảo tính minh bạch và dễ đọc tuyệt đối cho các con số, giá tiền hay tỷ lệ cược. |

### Quy tắc áp dụng

1. Định nghĩa tập trung: Khai báo toàn bộ palette tại một nơi duy nhất (CSS variables / theme file / design tokens). Ví dụ:

```css
:root {
  --color-bg-deep:    #050505; /* Void Black */
  --color-bg-elevated:#171717; /* Velvet Dark */
  --color-border:     #4D0000; /* Bloodline */
  --color-primary:    #CC0000; /* Jackpot Red */
  --color-accent:     #D4AF37; /* Casino Gold */
  --color-text-muted: #9CA3AF; /* Ash Gray */
  --color-text-light: #F3F4F6; /* Bone White */
}