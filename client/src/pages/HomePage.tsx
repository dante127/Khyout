import { Link } from 'react-router-dom';

const features = [
  {
    to: '/catalog',
    emoji: '🧶',
    title: 'الكتالوج الفني',
    description: 'أقمشة بخاماتها ومواصفاتها: الوزن (GSM)، التركيب، نوع النسيج',
  },
  {
    to: '/rfqs',
    emoji: '📋',
    title: 'طلبات العروض',
    description: 'أنشئ طلب عرض سعر محدد المدة واستقبل عروضاً من الموردين',
  },
  {
    to: '/samples',
    emoji: '✂️',
    title: 'طلبات العينات',
    description: 'اطلب عينات قماش لتقييمها قبل تنفيذ الطلب النهائي',
  },
];

export default function HomePage() {
  return (
    <section className="space-y-5">
      <div className="rounded-2xl border border-graphite-800 bg-gradient-to-bl from-graphite-900 to-graphite-950 p-5">
        <h2 className="text-xl font-bold text-graphite-50">مرحباً بك في خيوط 👋</h2>
        <p className="mt-2 text-sm leading-6 text-graphite-400">
          منصة تربط ورش الخياطة بموردي الأقمشة والخيوط — مواصفات فنية دقيقة، طلبات عروض شفافة،
          وإشعارات فورية عبر تيليجرام.
        </p>
      </div>

      <div className="grid gap-3">
        {features.map((feature) => (
          <Link
            key={feature.to}
            to={feature.to}
            className="flex items-center gap-4 rounded-2xl border border-graphite-800 bg-graphite-900 p-4 transition hover:border-bronze-600/60 hover:bg-graphite-800/60"
          >
            <span className="text-2xl" aria-hidden="true">
              {feature.emoji}
            </span>
            <span className="flex-1">
              <span className="block text-sm font-semibold text-graphite-100">{feature.title}</span>
              <span className="mt-0.5 block text-xs leading-5 text-graphite-500">
                {feature.description}
              </span>
            </span>
            <span className="text-graphite-600" aria-hidden="true">
              ‹
            </span>
          </Link>
        ))}
      </div>

      <p className="text-center text-[11px] leading-5 text-graphite-600">
        يعمل التطبيق جزئياً دون اتصال — ستُرسل تغييراتك تلقائياً عند عودة الشبكة
      </p>
    </section>
  );
}
