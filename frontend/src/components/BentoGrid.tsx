import { useHomeContent } from './useHomeContent'
import { ArrowUpRight, Check } from 'lucide-react';
import { useScrollReveal } from '@/hooks/useScrollReveal';

const features = [
  'Gasket-mounted design',
  'Sound-dampened foam',
  'CNC aluminum chassis',
  'Triple-mode wireless',
];

export default function BentoGrid({ onNavigate }: { onNavigate: (route: string) => void }) {
  const get = useHomeContent();
  const ref = useScrollReveal<HTMLDivElement>();
  return (
    <section className="px-3 md:px-6 py-12 md:py-20">
      <div ref={ref} className="max-w-7xl mx-auto">
        {/* Tiêu đề phần */}
        <div className="reveal mb-8 md:mb-12 flex items-end justify-between flex-wrap gap-4">
          <div>
            <p className="text-sm font-medium tracking-[0.2em] uppercase text-accent mb-3">{get('bento.text2')}</p>
            <h2 className="font-display font-bold text-4xl md:text-6xl text-light leading-[0.95]">{get('bento.text3')}<br />
              <span className="font-serif italic font-normal">{get('bento.text4')}</span>
            </h2>
          </div>
          <p className="text-light max-w-xs text-sm md:text-base">{get('bento.text5')}</p>
        </div>

        {/* Lưới bố cục Bento */}
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-3 md:gap-4 lg:gap-5 auto-rows-[minmax(200px,auto)] lg:auto-rows-[230px]">
          {/* Khung biểu mẫu 1 — Ảnh không gian sử dụng cỡ lớn */}
          <div className="reveal group relative lg:col-span-2 lg:row-span-2 rounded-3xl md:rounded-[2rem] overflow-hidden min-h-[320px] lg:min-h-0">
            <img
              src={get('bento.image1.src')}
              alt="Desk setup with mechanical keyboard"
              className="absolute inset-0 w-full h-full object-cover transition-transform duration-700 group-hover:scale-105"
            />
            <div className="absolute inset-0 bg-gradient-to-t from-deep/80 via-deep/70 to-transparent" />
            <div className="absolute bottom-0 left-0 p-6 md:p-8">
              <p className="text-light text-xs font-medium tracking-widest uppercase mb-2">{get('bento.text6')}</p>
              <h3 className="font-display font-bold text-light text-2xl md:text-4xl leading-tight mb-2">{get('bento.text7')}</h3>
              <p className="text-light text-sm max-w-xs">{get('bento.text8')}</p>
            </div>
          </div>

          {/* Khung biểu mẫu 2 — Chi tiết phím */}
          <div className="reveal group relative lg:col-span-1 lg:row-span-1 rounded-3xl overflow-hidden min-h-[220px] lg:min-h-0">
            <img
              src={get('bento.image2.src')}
              alt="Colorful mechanical keyboard keycaps"
              className="absolute inset-0 w-full h-full object-cover transition-transform duration-700 group-hover:scale-110"
            />
            <div className="absolute inset-0 bg-gradient-to-t from-deep/70 to-transparent" />
            <div className="absolute bottom-0 left-0 p-5">
              <p className="text-light text-[10px] font-medium tracking-widest uppercase mb-1">{get('bento.text9')}</p>
              <h3 className="font-display font-semibold text-light text-lg">{get('bento.text10')}</h3>
            </div>
          </div>

          {/* Khung biểu mẫu 3 — Khẩu hiệu */}
          <div className="reveal lg:col-span-1 lg:row-span-1 rounded-3xl bg-elevated border border-border flex flex-col justify-between p-6 md:p-7 min-h-[220px] lg:min-h-0">
            <div className="flex items-start justify-between">
              <span className="text-accent text-xs font-medium tracking-widest uppercase">{get('bento.text11')}</span>
              <ArrowUpRight className="w-4 h-4 text-light" />
            </div>
            <div>
              <p className="font-display font-bold text-light text-3xl md:text-4xl leading-[0.9]">{get('bento.text12')}<br />
                <span className="font-serif italic font-normal text-accent">{get('bento.text13')}</span>
              </p>
            </div>
          </div>

          {/* Khung biểu mẫu 4 — Switch khổ rộng */}
          <div className="reveal group relative lg:col-span-2 lg:row-span-1 rounded-3xl overflow-hidden min-h-[220px] lg:min-h-0">
            <img
              src={get('bento.image3.src')}
              alt="Mechanical keyboard switches close-up"
              className="absolute inset-0 w-full h-full object-cover transition-transform duration-700 group-hover:scale-105"
            />
            <div className="absolute inset-0 bg-gradient-to-r from-deep/80 to-deep/70" />
            <div className="absolute inset-0 flex items-center p-6 md:p-8">
              <div className="max-w-sm">
                <p className="text-accent text-xs font-medium tracking-widest uppercase mb-2">{get('bento.text14')}</p>
                <h3 className="font-display font-bold text-light text-2xl md:text-3xl leading-tight mb-2">{get('bento.text15')}</h3>
                <p className="text-muted text-sm">{get('bento.text16')}</p>
              </div>
            </div>
          </div>

          {/* Khung biểu mẫu 5 — Đèn RGB khổ rộng */}
          <div className="reveal group relative md:col-span-2 lg:col-span-2 lg:row-span-1 rounded-3xl overflow-hidden min-h-[220px] lg:min-h-0">
            <img
              src={get('bento.image4.src')}
              alt="RGB mechanical keyboards with transparent keycaps"
              className="absolute inset-0 w-full h-full object-cover transition-transform duration-700 group-hover:scale-105"
            />
            <div className="absolute inset-0 bg-gradient-to-t from-deep/70 via-transparent to-transparent" />
            <div className="absolute bottom-0 left-0 p-6">
              <p className="text-accent text-xs font-medium tracking-widest uppercase mb-1">{get('bento.text17')}</p>
              <h3 className="font-display font-bold text-light text-xl md:text-2xl">{get('bento.text18')}</h3>
            </div>
          </div>

          {/* Khung biểu mẫu 6 — Thao tác gõ phím */}
          <div className="reveal group relative lg:col-span-1 lg:row-span-1 rounded-3xl overflow-hidden min-h-[220px] lg:min-h-0">
            <img
              src={get('bento.image5.src')}
              alt="Thao tác gõ phím on RGB mechanical keyboard"
              className="absolute inset-0 w-full h-full object-cover transition-transform duration-700 group-hover:scale-110"
            />
            <div className="absolute inset-0 bg-gradient-to-t from-deep/70 to-transparent" />
            <div className="absolute bottom-0 left-0 p-5">
              <h3 className="font-display font-semibold text-light text-base">{get('bento.text19')}</h3>
            </div>
          </div>

          {/* Khung biểu mẫu 7 — Danh sách tính năng */}
          <div className="reveal lg:col-span-1 lg:row-span-1 glass-cream border border-border rounded-3xl p-6 min-h-[220px] lg:min-h-0 flex flex-col justify-between">
            <p className="text-xs font-medium tracking-widest uppercase text-accent">{get('bento.text20')}</p>
            <ul className="space-y-3">
              {features.map((feature, index) => (
                <li key={index} className="flex items-center gap-3">
                  <span className="flex items-center justify-center w-5 h-5 rounded-full bg-deep text-accent shrink-0 border border-border">
                    <Check className="w-3 h-3" />
                  </span>
                  <span className="text-sm text-light font-medium">
                    {get(`bento.feature${index}`)}
                  </span>
                </li>
              ))}
            </ul>
          </div>

          {/* Khung biểu mẫu 8 — Banner kêu gọi hành động khổ rộng */}
          <div className="reveal group relative md:col-span-2 lg:col-span-4 lg:row-span-1 rounded-3xl md:rounded-[2rem] overflow-hidden min-h-[260px] lg:min-h-0 border border-border">
            <img
              src={get('bento.image6.src')}
              alt="Colorful mechanical keyboard on wooden desk"
              className="absolute inset-0 w-full h-full object-cover transition-transform duration-700 group-hover:scale-105"
            />
            <div className="absolute inset-0 bg-gradient-to-r from-deep/85 via-deep/70 to-transparent" />
            <div className="absolute inset-0 flex items-center p-6 md:p-10">
              <div className="max-w-lg">
                <p className="text-accent text-xs font-medium tracking-widest uppercase mb-3">{get('bento.text21')}</p>
                <h3 className="font-display font-bold text-light text-3xl md:text-5xl leading-[0.95] mb-4">{get('bento.text22')}<br />
                  <span className="font-serif italic font-normal text-accent">{get('bento.text23')}</span>
                </h3>
                <button onClick={() => onNavigate('shop')} className="group/btn flex items-center gap-2 bg-primary text-deep font-semibold rounded-full px-6 py-3 text-sm hover:bg-accent hover:text-deep transition-all duration-300 shadow-lg shadow-deep">{get('bento.text24')}<ArrowUpRight className="w-4 h-4 text-deep group-hover/btn:translate-x-0.5 group-hover/btn:-translate-y-0.5 transition-transform" />
                </button>
              </div>
            </div>
          </div>
        </div>
      </div>
    </section>
  );
}
