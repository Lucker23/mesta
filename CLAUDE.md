# NONEK GAMING

Project: NONEK GAMING — Competitive Dota 2 esports team
Domain: nonek.online (GoDaddy, purchased)
Stack: Next.js 14+ | React | TypeScript | Tailwind CSS | Vercel
Brand: Dark only (#060610 bg), Teal #00e5ff, Gold #e8a020
Fonts: Orbitron (display), Rajdhani (UI), Exo 2 (body)
Languages: EN (default), CS, UK
Team: LUCKER23 (Manager), NONEK (MVP/Carry), CHILLZER (Coach)

## Build & Dev

```bash
npm run dev      # Start dev server (localhost:3000)
npm run build    # Production build
npm run start    # Start production server
npm run lint     # Run ESLint
```

## Project Structure

```
app/
  layout.tsx              # Root layout (fonts, metadata)
  globals.css             # Global styles + brand CSS variables
  [locale]/
    layout.tsx            # Locale layout (navigation, footer)
    page.tsx              # Home
    team/page.tsx          # Team roster
    about/page.tsx         # About us
    sponsors/page.tsx      # Sponsors
    contact/page.tsx       # Contact
    join/page.tsx          # Join/recruit
components/               # Shared React components
lib/
  roster.ts               # Team roster data (10 agents)
  i18n.ts                 # i18n utilities
  translations/           # EN, CS, UK translation files
middleware.ts             # Locale detection + redirect
public/                   # Static assets
```

## Brand Colors (CSS Variables)

- `--bg-primary`: #060610 (midnight)
- `--bg-elevated`: #0d1117 (cards)
- `--accent-teal`: #00e5ff (links, interactive)
- `--accent-gold`: #e8a020 (headlines, CTAs)
- `--text-primary`: #e0e6ed
- `--text-muted`: #6a7a8a
- `--text-dim`: #3a4a5a

## Conventions

- Dark theme only. No light mode.
- Brand colors are law — no deviations.
- Fonts: Orbitron for headings, Rajdhani for UI/nav, Exo 2 for body text.
- Mobile-first responsive: 480px, 768px, 1024px+.
- All credentials in Bitwarden. NEVER in code or chat.
- i18n: EN is default locale. CS and UK are supported.
- Deploy: push to main → Vercel auto-deploy → nonek.online
