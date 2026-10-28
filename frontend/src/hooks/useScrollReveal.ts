import { useEffect, useRef } from 'react';

export function useScrollReveal<T extends HTMLElement = HTMLDivElement>() {
  const ref = useRef<T>(null);

  useEffect(() => {
    const el = ref.current;
    if (!el) return;

    const children = Array.from(el.querySelectorAll<HTMLElement>('.reveal'));
    const motion = window.matchMedia('(prefers-reduced-motion: reduce)');
    if (motion.matches || !('IntersectionObserver' in window)) return;

    children.forEach((child) => {
      const siblings = Array.from(child.parentElement?.children || []).filter(node => node.matches('.reveal'));
      child.style.setProperty('--reveal-delay', `${Math.min(siblings.indexOf(child), 3) * 90}ms`);
      child.classList.add('reveal-pending');
    });

    const observer = new IntersectionObserver(
      (entries) => {
        entries.forEach((entry) => {
          if (entry.isIntersecting) {
            entry.target.classList.add('is-visible');
            observer.unobserve(entry.target);
          }
        });
      },
      { threshold: 0.08, rootMargin: '0px 0px -24px 0px' }
    );

    children.forEach((child) => observer.observe(child));

    const showAll = () => {
      if (!motion.matches) return;
      children.forEach(child => child.classList.add('is-visible'));
      observer.disconnect();
    };
    const showFocused = (event: FocusEvent) => {
      const card = (event.target as HTMLElement).closest('.reveal');
      if (card) {
        card.classList.add('is-visible');
        observer.unobserve(card);
      }
    };
    motion.addEventListener('change', showAll);
    el.addEventListener('focusin', showFocused);
    return () => {
      observer.disconnect();
      motion.removeEventListener('change', showAll);
      el.removeEventListener('focusin', showFocused);
      children.forEach(child => {
        child.classList.remove('reveal-pending');
        child.style.removeProperty('--reveal-delay');
      });
    };
  }, []);

  return ref;
}
