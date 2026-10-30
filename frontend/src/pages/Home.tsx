import { useHomeContent } from '../components/useHomeContent'
import Hero from '@/components/Hero';
import BentoGrid from '@/components/BentoGrid';
import SwitchShowcase from '@/components/SwitchShowcase';
import './Home.css';

const marqueeItems = [
  'Hot-Swappable',
  'Gasket-Mounted',
  'CNC Aluminum',
  'Per-Key RGB',
  'Triple-Mode Wireless',
  'PBT Keycaps',
  'Sound Dampened',
  'QMK/VIA Compatible',
];

function Marquee() {
  const get = useHomeContent()
  return (
    <div className="overflow-hidden border-y border-border bg-elevated py-4">
      <div className="flex whitespace-nowrap animate-marquee">
        {[0, 1].map((dup) => (
          <div key={dup} className="flex items-center shrink-0">
            {marqueeItems.map((item, index) => (
              <span
                key={`${dup}-${index}`}
                className="mx-6 text-sm font-medium tracking-widest uppercase text-light flex items-center gap-6"
              >
                {get(`marquee.item${index}`)}
                <span className="text-accent">/</span>
              </span>
            ))}
          </div>
        ))}
      </div>
    </div>
  );
}

function Home({ onNavigate }: { onNavigate: (route: string) => void }) {
  return (
    <div className="home-page min-h-screen bg-deep font-body text-light overflow-x-hidden">

      <div>
        <Hero onNavigate={onNavigate} />
        <Marquee />
        <BentoGrid onNavigate={onNavigate} />
        <SwitchShowcase />
      </div>

    </div>
  );
}

export default Home;
