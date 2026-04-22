import Link from 'next/link';

interface FooterProps {
  translations: {
    slogan: string;
    rights: string;
    discord: string;
    twitter: string;
    twitch: string;
  };
}

export default function Footer({ translations }: FooterProps) {
  return (
    <footer className="relative z-10 border-t border-text-dim/20 bg-midnight">
      <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-12">
        <div className="grid grid-cols-1 md:grid-cols-3 gap-8">
          {/* Brand */}
          <div>
            <h3 className="font-orbitron text-lg font-bold text-gold tracking-wider">
              NONEK GAMING
            </h3>
            <p className="font-rajdhani text-text-muted mt-2">
              {translations.slogan}
            </p>
          </div>

          {/* Links */}
          <div>
            <h4 className="font-rajdhani text-sm font-semibold text-text-primary uppercase tracking-wider mb-3">
              Links
            </h4>
            <div className="space-y-2">
              <Link href="#" className="block font-exo2 text-sm text-text-muted hover:text-teal transition-colors">
                {translations.discord}
              </Link>
              <Link href="#" className="block font-exo2 text-sm text-text-muted hover:text-teal transition-colors">
                {translations.twitter}
              </Link>
              <Link href="#" className="block font-exo2 text-sm text-text-muted hover:text-teal transition-colors">
                {translations.twitch}
              </Link>
            </div>
          </div>

          {/* Contact */}
          <div>
            <h4 className="font-rajdhani text-sm font-semibold text-text-primary uppercase tracking-wider mb-3">
              Contact
            </h4>
            <p className="font-exo2 text-sm text-text-muted">
              team@nonek.online
            </p>
            <p className="font-exo2 text-sm text-text-muted mt-1">
              Prague, Europe
            </p>
          </div>
        </div>

        <div className="mt-8 pt-8 border-t border-text-dim/20 text-center">
          <p className="font-exo2 text-xs text-text-dim">
            &copy; {new Date().getFullYear()} NONEK GAMING. {translations.rights}
          </p>
        </div>
      </div>
    </footer>
  );
}
