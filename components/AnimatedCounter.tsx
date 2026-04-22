'use client';

import { useEffect, useRef, useState } from 'react';

interface AnimatedCounterProps {
  end: number | string;
  label: string;
  duration?: number;
}

export default function AnimatedCounter({ end, label, duration = 2000 }: AnimatedCounterProps) {
  const [display, setDisplay] = useState<string>(typeof end === 'number' ? '0' : '');
  const ref = useRef<HTMLDivElement>(null);
  const hasAnimated = useRef(false);

  useEffect(() => {
    const el = ref.current;
    if (!el) return;

    const observer = new IntersectionObserver(
      ([entry]) => {
        if (entry.isIntersecting && !hasAnimated.current) {
          hasAnimated.current = true;

          if (typeof end === 'number') {
            const startTime = performance.now();
            const tick = (now: number) => {
              const elapsed = now - startTime;
              const progress = Math.min(elapsed / duration, 1);
              const eased = 1 - Math.pow(1 - progress, 3);
              setDisplay(Math.round(eased * (end as number)).toString());
              if (progress < 1) requestAnimationFrame(tick);
            };
            requestAnimationFrame(tick);
          } else {
            setDisplay(end as string);
          }

          observer.unobserve(el);
        }
      },
      { threshold: 0.3 }
    );

    observer.observe(el);
    return () => observer.disconnect();
  }, [end, duration]);

  return (
    <div ref={ref} className="text-center">
      <div className="font-orbitron text-3xl md:text-4xl font-bold text-teal">
        {display}
      </div>
      <div className="font-rajdhani text-sm text-text-muted mt-2 uppercase tracking-wider">
        {label}
      </div>
    </div>
  );
}
