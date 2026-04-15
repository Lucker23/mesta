import type { Metadata } from 'next';
import { isValidLocale, getTranslations } from '@/lib/i18n';
import { activeAgents, recruitingAgents } from '@/lib/roster';
import AgentCard from '@/components/AgentCard';
import ScrollReveal from '@/components/ScrollReveal';

export const metadata: Metadata = {
  title: 'Team',
};

export default async function TeamPage({
  params,
}: {
  params: { locale: string };
}) {
  const locale = isValidLocale(params.locale) ? params.locale : 'en';
  const t = await getTranslations(locale);

  return (
    <div className="page-enter-active py-20 px-4">
      <div className="max-w-6xl mx-auto">
        {/* Header */}
        <ScrollReveal>
          <div className="text-center mb-16">
            <h1 className="font-orbitron text-4xl md:text-5xl font-bold text-gold tracking-wider">
              {t.team.title}
            </h1>
            <p className="font-exo2 text-text-muted mt-4 max-w-2xl mx-auto">
              {t.team.subtitle}
            </p>
          </div>
        </ScrollReveal>

        {/* Active Agents */}
        <ScrollReveal>
          <h2 className="font-orbitron text-xl font-bold text-teal tracking-wider mb-8 flex items-center gap-3">
            <span className="w-2 h-2 rounded-full bg-emerald-500 status-dot" />
            {t.team.activeTitle}
          </h2>
        </ScrollReveal>
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6 mb-16">
          {activeAgents.map((agent, i) => (
            <ScrollReveal key={agent.id} delay={i * 100}>
              <AgentCard agent={agent} locale={locale} />
            </ScrollReveal>
          ))}
        </div>

        {/* Recruiting */}
        <ScrollReveal>
          <h2 className="font-orbitron text-xl font-bold text-gold tracking-wider mb-8 flex items-center gap-3">
            <span className="w-2 h-2 rounded-full bg-amber-500 amber-pulse" />
            {t.team.recruitingTitle}
          </h2>
        </ScrollReveal>
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 gap-6">
          {recruitingAgents.map((agent, i) => (
            <ScrollReveal key={agent.id} delay={i * 80}>
              <AgentCard agent={agent} locale={locale} applyLabel={t.team.applyNow} />
            </ScrollReveal>
          ))}
        </div>
      </div>
    </div>
  );
}
