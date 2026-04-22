import Link from 'next/link';
import { isValidLocale, getTranslations } from '@/lib/i18n';
import { activeAgents } from '@/lib/roster';
import AgentCard from '@/components/AgentCard';
import ScrollReveal from '@/components/ScrollReveal';
import AnimatedCounter from '@/components/AnimatedCounter';

export default async function HomePage({
  params,
}: {
  params: { locale: string };
}) {
  const locale = isValidLocale(params.locale) ? params.locale : 'en';
  const t = await getTranslations(locale);

  return (
    <div className="page-enter-active">
      {/* Hero Section */}
      <section className="relative min-h-[90vh] flex items-center justify-center px-4">
        <div className="text-center max-w-4xl mx-auto">
          <ScrollReveal>
            <h1 className="font-orbitron text-5xl md:text-7xl lg:text-8xl font-black text-gold tracking-widest">
              {t.hero.title}
            </h1>
          </ScrollReveal>
          <ScrollReveal delay={200}>
            <p className="font-rajdhani text-xl md:text-2xl text-teal mt-4 tracking-wider uppercase">
              {t.hero.subtitle}
            </p>
          </ScrollReveal>
          <ScrollReveal delay={400}>
            <p className="font-exo2 text-base md:text-lg text-text-muted mt-6 max-w-2xl mx-auto leading-relaxed">
              {t.hero.description}
            </p>
          </ScrollReveal>
          <ScrollReveal delay={600}>
            <div className="flex flex-col sm:flex-row gap-4 justify-center mt-10">
              <Link
                href={`/${locale}/team`}
                className="font-rajdhani font-bold text-sm uppercase tracking-wider px-8 py-3 bg-teal text-midnight rounded hover:bg-teal/90 transition-colors"
              >
                {t.hero.cta}
              </Link>
              <Link
                href={`/${locale}/join`}
                className="font-rajdhani font-bold text-sm uppercase tracking-wider px-8 py-3 border border-gold text-gold rounded hover:bg-gold/10 transition-colors"
              >
                {t.hero.ctaSecondary}
              </Link>
            </div>
          </ScrollReveal>
        </div>

        {/* Scroll indicator */}
        <div className="absolute bottom-8 left-1/2 -translate-x-1/2 animate-bounce">
          <svg className="w-6 h-6 text-text-dim" fill="none" viewBox="0 0 24 24" stroke="currentColor">
            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M19 14l-7 7m0 0l-7-7m7 7V3" />
          </svg>
        </div>
      </section>

      {/* Top 3 Agents */}
      <section className="py-20 px-4">
        <div className="max-w-6xl mx-auto">
          <ScrollReveal>
            <h2 className="font-orbitron text-2xl md:text-3xl font-bold text-gold text-center tracking-wider mb-12">
              {t.team.activeTitle}
            </h2>
          </ScrollReveal>
          <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
            {activeAgents.map((agent, i) => (
              <ScrollReveal key={agent.id} delay={i * 150}>
                <AgentCard agent={agent} locale={locale} />
              </ScrollReveal>
            ))}
          </div>
        </div>
      </section>

      {/* Stats */}
      <section className="py-20 px-4 bg-elevated/30">
        <div className="max-w-4xl mx-auto">
          <ScrollReveal>
            <h2 className="font-orbitron text-2xl md:text-3xl font-bold text-gold text-center tracking-wider mb-12">
              {t.home.statsTitle}
            </h2>
          </ScrollReveal>
          <div className="grid grid-cols-2 md:grid-cols-4 gap-8">
            <AnimatedCounter end={2024} label={t.home.stats.founded} />
            <AnimatedCounter end="Europe" label={t.home.stats.region} />
            <AnimatedCounter end="Dota 2" label={t.home.stats.game} />
            <AnimatedCounter end={10} label={t.home.stats.agents} />
          </div>
        </div>
      </section>

      {/* News / Timeline */}
      <section className="py-20 px-4">
        <div className="max-w-3xl mx-auto">
          <ScrollReveal>
            <h2 className="font-orbitron text-2xl md:text-3xl font-bold text-gold text-center tracking-wider mb-12">
              {t.home.newsTitle}
            </h2>
          </ScrollReveal>
          <div className="space-y-6">
            {t.home.news.map((item: { date: string; title: string; description: string }, i: number) => (
              <ScrollReveal key={i} delay={i * 100}>
                <div className="flex gap-6 items-start">
                  <div className="flex-shrink-0 w-20">
                    <span className="font-orbitron text-xs text-teal">{item.date}</span>
                  </div>
                  <div className="border-l-2 border-text-dim/30 pl-6 pb-6">
                    <h3 className="font-rajdhani text-lg font-semibold text-text-primary">
                      {item.title}
                    </h3>
                    <p className="font-exo2 text-sm text-text-muted mt-1">
                      {item.description}
                    </p>
                  </div>
                </div>
              </ScrollReveal>
            ))}
          </div>
        </div>
      </section>

      {/* CTA */}
      <section className="py-20 px-4 bg-elevated/30">
        <div className="max-w-2xl mx-auto text-center">
          <ScrollReveal>
            <h2 className="font-orbitron text-2xl md:text-3xl font-bold text-gold tracking-wider">
              {t.hero.ctaSecondary}
            </h2>
            <p className="font-exo2 text-text-muted mt-4 mb-8">
              {t.team.subtitle}
            </p>
            <Link
              href={`/${locale}/join`}
              className="inline-block font-rajdhani font-bold text-sm uppercase tracking-wider px-8 py-3 bg-gold text-midnight rounded hover:bg-gold/90 transition-colors"
            >
              {t.team.applyNow}
            </Link>
          </ScrollReveal>
        </div>
      </section>
    </div>
  );
}
