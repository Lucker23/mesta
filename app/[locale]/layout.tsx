import { locales, isValidLocale, getTranslations } from '@/lib/i18n';
import Navigation from '@/components/Navigation';
import Footer from '@/components/Footer';
import ScrollProgressBar from '@/components/ScrollProgressBar';
import ParticleBackground from '@/components/ParticleBackground';

export function generateStaticParams() {
  return locales.map((locale) => ({ locale }));
}

export default async function LocaleLayout({
  children,
  params,
}: {
  children: React.ReactNode;
  params: { locale: string };
}) {
  const locale = isValidLocale(params.locale) ? params.locale : 'en';
  const t = await getTranslations(locale);

  return (
    <>
      <ScrollProgressBar />
      <ParticleBackground />
      <Navigation locale={locale} translations={t.nav} />
      <main className="relative z-10 min-h-screen pt-16">
        {children}
      </main>
      <Footer translations={t.footer} />
    </>
  );
}
