import { useCallback, useEffect, useState } from 'react';
import StatusChip, { sampleTone } from '../components/StatusChip';
import { listSamples, updateSampleStatus } from '../lib/api/samples';
import type { Sample, SampleStatus } from '../lib/api/samples';
import { ApiError } from '../lib/api/client';
import { formatDate, formatNumber, sampleStatusLabel } from '../lib/format';
import { decodeJwtClaims } from '../lib/jwt';
import { tokenStore } from '../lib/storage';

const filters: { value: SampleStatus | null; label: string }[] = [
  { value: null, label: 'الكل' },
  { value: 'Requested', label: 'مطلوبة' },
  { value: 'Approved', label: 'معتمدة' },
  { value: 'Shipped', label: 'مشحونة' },
  { value: 'Received', label: 'مستلمة' },
  { value: 'Rejected', label: 'مرفوضة' },
];

function chipClass(active: boolean): string {
  return `shrink-0 rounded-full border px-3.5 py-1.5 text-xs transition ${
    active
      ? 'border-bronze-500 bg-bronze-500/15 text-bronze-300'
      : 'border-graphite-700 bg-graphite-900 text-graphite-400 hover:text-graphite-200'
  }`;
}

interface SampleAction {
  label: string;
  target: SampleStatus;
  danger?: boolean;
}

function actionsFor(status: SampleStatus): SampleAction[] {
  switch (status) {
    case 'Requested':
      return [
        { label: 'اعتماد', target: 'Approved' },
        { label: 'رفض', target: 'Rejected', danger: true },
      ];
    case 'Approved':
      return [{ label: 'وسم كمشحونة', target: 'Shipped' }];
    case 'Shipped':
      return [{ label: 'تأكيد الاستلام', target: 'Received' }];
    default:
      return [];
  }
}

export default function SamplesPage() {
  const claims = decodeJwtClaims(tokenStore.get()?.accessToken ?? '');
  const isSupplier = claims?.role === 'Supplier';

  const [status, setStatus] = useState<SampleStatus | null>(null);
  const [items, setItems] = useState<Sample[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [busyId, setBusyId] = useState<string | null>(null);
  const [actionError, setActionError] = useState<string | null>(null);

  const load = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      const result = await listSamples({ status: status ?? undefined, pageNumber: 1, pageSize: 50 });
      setItems(result.items);
    } catch (cause) {
      setError(cause instanceof ApiError ? cause.message : 'تعذّر تحميل طلبات العينات');
    } finally {
      setLoading(false);
    }
  }, [status]);

  useEffect(() => {
    void load();
  }, [load]);

  const runAction = async (sample: Sample, target: SampleStatus) => {
    setBusyId(sample.id);
    setActionError(null);
    try {
      const updated = await updateSampleStatus(sample.id, target);
      setItems((previous) => previous.map((item) => (item.id === updated.id ? updated : item)));
    } catch (cause) {
      setActionError(cause instanceof ApiError ? cause.message : 'تعذّر تحديث حالة العينة');
    } finally {
      setBusyId(null);
    }
  };

  return (
    <section className="space-y-4">
      <div className="flex items-center justify-between">
        <h2 className="text-xl font-bold text-graphite-50">العينات</h2>
      </div>

      {!isSupplier ? (
        <p className="text-xs leading-5 text-graphite-500">
          اطلب العينات من صفحة أي منتج في الكتالوج — زر «طلب عينة».
        </p>
      ) : null}

      <div className="flex gap-2 overflow-x-auto pb-1">
        {filters.map((filter) => (
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

      {actionError ? (
        <p role="alert" className="rounded-lg bg-red-500/10 px-3 py-2 text-xs leading-5 text-red-300">
          {actionError}
        </p>
      ) : null}

      {loading ? (
        <div className="space-y-3">
          {[0, 1].map((key) => (
            <div key={key} className="h-28 animate-pulse rounded-2xl border border-graphite-800 bg-graphite-900" />
          ))}
        </div>
      ) : error ? (
        <div role="alert" className="rounded-2xl border border-red-400/30 bg-red-500/10 p-4 text-sm text-red-300">
          <p>{error}</p>
          <button
            type="button"
            onClick={() => void load()}
            className="mt-3 rounded-lg border border-red-400/40 px-3 py-1.5 text-xs font-semibold transition hover:bg-red-500/10"
          >
            إعادة المحاولة
          </button>
        </div>
      ) : items.length === 0 ? (
        <div className="rounded-2xl border border-dashed border-graphite-700 bg-graphite-900/50 p-8 text-center text-sm text-graphite-400">
          لا توجد طلبات عينات
        </div>
      ) : (
        <div className="space-y-3">
          {items.map((sample) => {
            const actions = isSupplier ? actionsFor(sample.status) : [];
            return (
              <div key={sample.id} className="rounded-2xl border border-graphite-800 bg-graphite-900 p-4">
                <div className="flex items-start justify-between gap-3">
                  <h3 className="line-clamp-2 text-sm font-semibold text-graphite-100">{sample.productTitle}</h3>
                  <StatusChip label={sampleStatusLabel(sample.status)} tone={sampleTone(sample.status)} />
                </div>
                <p className="mt-2 text-xs text-graphite-400">
                  {isSupplier ? `من: ${sample.buyerCompanyName}` : `إلى: ${sample.supplierCompanyName}`}
                  {' · '}
                  الكمية: {formatNumber(sample.quantity)}
                  {sample.deliveryCity ? ` · التسليم: ${sample.deliveryCity}` : ''}
                </p>
                <p className="mt-1.5 text-[11px] text-graphite-500">طُلب في {formatDate(sample.createdAt)}</p>
                {sample.note ? <p className="mt-2 text-xs leading-5 text-graphite-400">{sample.note}</p> : null}
                {actions.length > 0 ? (
                  <div className="mt-3 flex gap-2">
                    {actions.map((action) => (
                      <button
                        key={action.target}
                        type="button"
                        disabled={busyId === sample.id}
                        onClick={() => void runAction(sample, action.target)}
                        className={
                          action.danger
                            ? 'rounded-lg border border-red-400/40 bg-red-500/10 px-3 py-1.5 text-xs font-semibold text-red-300 transition hover:bg-red-500/15 disabled:opacity-50'
                            : 'rounded-lg border border-emerald-400/40 bg-emerald-500/15 px-3 py-1.5 text-xs font-semibold text-emerald-300 transition hover:bg-emerald-500/25 disabled:opacity-50'
                        }
                      >
                        {action.label}
                      </button>
                    ))}
                  </div>
                ) : null}
              </div>
            );
          })}
        </div>
      )}
    </section>
  );
}
