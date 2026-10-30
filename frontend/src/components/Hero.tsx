import { useHomeContent } from './useHomeContent'
import { useState } from 'react';
import { ArrowRight } from 'lucide-react';

type SwitchType = 'linear' | 'tactile' | 'clicky';

const switchOptions: Record<
  SwitchType,
  { label: string; desc: string; force: string }
> = {
  linear: {
    label: 'Linear',
    desc: 'Smooth, uninterrupted keystrokes. Built for speed — no bump, no click, just glide.',
    force: '45g actuation',
  },
  tactile: {
    label: 'Tactile',
    desc: 'A pronounced bump at actuation point. Feedback you can feel without bottoming out.',
    force: '55g actuation',
  },
  clicky: {
    label: 'Clicky',
    desc: 'An audible click on every press. Sound that satisfies with each keystroke.',
    force: '50g actuation',
  },
};

export default function Hero({ onNavigate }: { onNavigate: (route: string) => void }) {
  const get = useHomeContent();
  const [switchType, setSwitchType] = useState<SwitchType>('tactile');
  return (
    <section className="relative min-h-screen flex flex-col">
      {/* Thanh trên cùng */}
      <div className="absolute top-0 left-0 right-0 z-20 px-5 md:px-10 pt-6 md:pt-8 flex items-center justify-between">
        <div className="flex items-center gap-3">
          <span className="font-display font-bold text-2xl md:text-3xl text-light tracking-tight">{get('hero.text2')}</span>
          <span className="hidden md:inline-block h-6 w-px bg-dark" />
          <span className="hidden md:inline text-sm text-light font-medium tracking-wide">{get('hero.text3')}</span>
        </div>

        <div className="glass-cream rounded-full px-4 py-2 text-xs md:text-sm font-medium text-light">{get('hero.text4')}</div>
      </div>

      {/* Khung giới thiệu chính */}
      <div className="flex-1 px-3 md:px-6 pt-24 md:pt-28 pb-28 md:pb-32">
        <div className="relative h-full min-h-[560px] md:min-h-[640px] rounded-[1.5rem] md:rounded-[2.5rem] overflow-hidden group">
          {/* Ảnh nền */}
          <img
            src={get('hero.image1.src')}
            alt="Premium mechanical keyboard with illuminated keys"
            className="absolute inset-0 w-full h-full object-cover transition-transform duration-[1.2s] ease-out group-hover:scale-105"
          />

          {/* Các lớp phủ chuyển màu */}
          <div className="absolute inset-0 bg-gradient-to-t from-deep via-deep/70 to-deep/70" />
          <div className="absolute inset-0 bg-gradient-to-r from-deep/70 via-transparent to-transparent" />

          {/* Nút chuyển dạng viên thuốc — góc trên bên phải */}
          <div className="absolute top-5 right-5 md:top-7 md:right-7 z-10">
            <div className="glass-light flex items-center gap-1 rounded-full p-1">
              {(Object.keys(switchOptions) as SwitchType[]).map((type) => (
                <button
                  key={type}
                  onClick={() => setSwitchType(type)}
                  aria-pressed={switchType === type}
                  className={`px-3 md:px-4 py-1.5 md:py-2 rounded-full text-xs md:text-sm font-medium transition-all duration-300 ${
                    switchType === type
                      ? 'bg-primary text-deep font-semibold shadow-lg shadow-deep'
                      : 'text-light hover:text-accent'
                  }`}
                >
                  {get(`hero.${type}.label`)}
                </button>
              ))}
            </div>
          </div>

          {/* Nội dung giới thiệu chính */}
          <div className="absolute bottom-0 left-0 right-0 p-6 md:p-10 lg:p-14">
            <div className="max-w-2xl">
              <p className="text-accent text-xs md:text-sm font-medium tracking-[0.2em] uppercase mb-4 md:mb-6">{get('hero.text5')}</p>
              <h1 className="font-display font-bold text-light text-5xl sm:text-6xl md:text-7xl lg:text-[5.5rem] leading-[0.9] mb-5 md:mb-7">{get('hero.text6')}<br />
                <span className="font-serif italic font-normal text-accent">{get('hero.text7')}</span>
              </h1>
              <p className="text-muted text-base md:text-lg max-w-md mb-6 md:mb-8 leading-relaxed">
                {get(`hero.${switchType}.desc`)}
              </p>
              <div className="flex flex-wrap items-center gap-3 md:gap-4">
                <button onClick={() => onNavigate('shop')} className="group/btn flex items-center gap-2 bg-primary text-deep font-semibold rounded-full px-5 md:px-7 py-3 md:py-3.5 text-sm md:text-base hover:bg-accent hover:text-deep transition-all duration-300 shadow-xl shadow-deep">{get('hero.text8')}<span className="text-deep transition-colors">
                    →
                  </span>
                  <ArrowRight className="w-4 h-4 group-hover/btn:translate-x-1 transition-transform" />
                </button>
                <button onClick={() => document.getElementById('switches')?.scrollIntoView()} className="text-light border border-border rounded-full px-5 md:px-7 py-3 md:py-3.5 font-medium text-sm md:text-base hover:border-accent hover:text-accent hover:bg-border/30 transition-all duration-300">{get('hero.text9')}</button>
                <span className="hidden md:inline-flex items-center gap-2 text-muted text-sm ml-2">
                  <span className="w-1.5 h-1.5 rounded-full bg-primary animate-pulse" />
                  {get(`hero.${switchType}.force`)}
                </span>
              </div>
            </div>
          </div>
        </div>
      </div>
    </section>
  );
}
