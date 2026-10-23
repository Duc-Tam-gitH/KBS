import { ArrowRight } from 'lucide-react'

export default function Footer({ onNavigate }: { onNavigate: (route: string) => void }) {
  return (
    <footer className="bg-elevated border-t border-border px-3 md:px-6 pt-16 md:pt-24 pb-32">
      <div className="max-w-7xl mx-auto">
        <div className="rounded-3xl md:rounded-[2rem] bg-deep border border-border p-8 md:p-12 lg:p-16 mb-12 flex flex-col md:flex-row gap-8 items-start md:items-center justify-between shadow-xl shadow-deep">
          <div>
            <p className="text-accent text-xs tracking-[0.2em] uppercase mb-4">KBS / FIND YOUR FEEL</p>
            <h2 className="font-display font-bold text-4xl md:text-5xl leading-tight text-light">
              Không chỉ là gõ.<br /><span className="font-serif italic font-normal text-accent">Mà là cảm giác.</span>
            </h2>
          </div>
          <button onClick={() => onNavigate('shop')}
            className="flex items-center gap-3 rounded-full bg-primary text-deep font-semibold px-7 py-4 hover:bg-accent hover:text-deep transition-colors shadow-lg shadow-deep">
            Khám phá bộ sưu tập <ArrowRight size={18} />
          </button>
        </div>
        <div className="grid md:grid-cols-2 gap-8 border-b border-border pb-10 mb-8">
          <div><p className="font-display font-bold text-3xl mb-3 text-light">KBS</p>
            <p className="text-muted max-w-md">Keyboard Beyond Standard — bàn phím cho nhịp làm việc của bạn.</p></div>
          <nav aria-label="Liên kết cuối trang" className="flex flex-wrap gap-6 items-center md:justify-end text-light">
            <button className="hover:text-accent transition-colors" onClick={() => onNavigate('home')}>Trang chủ</button>
            <button className="hover:text-accent transition-colors" onClick={() => onNavigate('shop')}>Sản phẩm</button>
            <button className="hover:text-accent transition-colors" onClick={() => onNavigate('likes')}>Yêu thích</button>
            <button className="hover:text-accent transition-colors" onClick={() => onNavigate('orders')}>Đơn hàng</button>
            <button className="hover:text-accent transition-colors" onClick={() => onNavigate('account')}>Tài khoản</button>
          </nav>
        </div>
        <p className="text-muted text-xs">© {new Date().getFullYear()} KBS — Keyboard Beyond Standard.</p>
      </div>
    </footer>
  )
}
