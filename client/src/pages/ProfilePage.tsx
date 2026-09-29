import { useAuth } from '../auth/AuthContext';

const items = [
  { label: 'ربط تيليجرام', hint: 'اشترك في إشعارات الطلبات وعروض الأسعار' },
  { label: 'بيانات الشركة', hint: 'الاسم، المدينة، نوع النشاط، حالة التوثيق' },
];

export default function ProfilePage() {
  const { logout } = useAuth();

  return (
    <section className="space-y-4">
      <h2 className="text-xl font-bold text-graphite-50">حسابي</h2>

      <ul className="divide-y divide-graphite-800 overflow-hidden rounded-2xl border border-graphite-800 bg-graphite-900">
        {items.map((item) => (
          <li key={item.label} className="flex items-center justify-between px-4 py-3.5">
            <div>
              <p className="text-sm text-graphite-100">{item.label}</p>
              <p className="mt-0.5 text-xs text-graphite-500">{item.hint}</p>
            </div>
            <span className="text-xs text-graphite-600">قيد الإنشاء</span>
          </li>
        ))}
      </ul>

      <button
        type="button"
        onClick={logout}
        className="w-full rounded-xl border border-red-400/30 bg-red-500/10 py-3 text-sm font-semibold text-red-300 transition hover:bg-red-500/15"
      >
        تسجيل الخروج
      </button>
    </section>
  );
}
