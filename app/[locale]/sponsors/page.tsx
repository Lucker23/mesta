import type { Metadata } from 'next';
import Link from 'next/link';
import { isValidLocale, getTranslations } from '@/lib/i18n';
import ScrollReveal from '@/components/ScrollReveal';

export const metadata: Metadata = {
  title: 'Sponsors',
};

export default async function SponsorsPage({
  params,
}: {
  params: { locale: string };
}) {
  const locale = isValidLocale(params.locale) ? params.locale : 'en';
  const t = await getTranslations(locale);

  const placeholders = Array.from({ length: 6 }, (_, i) => i + 1);

  return (
    <div className="page-enter-active py-20 px-4">
      <div className="max-w-6xl mx-auto">
        {/* Header */}
        <ScrollReveal>
          <div className="text-center mb-16">
            <h1 className="font-orbitron text-4xl md:text-5xl font-bold text-gold tracking-wider">
              {t.sponsors.title}
            </h1>
            <p className="font-exo2 text-text-muted mt-4 max-w-2xl mx-auto">
              {t.sponsors.subtitle}
            </p>
          </div>
        </ScrollReveal>

        {/* Sponsor placeholders */}
        <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-6 mb-20">
          {placeholders.map((i) => (
            <ScrollReveal key={i} delay={i * 80}>
              <div className="bg-elevated/50 rounded-lg border-2 border-dashed border-text-dim/30 p-12 flex items-center justify-center hover:border-teal/30 transition-colors group">
                <div className="text-center">
                  <div className="w-16 h-16 mx-auto rounded-full border-2 border-dashed border-text-dim/30 flex items-center justify-center mb-4 group-hover:border-teal/40 transition-colors">
                    <span className="font-orbitron text-2xl text-text-dim group-hover:text-text-muted transition-colors">
                      ?
                    </span>
                  </div>
                  <p className="font-rajdhani text-sm text-text-dim group-hover:text-text-muted transition-colors">
                    {t.sponsors.placeholder}
                  </p>
                </div>
              </div>
            </ScrollReveal>
          ))}
        </div>

        {/* Become a Partner CTA */}
        <ScrollReveal>
          <div className="bg-elevated rounded-lg p-8 md:p-12 border border-gold/20 text-center max-w-2xl mx-auto">
            <h2 className="font-orbitron text-2xl font-bold text-gold tracking-wider mb-4">
              {t.sponsors.becomeSponsor}
            </h2>
            <p className="font-exo2 text-text-muted mb-8 leading-relaxed">
              {t.sponsors.becomeDescription}
            </p>
            <Link
              href={`/${locale}/contact`}
              className="inline-block font-rajdhani font-bold text-sm uppercase tracking-wider px-8 py-3 bg-gold text-midnight rounded hover:bg-gold/90 transition-colors"
            >
              {t.sponsors.contactUs}
            </Link>
          </div>
        </ScrollReveal>
      </div>
    </div>
  );
}
