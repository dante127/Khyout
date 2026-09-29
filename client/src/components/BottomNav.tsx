import type { ComponentType } from 'react';
import { NavLink } from 'react-router-dom';

function HomeIcon({ className }: { className?: string }) {
  return (
    <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round" className={className} aria-hidden="true">
      <path d="M4 10.5 12 4l8 6.5" />
      <path d="M6 9.5V20h12V9.5" />
    </svg>
  );
}

function CatalogIcon({ className }: { className?: string }) {
  return (
    <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round" className={className} aria-hidden="true">
      <rect x="4" y="4" width="7" height="7" rx="1.5" />
      <rect x="13" y="4" width="7" height="7" rx="1.5" />
      <rect x="4" y="13" width="7" height="7" rx="1.5" />
      <rect x="13" y="13" width="7" height="7" rx="1.5" />
    </svg>
  );
}

function RfqsIcon({ className }: { className?: string }) {
  return (
    <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round" className={className} aria-hidden="true">
      <path d="M7 4h7l4 4v12H7z" />
      <path d="M14 4v4h4" />
      <path d="M9.5 12h5" />
      <path d="M9.5 15.5h5" />
    </svg>
  );
}

function SamplesIcon({ className }: { className?: string }) {
  return (
    <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round" className={className} aria-hidden="true">
      <path d="M5 8h14v10a2 2 0 0 1-2 2H7a2 2 0 0 1-2-2z" />
      <path d="M7 8V6a2 2 0 0 1 2-2h6a2 2 0 0 1 2 2v2" />
    </svg>
  );
}

function ProfileIcon({ className }: { className?: string }) {
  return (
    <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round" className={className} aria-hidden="true">
      <circle cx="12" cy="8.5" r="3.5" />
      <path d="M5 20c1.5-3.5 4-5 7-5s5.5 1.5 7 5" />
    </svg>
  );
}

interface NavItem {
  to: string;
  label: string;
  icon: ComponentType<{ className?: string }>;
}

const items: NavItem[] = [
  { to: '/', label: 'الرئيسية', icon: HomeIcon },
  { to: '/catalog', label: 'الكتالوج', icon: CatalogIcon },
  { to: '/rfqs', label: 'الطلبات', icon: RfqsIcon },
  { to: '/samples', label: 'العينات', icon: SamplesIcon },
  { to: '/profile', label: 'حسابي', icon: ProfileIcon },
];

export default function BottomNav() {
  return (
    <nav
      className="fixed inset-x-0 bottom-0 z-10 border-t border-graphite-800 bg-graphite-900/95 backdrop-blur"
      aria-label="التنقل الرئيسي"
    >
      <div className="mx-auto flex w-full max-w-3xl items-stretch justify-between px-2">
        {items.map((item) => {
          const Icon = item.icon;
          return (
            <NavLink
              key={item.to}
              to={item.to}
              end={item.to === '/'}
              className={({ isActive }) =>
                `flex flex-1 flex-col items-center gap-1 py-2.5 text-[11px] transition-colors ${
                  isActive ? 'text-bronze-400' : 'text-graphite-400 hover:text-graphite-200'
                }`
              }
            >
              <Icon className="h-6 w-6" />
              <span>{item.label}</span>
            </NavLink>
          );
        })}
      </div>
    </nav>
  );
}
