import { Outlet } from 'react-router-dom';
import BottomNav from '../components/BottomNav';
import OfflineBanner from '../components/OfflineBanner';

export default function AppShell() {
  return (
    <div className="flex min-h-dvh flex-col bg-graphite-950">
      <OfflineBanner />
      <header className="border-b border-graphite-800 bg-graphite-900/80 px-4 py-3 backdrop-blur">
        <div className="mx-auto flex w-full max-w-3xl items-center justify-between">
          <div className="flex items-center gap-2">
            <span
              className="inline-block h-2.5 w-2.5 rounded-full bg-bronze-500"
              aria-hidden="true"
            />
            <h1 className="text-lg font-bold tracking-tight text-graphite-50">خيوط</h1>
            <span className="text-xs text-graphite-400">سوق الأقمشة والنسيج</span>
          </div>
        </div>
      </header>
      <main className="mx-auto w-full max-w-3xl flex-1 px-4 pb-24 pt-4">
        <Outlet />
      </main>
      <BottomNav />
    </div>
  );
}
