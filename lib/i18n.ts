export const locales = ['en', 'cs', 'uk'] as const;
export type Locale = (typeof locales)[number];
export const defaultLocale: Locale = 'en';

export function isValidLocale(locale: string): locale is Locale {
  return locales.includes(locale as Locale);
}

export async function getTranslations(locale: Locale) {
  const translations = await import(`./translations/${locale}`);
  return translations.default;
}

export function getLocaleName(locale: Locale): string {
  const names: Record<Locale, string> = {
    en: 'English',
    cs: 'Cestina',
    uk: 'Українська',
  };
  return names[locale];
}
