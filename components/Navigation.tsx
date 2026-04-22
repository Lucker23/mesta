'use client';

import { useState } from 'react';
import Link from 'next/link';
import { usePathname } from 'next/navigation';
import { locales, getLocaleName } from '@/lib/i18n';
import type { Locale } from '@/lib/i18n';

interface NavigationProps {
  locale: string;
  translations: {
    home: string;
    team: string;
    about: string;
    sponsors: string;
    contact: string;
    join: string;
  };
}

export default function Navigation({ locale, translations }: NavigationProps) {
  const [mobileOpen, setMobileOpen] = useState(false);
  const pathname = usePathname();

  const links = [
    { href: `/${locale}`, label: translations.home },
    { href: `/${locale}/team`, label: translations.team },
    { href: `/${locale}/about`, label: translations.about },
    { href: `/${locale}/sponsors`, label: translations.sponsors },
    { href: `/${locale}/contact`, label: translations.contact },
    { href: `/${locale}/join`, label: translations.join },
  ];

  const isActive = (href: string) => pathname === href;

  return (
    <nav className="fixed top-0 left-0 right-0 z-50 bg-midnight/90 backdrop-blur-md border-b border-text-dim/20">
      <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
        <div className="flex items-center justify-between h-16">
          {/* Logo */}
          <Link href={`/${locale}`} className="flex items-center gap-2">
            <span className="font-orbitron text-xl font-bold text-gold tracking-wider">
              NONEK
            </span>
            <span className="font-rajdhani text-sm text-text-muted hidden sm:block">
              GAMING
            </span>
          </Link>

          {/* Desktop nav */}
          <div className="hidden md:flex items-center gap-1">
            {links.map((link) => (
              <Link
                key={link.href}
                href={link.href}
                className={`font-rajdhani text-sm font-medium px-3 py-2 rounded transition-colors ${
                  isActive(link.href)
                    ? 'text-teal bg-teal/10'
                    : 'text-text-muted hover:text-text-primary hover:bg-white/5'
                }`}
              >
                {link.label}
              </Link>
            ))}

            {/* Locale switcher */}
            <div className="ml-4 flex items-center gap-1 border-l border-text-dim/30 pl-4">
              {locales.map((loc) => (
                <Link
                  key={loc}
                  href={pathname.replace(`/${locale}`, `/${loc}`)}
                  className={`font-rajdhani text-xs px-2 py-1 rounded transition-colors ${
                    locale === loc
                      ? 'text-teal bg-teal/10'
                      : 'text-text-dim hover:text-text-muted'
                  }`}
                  title={getLocaleName(loc as Locale)}
                >
                  {loc.toUpperCase()}
                </Link>
              ))}
            </div>
          </div>

          {/* Mobile toggle */}
          <button
            onClick={() => setMobileOpen(!mobileOpen)}
            className="md:hidden p-2 text-text-muted hover:text-text-primary"
            aria-label="Toggle menu"
          >
            <svg className="w-6 h-6" fill="none" viewBox="0 0 24 24" stroke="currentColor">
              {mobileOpen ? (
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M6 18L18 6M6 6l12 12" />
              ) : (
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M4 6h16M4 12h16M4 18h16" />
              )}
            </svg>
          </button>
        </div>
      </div>

      {/* Mobile menu */}
      {mobileOpen && (
        <div className="md:hidden bg-elevated border-b border-text-dim/20">
          <div className="px-4 py-3 space-y-1">
            {links.map((link) => (
              <Link
                key={link.href}
                href={link.href}
                onClick={() => setMobileOpen(false)}
                className={`block font-rajdhani text-sm font-medium px-3 py-2 rounded transition-colors ${
                  isActive(link.href)
                    ? 'text-teal bg-teal/10'
                    : 'text-text-muted hover:text-text-primary hover:bg-white/5'
                }`}
              >
                {link.label}
              </Link>
            ))}
            <div className="flex items-center gap-2 pt-2 mt-2 border-t border-text-dim/20">
              {locales.map((loc) => (
                <Link
                  key={loc}
                  href={pathname.replace(`/${locale}`, `/${loc}`)}
                  onClick={() => setMobileOpen(false)}
                  className={`font-rajdhani text-xs px-2 py-1 rounded transition-colors ${
                    locale === loc
                      ? 'text-teal bg-teal/10'
                      : 'text-text-dim hover:text-text-muted'
                  }`}
                >
                  {loc.toUpperCase()}
                </Link>
              ))}
            </div>
          </div>
        </div>
      )}
    </nav>
  );
}
