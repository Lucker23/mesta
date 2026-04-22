import type { Metadata } from 'next';
import { isValidLocale, getTranslations } from '@/lib/i18n';
import ScrollReveal from '@/components/ScrollReveal';
import AnimatedCounter from '@/components/AnimatedCounter';

export const metadata: Metadata = {
  title: 'About',
};

export default async function AboutPage({
  params,
}: {
  params: { locale: string };
}) {
  const locale = isValidLocale(params.locale) ? params.locale : 'en';
  const t = await getTranslations(locale);

  return (
    <div className="page-enter-active py-20 px-4">
      <div className="max-w-4xl mx-auto">
        {/* Header */}
        <ScrollReveal>
          <div className="text-center mb-16">
            <h1 className="font-orbitron text-4xl md:text-5xl font-bold text-gold tracking-wider">
              {t.about.title}
            </h1>
            <p className="font-rajdhani text-xl text-teal mt-4 tracking-wider uppercase">
              {t.about.subtitle}
            </p>
          </div>
        </ScrollReveal>

        {/* Story */}
        <ScrollReveal>
          <div className="bg-elevated rounded-lg p-8 border border-text-dim/20 mb-8">
            <p className="font-exo2 text-text-muted leading-relaxed">
              {t.about.story}
            </p>
          </div>
        </ScrollReveal>

        {/* Vision */}
        <ScrollReveal delay={100}>
          <div className="bg-elevated rounded-lg p-8 border border-text-dim/20 mb-16">
            <p className="font-exo2 text-text-muted leading-relaxed">
              {t.about.vision}
            </p>
          </div>
        </ScrollReveal>

        {/* Values */}
        <ScrollReveal>
          <div className="grid grid-cols-1 md:grid-cols-2 gap-6 mb-16">
            {t.about.values.map((value: { title: string; description: string }, i: number) => (
              <ScrollReveal key={i} delay={i * 100}>
                <div className="bg-elevated rounded-lg p-6 border border-text-dim/20 hover:border-teal/30 transition-colors">
                  <h3 className="font-orbitron text-sm font-bold text-teal tracking-wider mb-3">
                    {value.title}
                  </h3>
                  <p className="font-exo2 text-sm text-text-muted leading-relaxed">
                    {value.description}
                  </p>
                </div>
              </ScrollReveal>
            ))}
          </div>
        </ScrollReveal>

        {/* Stats */}
        <ScrollReveal>
          <h2 className="font-orbitron text-xl font-bold text-gold text-center tracking-wider mb-8">
            {t.about.statsTitle}
          </h2>
        </ScrollReveal>
        <div className="grid grid-cols-2 md:grid-cols-4 gap-8">
          <AnimatedCounter end={2024} label={t.home.stats.founded} />
          <AnimatedCounter end="Prague" label={t.home.stats.region} />
          <AnimatedCounter end="Dota 2" label={t.home.stats.game} />
          <AnimatedCounter end={10} label={t.home.stats.agents} />
        </div>
      </div>
    </div>
  );
}
