import type { Metadata } from 'next';
import { Orbitron, Rajdhani, Exo_2 } from 'next/font/google';
import './globals.css';

const orbitron = Orbitron({
  subsets: ['latin'],
  variable: '--font-orbitron',
  display: 'swap',
});

const rajdhani = Rajdhani({
  subsets: ['latin'],
  weight: ['300', '400', '500', '600', '700'],
  variable: '--font-rajdhani',
  display: 'swap',
});

const exo2 = Exo_2({
  subsets: ['latin', 'cyrillic'],
  variable: '--font-exo2',
  display: 'swap',
});

export const metadata: Metadata = {
  title: {
    default: 'NONEK GAMING — Pure Focus Gaming',
    template: '%s | NONEK GAMING',
  },
  description: 'Competitive Dota 2 esports team based in Prague, Europe. Built on discipline, strategy, and relentless improvement.',
  metadataBase: new URL('https://nonek.online'),
  openGraph: {
    title: 'NONEK GAMING — Pure Focus Gaming',
    description: 'Competitive Dota 2 esports team based in Prague, Europe.',
    url: 'https://nonek.online',
    siteName: 'NONEK GAMING',
    locale: 'en_US',
    type: 'website',
  },
  twitter: {
    card: 'summary_large_image',
    title: 'NONEK GAMING — Pure Focus Gaming',
    description: 'Competitive Dota 2 esports team based in Prague, Europe.',
  },
  robots: {
    index: true,
    follow: true,
  },
};

export default function RootLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  return (
    <html lang="en" className={`${orbitron.variable} ${rajdhani.variable} ${exo2.variable}`}>
      <body className="font-exo2 antialiased">
        {children}
      </body>
    </html>
  );
}
