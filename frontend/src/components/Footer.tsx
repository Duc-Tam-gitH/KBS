import { useHomeContent } from './useHomeContent'
import { ArrowRight, Facebook, Instagram, Twitter, Youtube, Linkedin, Mail } from 'lucide-react'

export default function Footer({ onNavigate }: { onNavigate: (route: string) => void }) {
  const get = useHomeContent()
  return (
    <footer className="bg-elevated border-t border-border px-4 md:px-8 pt-16 pb-8">
      <div className="max-w-7xl mx-auto">
        
        {/* Lưới 4 cột chính */}
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-12 mb-16">
          
          {/* Cột 1: Logo, Mô tả & Social */}
          <div className="flex flex-col gap-6">
            <div className="flex items-center gap-2 text-light">
              {/* Icon Logo giả lập (bạn có thể thay bằng SVG logo của bạn) */}
              <div className="w-8 h-8 rounded-lg bg-primary flex items-center justify-center font-bold text-deep">{get('footer.text1')}</div>
              <span className="font-display font-bold text-2xl tracking-tight">{get('footer.text2')}</span>
            </div>
            <p className="text-muted text-sm leading-relaxed max-w-xs">{get('footer.text3')}</p>
            <div className="flex items-center gap-3">
              {[Facebook, Twitter, Instagram, Youtube, Linkedin].map((Icon, index) => (
                <a
                  key={index}
                  href="#"
                  className="w-9 h-9 rounded-full border border-border flex items-center justify-center text-muted hover:text-accent hover:border-accent transition-colors duration-300"
                  aria-label="Social Link"
                >
                  <Icon size={16} />
                </a>
              ))}
            </div>
          </div>

          {/* Cột 2: Quick Links */}
          <div className="flex flex-col gap-4">
            <h3 className="font-semibold text-light text-base mb-2">{get('footer.text4')}</h3>
            <nav className="flex flex-col gap-3 text-sm text-muted">
              <button className="text-left hover:text-accent transition-colors" onClick={() => onNavigate('home')}>{get('footer.text5')}</button>
              <button className="text-left hover:text-accent transition-colors" onClick={() => onNavigate('shop')}>{get('footer.text6')}</button>
              <button className="text-left hover:text-accent transition-colors" onClick={() => onNavigate('likes')}>{get('footer.text7')}</button>
              <button className="text-left hover:text-accent transition-colors" onClick={() => onNavigate('orders')}>{get('footer.text8')}</button>
              <button className="text-left hover:text-accent transition-colors" onClick={() => onNavigate('account')}>{get('footer.text9')}</button>
            </nav>
          </div>

          {/* Cột 3: Support (Hỗ trợ) */}
          <div className="flex flex-col gap-4">
            <h3 className="font-semibold text-light text-base mb-2">{get('footer.text10')}</h3>
            <nav className="flex flex-col gap-3 text-sm text-muted">
              <a href="#" className="hover:text-accent transition-colors">{get('footer.text11')}</a>
              <a href="#" className="hover:text-accent transition-colors">{get('footer.text12')}</a>
              <a href="#" className="hover:text-accent transition-colors">{get('footer.text13')}</a>
              <a href="#" className="hover:text-accent transition-colors">{get('footer.text14')}</a>
              <a href="#" className="hover:text-accent transition-colors">{get('footer.text15')}</a>
            </nav>
          </div>

          {/* Cột 4: Subscribe (Đăng ký nhận tin) */}
          <div className="flex flex-col gap-4">
            <h3 className="font-semibold text-light text-base mb-2">{get('footer.text16')}</h3>
            <p className="text-muted text-sm mb-2">{get('footer.text17')}</p>
            <form className="flex items-center w-full" onSubmit={(e) => e.preventDefault()}>
              <div className="relative w-full flex">
                <Mail className="absolute left-3 top-1/2 -translate-y-1/2 text-muted" size={16} />
                <input 
                  type="email" 
                  placeholder="Địa chỉ email của bạn" 
                  className="w-full bg-deep border border-border text-light text-sm rounded-l-lg py-2.5 pl-10 pr-3 focus:outline-none focus:border-accent transition-colors"
                  required
                />
                <button 
                  type="submit"
                  className="bg-primary text-deep px-4 py-2.5 rounded-r-lg hover:bg-accent transition-colors flex items-center justify-center"
                  aria-label="Đăng ký"
                >
                  <ArrowRight size={18} />
                </button>
              </div>
            </form>
          </div>

        </div>

        {/* Thanh ngang phân cách */}
        <div className="border-t border-border pt-6 flex flex-col md:flex-row items-center justify-between gap-4">
          <p className="text-muted text-xs">
            © {new Date().getFullYear()} KBS — Keyboard Beyond Standard. All rights reserved.
          </p>
          <div className="flex gap-6 text-xs text-muted">
            <a href="#" className="hover:text-accent transition-colors">{get('footer.text18')}</a>
            <a href="#" className="hover:text-accent transition-colors">{get('footer.text19')}</a>
            <a href="#" className="hover:text-accent transition-colors">{get('footer.text20')}</a>
          </div>
        </div>

      </div>
    </footer>
  )
}