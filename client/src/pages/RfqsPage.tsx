import { useCallback, useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import StatusChip, { rfqTone } from '../components/StatusChip';
import { getMyRfqs } from '../lib/api/rfqs';
import type { RfqStatus, RfqSummary } from '../lib/api/rfqs';
import { ApiError } from '../lib/api/client';
import { daysLeft, formatDate, formatNumber, rfqStatusLabel, unitLabel } from '../lib/format';
import { decodeJwtClaims } from '../lib/jwt';
import { tokenStore } from '../lib/storage';

const statusFilters: { value: RfqStatus | null; label: string }[] = [
  { value: null, label: 'الكل' },
  { value: 'Open', label: 'مفتوحة' },
  { value: 'Awarded', label: 'مُرساة' },
  { value: 'Cancelled', label: 'ملغاة' },
  { value: 'Expired', label: 'منتهية' },
];

function chipClass(active: boolean): string {
  return `shrink-0 rounded-full border px-3.5 py-1.5 text-xs transition ${
    active
      ? 'border-bronze-500 bg-bronze-500/15 text-bronze-300'
      : 'border-graphite-700 bg-graphite-900 text-graphite-400 hover:text-graphite-200'
  }`;
}

function RfqCard({ rfq }: { rfq: RfqSummary }) {
  const remaining = daysLeft(rfq.closingDate);
  return (
    <Link
      to={`/rfqs/${rfq.id}`}
      className="block rounded-2xl border border-graphite-800 bg-graphite-900 p-4 transition hover:border-bronze-600/60 hover:bg-graphite-800/60"
    >
      <div className="flex items-start justify-between gap-3">
        <h3 className="line-clamp-2 text-sm font-semibold text-graphite-100">{rfq.title}</h3>
        <StatusChip label={rfqStatusLabel(rfq.status)} tone={rfqTone(rfq.status)} />
      </div>
      <p className="mt-2 text-xs text-graphite-400">
        {rfq.categoryName} · {formatNumber(rfq.quantityNeeded)} {unitLabel(rfq.unitOfMeasure)}
      </p>
      <div className="mt-3 flex items-center justify-between text-[11px]">
        <span className={rfq.status === 'Open' && remaining <= 3 ? 'text-bronze-300' : 'text-graphite-500'}>
          {rfq.status === 'Open'
            ? remaining > 0
              ? `يُغلق خلال ${remaining} يوم`
              : 'جاهز للإغلاق التلقائي'
            : `أُغلق ${formatDate(rfq.closingDate)}`}
        </span>
        <span className="text-graphite-400">{rfq.bidCount} عروض</span>
      </div>
    </Link>
  );
}

export default function RfqsPage() {
  const claims = decodeJwtClaims(tokenStore.get()?.accessToken ?? '');
  const isBuyer = claims?.role === 'Buyer';

  const [status, setStatus] = useState<RfqStatus | null>(null);
  const [items, setItems] = useState<RfqSummary[]>([]);
  const [page, setPage] = useState(1);
  const [totalPages, setTotalPages] = useState(1);

  const [loading, setLoading] = useState(true);
  const [loadingMore, setLoadingMore] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const load = useCallback(
    async (targetPage: number, append: boolean) => {
      if (!isBuyer) return;
      if (append) {
        setLoadingMore(true);
      } else {
        setLoading(true);
      }
      setError(null);

      try {
        const result = await getMyRfqs({
          status: status ?? undefined,
          pageNumber: targetPage,
          pageSize: 20,
        });
        setItems((previous) => (append ? [...previous, ...result.items] : result.items));
        setPage(result.pageNumber);
        setTotalPages(result.totalPages);
      } catch (cause) {
        setError(cause instanceof ApiError ? cause.message : 'تعذّر تحميل الطلبات');
      } finally {
        setLoading(false);
        setLoadingMore(false);
      }
    },
    [isBuyer, status],
  );

  useEffect(() => {
    void load(1, false);
  }, [load]);

  if (!isBuyer) {
    return (
      <section className="space-y-4">
        <h2 className="text-xl font-bold text-graphite-50">الطلبات</h2>
        <div className="rounded-2xl border border-graphite-800 bg-graphite-900 p-5 text-sm leading-6 text-graphite-300">
          كمورّد، ستصلك إشعارات فورية عبر تيليجرام لكل طلب مطابق لمنتجاتك، مع رابط مباشر لعرض
          التفاصيل وتقديم عرضك السّري.
          <p className="mt-2 text-xs text-graphite-500">
            يمكنك أيضاً فتح أي طلب عبر رابطه المباشر من الإشعار.
          </p>
        </div>
      </section>
    );
  }

  return (
    <section className="space-y-4">
      <div className="flex items-center justify-between">
        <h2 className="text-xl font-bold text-graphite-50">طلباتي</h2>
        <Link
          to="/rfqs/new"
          className="rounded-xl bg-bronze-500 px-3.5 py-2 text-xs font-bold text-graphite-950 transition hover:bg-bronze-400"
        >
          + طلب جديد
        </Link>
      </div>

      <div className="flex gap-2 overflow-x-auto pb-1">
        {statusFilters.map((filter) => (
          <button
            key={filter.label}
            type="button"
            onClick={() => setStatus(filter.value)}
            className={chipClass(status === filter.value)}
          >
            {filter.label}
          </button>
        ))}
      </div>

      {loading ? (
        <div className="space-y-3">
          {[0, 1, 2].map((key) => (
            <div key={key} className="h-24 animate-pulse rounded-2xl border border-graphite-800 bg-graphite-900" />
          ))}
        </div>
      ) : error ? (
        <div role="alert" className="rounded-2xl border border-red-400/30 bg-red-500/10 p-4 text-sm text-red-300">
          <p>{error}</p>
          <button
            type="button"
            onClick={() => void load(1, false)}
            className="mt-3 rounded-lg border border-red-400/40 px-3 py-1.5 text-xs font-semibold transition hover:bg-red-500/10"
          >
            إعادة المحاولة
          </button>
        </div>
      ) : items.length === 0 ? (
        <div className="rounded-2xl border border-dashed border-graphite-700 bg-graphite-900/50 p-8 text-center text-sm text-graphite-400">
          لا توجد طلبات بعد — أنشئ طلبك الأول
        </div>
      ) : (
        <>
          <div className="space-y-3">
            {items.map((rfq) => (
              <RfqCard key={rfq.id} rfq={rfq} />
            ))}
          </div>
          {page < totalPages ? (
            <button
              type="button"
              disabled={loadingMore}
              onClick={() => void load(page + 1, true)}
              className="w-full rounded-xl border border-graphite-700 bg-graphite-900 py-3 text-sm font-semibold text-graphite-200 transition hover:border-bronze-600/60 disabled:opacity-50"
            >
              {loadingMore ? 'جارٍ التحميل…' : 'تحميل المزيد'}
            </button>
          ) : null}
        </>
      )}
    </section>
  );
}
