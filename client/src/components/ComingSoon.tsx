export default function ComingSoon({ title, description }: { title: string; description: string }) {
  return (
    <section className="space-y-3">
      <h2 className="text-xl font-bold text-graphite-50">{title}</h2>
      <p className="text-sm leading-6 text-graphite-400">{description}</p>
      <div className="rounded-2xl border border-dashed border-graphite-700 bg-graphite-900/50 p-6 text-center">
        <p className="text-sm text-graphite-400">قيد الإنشاء — المرحلة القادمة</p>
      </div>
    </section>
  );
}
