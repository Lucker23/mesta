import type { Metadata } from 'next';
import { isValidLocale, getTranslations } from '@/lib/i18n';
import ScrollReveal from '@/components/ScrollReveal';

export const metadata: Metadata = {
  title: 'Contact',
};

export default async function ContactPage({
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
              {t.contact.title}
            </h1>
            <p className="font-exo2 text-text-muted mt-4 max-w-2xl mx-auto">
              {t.contact.subtitle}
            </p>
          </div>
        </ScrollReveal>

        <div className="grid grid-cols-1 md:grid-cols-2 gap-12">
          {/* Contact channels */}
          <div className="space-y-6">
            <ScrollReveal>
              <div className="bg-elevated rounded-lg p-6 border border-text-dim/20 hover:border-teal/30 transition-colors">
                <h3 className="font-orbitron text-sm font-bold text-teal tracking-wider mb-2">
                  {t.contact.discord}
                </h3>
                <p className="font-exo2 text-text-muted text-sm">
                  NONEK GAMING Discord Server
                </p>
              </div>
            </ScrollReveal>

            <ScrollReveal delay={100}>
              <div className="bg-elevated rounded-lg p-6 border border-text-dim/20 hover:border-teal/30 transition-colors">
                <h3 className="font-orbitron text-sm font-bold text-teal tracking-wider mb-2">
                  {t.contact.steam}
                </h3>
                <p className="font-exo2 text-text-muted text-sm">
                  Steam Community Group
                </p>
              </div>
            </ScrollReveal>

            <ScrollReveal delay={200}>
              <div className="bg-elevated rounded-lg p-6 border border-text-dim/20 hover:border-teal/30 transition-colors">
                <h3 className="font-orbitron text-sm font-bold text-teal tracking-wider mb-2">
                  {t.contact.email}
                </h3>
                <p className="font-exo2 text-text-muted text-sm">
                  team@nonek.online
                </p>
              </div>
            </ScrollReveal>
          </div>

          {/* Contact form */}
          <ScrollReveal delay={150}>
            <form className="space-y-4">
              <div>
                <label className="block font-rajdhani text-sm text-text-muted mb-1">
                  {t.contact.form.name}
                </label>
                <input
                  type="text"
                  className="w-full bg-elevated border border-text-dim/30 rounded px-4 py-2.5 font-exo2 text-sm text-text-primary placeholder-text-dim focus:outline-none focus:border-teal/50 transition-colors"
                  placeholder={t.contact.form.name}
                />
              </div>
              <div>
                <label className="block font-rajdhani text-sm text-text-muted mb-1">
                  {t.contact.form.email}
                </label>
                <input
                  type="email"
                  className="w-full bg-elevated border border-text-dim/30 rounded px-4 py-2.5 font-exo2 text-sm text-text-primary placeholder-text-dim focus:outline-none focus:border-teal/50 transition-colors"
                  placeholder={t.contact.form.email}
                />
              </div>
              <div>
                <label className="block font-rajdhani text-sm text-text-muted mb-1">
                  {t.contact.form.subject}
                </label>
                <input
                  type="text"
                  className="w-full bg-elevated border border-text-dim/30 rounded px-4 py-2.5 font-exo2 text-sm text-text-primary placeholder-text-dim focus:outline-none focus:border-teal/50 transition-colors"
                  placeholder={t.contact.form.subject}
                />
              </div>
              <div>
                <label className="block font-rajdhani text-sm text-text-muted mb-1">
                  {t.contact.form.message}
                </label>
                <textarea
                  rows={5}
                  className="w-full bg-elevated border border-text-dim/30 rounded px-4 py-2.5 font-exo2 text-sm text-text-primary placeholder-text-dim focus:outline-none focus:border-teal/50 transition-colors resize-none"
                  placeholder={t.contact.form.message}
                />
              </div>
              <button
                type="submit"
                className="w-full font-rajdhani font-bold text-sm uppercase tracking-wider px-8 py-3 bg-teal text-midnight rounded hover:bg-teal/90 transition-colors"
              >
                {t.contact.form.send}
              </button>
            </form>
          </ScrollReveal>
        </div>
      </div>
    </div>
  );
}
