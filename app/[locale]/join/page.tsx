import type { Metadata } from 'next';
import { isValidLocale, getTranslations } from '@/lib/i18n';
import { recruitingAgents } from '@/lib/roster';
import ScrollReveal from '@/components/ScrollReveal';

export const metadata: Metadata = {
  title: 'Join Us',
};

export default async function JoinPage({
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
              {t.join.title}
            </h1>
            <p className="font-exo2 text-text-muted mt-4 max-w-2xl mx-auto">
              {t.join.subtitle}
            </p>
          </div>
        </ScrollReveal>

        <div className="grid grid-cols-1 lg:grid-cols-2 gap-12 mb-16">
          {/* Requirements */}
          <ScrollReveal>
            <div className="bg-elevated rounded-lg p-8 border border-text-dim/20">
              <h2 className="font-orbitron text-lg font-bold text-teal tracking-wider mb-6">
                {t.join.requirements}
              </h2>
              <ul className="space-y-3">
                {t.join.requirementsList.map((req: string, i: number) => (
                  <li key={i} className="flex items-start gap-3">
                    <span className="text-teal mt-0.5 flex-shrink-0">
                      <svg className="w-4 h-4" fill="currentColor" viewBox="0 0 20 20">
                        <path fillRule="evenodd" d="M16.707 5.293a1 1 0 010 1.414l-8 8a1 1 0 01-1.414 0l-4-4a1 1 0 011.414-1.414L8 12.586l7.293-7.293a1 1 0 011.414 0z" clipRule="evenodd" />
                      </svg>
                    </span>
                    <span className="font-exo2 text-sm text-text-muted">{req}</span>
                  </li>
                ))}
              </ul>
            </div>
          </ScrollReveal>

          {/* Process */}
          <ScrollReveal delay={100}>
            <div className="bg-elevated rounded-lg p-8 border border-text-dim/20">
              <h2 className="font-orbitron text-lg font-bold text-gold tracking-wider mb-6">
                {t.join.process}
              </h2>
              <ol className="space-y-4">
                {t.join.processSteps.map((step: string, i: number) => (
                  <li key={i} className="flex items-start gap-4">
                    <span className="font-orbitron text-xs font-bold text-gold bg-gold/10 rounded-full w-6 h-6 flex items-center justify-center flex-shrink-0 mt-0.5">
                      {i + 1}
                    </span>
                    <span className="font-exo2 text-sm text-text-muted">{step}</span>
                  </li>
                ))}
              </ol>
            </div>
          </ScrollReveal>
        </div>

        {/* Coach Quote */}
        <ScrollReveal>
          <div className="bg-elevated/50 rounded-lg p-8 border-l-4 border-teal mb-16 text-center">
            <p className="font-exo2 text-text-primary italic leading-relaxed">
              {t.join.coachQuote}
            </p>
            <p className="font-rajdhani text-sm text-teal mt-4 font-semibold">
              {t.join.coachName}
            </p>
          </div>
        </ScrollReveal>

        {/* Open Positions */}
        <ScrollReveal>
          <h2 className="font-orbitron text-xl font-bold text-gold tracking-wider mb-6 text-center">
            {t.join.positions}
          </h2>
        </ScrollReveal>
        <div className="grid grid-cols-2 md:grid-cols-4 gap-4 mb-16">
          {recruitingAgents.map((agent, i) => (
            <ScrollReveal key={agent.id} delay={i * 60}>
              <div
                className="bg-elevated/50 rounded-lg p-4 border border-dashed text-center hover:bg-elevated transition-colors"
                style={{ borderColor: `${agent.color}40` }}
              >
                <span
                  className="font-orbitron text-xs font-bold tracking-wider"
                  style={{ color: agent.color }}
                >
                  {agent.tag}
                </span>
                <p className="font-rajdhani text-sm text-text-muted mt-1">
                  {agent.role}
                </p>
                {agent.position && (
                  <p className="font-exo2 text-xs text-text-dim mt-0.5">
                    {agent.position}
                  </p>
                )}
              </div>
            </ScrollReveal>
          ))}
        </div>

        {/* Application Form */}
        <ScrollReveal>
          <div className="bg-elevated rounded-lg p-8 border border-text-dim/20 max-w-2xl mx-auto">
            <h2 className="font-orbitron text-lg font-bold text-teal tracking-wider mb-6 text-center">
              {t.join.form.submit}
            </h2>
            <form className="space-y-4">
              <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
                <div>
                  <label className="block font-rajdhani text-sm text-text-muted mb-1">
                    {t.join.form.ign}
                  </label>
                  <input
                    type="text"
                    className="w-full bg-midnight border border-text-dim/30 rounded px-4 py-2.5 font-exo2 text-sm text-text-primary placeholder-text-dim focus:outline-none focus:border-teal/50 transition-colors"
                    placeholder={t.join.form.ign}
                  />
                </div>
                <div>
                  <label className="block font-rajdhani text-sm text-text-muted mb-1">
                    {t.join.form.discord}
                  </label>
                  <input
                    type="text"
                    className="w-full bg-midnight border border-text-dim/30 rounded px-4 py-2.5 font-exo2 text-sm text-text-primary placeholder-text-dim focus:outline-none focus:border-teal/50 transition-colors"
                    placeholder={t.join.form.discord}
                  />
                </div>
              </div>
              <div>
                <label className="block font-rajdhani text-sm text-text-muted mb-1">
                  {t.join.form.dotabuff}
                </label>
                <input
                  type="url"
                  className="w-full bg-midnight border border-text-dim/30 rounded px-4 py-2.5 font-exo2 text-sm text-text-primary placeholder-text-dim focus:outline-none focus:border-teal/50 transition-colors"
                  placeholder="https://www.dotabuff.com/players/..."
                />
              </div>
              <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
                <div>
                  <label className="block font-rajdhani text-sm text-text-muted mb-1">
                    {t.join.form.position}
                  </label>
                  <select className="w-full bg-midnight border border-text-dim/30 rounded px-4 py-2.5 font-exo2 text-sm text-text-muted focus:outline-none focus:border-teal/50 transition-colors">
                    <option value="">--</option>
                    <option value="mid">Mid (Pos 2)</option>
                    <option value="offlane">Offlane (Pos 3)</option>
                    <option value="pos4">Soft Support (Pos 4)</option>
                    <option value="pos5">Hard Support (Pos 5)</option>
                    <option value="analyst">Data Analyst</option>
                    <option value="content">Content Creator</option>
                    <option value="sub">Substitute</option>
                  </select>
                </div>
                <div>
                  <label className="block font-rajdhani text-sm text-text-muted mb-1">
                    {t.join.form.rank}
                  </label>
                  <input
                    type="text"
                    className="w-full bg-midnight border border-text-dim/30 rounded px-4 py-2.5 font-exo2 text-sm text-text-primary placeholder-text-dim focus:outline-none focus:border-teal/50 transition-colors"
                    placeholder={t.join.form.rank}
                  />
                </div>
              </div>
              <div>
                <label className="block font-rajdhani text-sm text-text-muted mb-1">
                  {t.join.form.email}
                </label>
                <input
                  type="email"
                  className="w-full bg-midnight border border-text-dim/30 rounded px-4 py-2.5 font-exo2 text-sm text-text-primary placeholder-text-dim focus:outline-none focus:border-teal/50 transition-colors"
                  placeholder={t.join.form.email}
                />
              </div>
              <div>
                <label className="block font-rajdhani text-sm text-text-muted mb-1">
                  {t.join.form.about}
                </label>
                <textarea
                  rows={4}
                  className="w-full bg-midnight border border-text-dim/30 rounded px-4 py-2.5 font-exo2 text-sm text-text-primary placeholder-text-dim focus:outline-none focus:border-teal/50 transition-colors resize-none"
                  placeholder={t.join.form.about}
                />
              </div>
              <button
                type="submit"
                className="w-full font-rajdhani font-bold text-sm uppercase tracking-wider px-8 py-3 bg-gold text-midnight rounded hover:bg-gold/90 transition-colors"
              >
                {t.join.form.submit}
              </button>
            </form>
          </div>
        </ScrollReveal>
      </div>
    </div>
  );
}
