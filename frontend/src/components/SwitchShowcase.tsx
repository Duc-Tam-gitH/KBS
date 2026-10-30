import { useHomeContent } from './useHomeContent'
import { Zap, Star, Volume2 } from 'lucide-react';
import { useScrollReveal } from '@/hooks/useScrollReveal';

type Switch = {
  type: string;
  name: string;
  desc: string;
  force: string;
  travel: string;
  image: string;
  icon: typeof Zap;
};

const switches: Switch[] = [
  {
    type: 'Linear',
    name: 'Speed',
    desc: 'Smooth, frictionless travel from top to bottom. No bump, no click — just pure glide.',
    force: '45g',
    travel: '4mm',
    image:
      'https://images.pexels.com/photos/6460801/pexels-photo-6460801.jpeg?auto=compress&cs=tinysrgb&w=800',
    icon: Zap,
  },
  {
    type: 'Tactile',
    name: 'Feedback',
    desc: 'A pronounced tactile bump lets you feel exactly when the switch actuates.',
    force: '55g',
    travel: '4mm',
    image:
      'https://images.pexels.com/photos/1320916/pexels-photo-1320916.jpeg?auto=compress&cs=tinysrgb&w=800',
    icon: Star,
  },
  {
    type: 'Clicky',
    name: 'Presence',
    desc: 'An audible click on every keystroke. For those who want the room to know they are typing.',
    force: '50g',
    travel: '4mm',
    image:
      'https://images.pexels.com/photos/18337017/pexels-photo-18337017.jpeg?auto=compress&cs=tinysrgb&w=800',
    icon: Volume2,
  },
];

export default function SwitchShowcase() {
  const get = useHomeContent();
  const ref = useScrollReveal<HTMLDivElement>();
  return (
    <section id="switches" className="px-3 md:px-6 py-16 md:py-24 bg-elevated border-t border-border relative overflow-hidden">
      {/* Ánh sáng nhẹ */}
      <div className="absolute top-0 left-1/2 -translate-x-1/2 w-[600px] h-[300px] bg-primary/10 blur-[120px] rounded-full pointer-events-none" />

      <div ref={ref} className="max-w-7xl mx-auto relative">
        {/* Phần đầu */}
        <div className="reveal mb-10 md:mb-16 flex items-end justify-between flex-wrap gap-4">
          <div>
            <p className="text-sm font-medium tracking-[0.2em] uppercase text-accent mb-3">{get('switches.text2')}</p>
            <h2 className="font-display font-bold text-4xl md:text-6xl lg:text-7xl text-light leading-[0.95]">{get('switches.text3')}<br />
              <span className="font-serif italic font-normal text-accent">{get('switches.text4')}</span>
            </h2>
          </div>
          <p className="text-muted max-w-xs text-sm md:text-base">{get('switches.text5')}</p>
        </div>

        {/* Các thẻ switch */}
        <div className="grid grid-cols-1 md:grid-cols-3 gap-3 md:gap-5">
          {switches.map((sw, i) => {
            const Icon = sw.icon;
            return (
              <div
                key={i}
                className="reveal group relative rounded-3xl overflow-hidden cursor-pointer min-h-[380px] md:min-h-[440px] border border-border hover:border-accent transition-colors"
              >
                <img
                  src={get(`switches.card${i}.image`)}
                  alt={`${get(`switches.card${i}.type`)} mechanical keyboard switches`}
                  className="absolute inset-0 w-full h-full object-cover transition-transform duration-700 group-hover:scale-110"
                />
                <div className="absolute inset-0 bg-gradient-to-t from-deep via-deep/70 to-deep/70" />
                <div className="absolute inset-0 p-6 md:p-8 flex flex-col justify-between">
                  {/* Phần trên */}
                  <div className="flex items-center justify-between">
                    <div className="glass-light flex items-center gap-2 rounded-full px-3 py-1.5 border border-border">
                      <Icon className="w-3.5 h-3.5 text-accent" />
                      <span className="text-xs font-medium tracking-widest uppercase text-light">
                        {get(`switches.card${i}.type`)}
                      </span>
                    </div>
                    <span className="text-accent text-sm font-display font-bold">
                      0{i + 1}
                    </span>
                  </div>

                  {/* Phần dưới */}
                  <div>
                    <h3 className="font-display font-bold text-light text-3xl md:text-4xl mb-2">
                      {get(`switches.card${i}.name`)}
                    </h3>
                    <p className="text-muted text-sm mb-5 leading-relaxed">
                      {get(`switches.card${i}.desc`)}
                    </p>
                    <div className="flex items-center gap-4 pt-4 border-t border-border">
                      <div>
                        <p className="text-muted text-[10px] font-medium tracking-widest uppercase">{get('switches.text6')}</p>
                        <p className="text-light font-display font-semibold text-sm">
                          {get(`switches.card${i}.force`)}
                        </p>
                      </div>
                      <div className="h-8 w-px bg-primary" />
                      <div>
                        <p className="text-muted text-[10px] font-medium tracking-widest uppercase">{get('switches.text7')}</p>
                        <p className="text-light font-display font-semibold text-sm">
                          {get(`switches.card${i}.travel`)}
                        </p>
                      </div>
                    </div>
                  </div>
                </div>
              </div>
            );
          })}
        </div>
      </div>
    </section>
  );
}
