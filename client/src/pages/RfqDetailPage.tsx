import { useCallback, useEffect, useMemo, useState } from 'react';
import type { FormEvent, ReactNode } from 'react';
import { Link, useParams } from 'react-router-dom';
import StatusChip, { quotationTone, rfqTone } from '../components/StatusChip';
import {
  acceptQuotation,
  closeRfq,
  extendRfq,
  getRfq,
  getRfqBids,
  rejectQuotation,
  submitQuotation,
  withdrawQuotation,
} from '../lib/api/rfqs';
import type { RfqBid, RfqDetailResult } from '../lib/api/rfqs';
import { ApiError } from '../lib/api/client';
import {
  daysLeft,
  formatDate,
  formatMoney,
  formatNumber,
  quotationStatusLabel,
  rfqStatusLabel,
  unitLabel,
} from '../lib/format';
import { decodeJwtClaims } from '../lib/jwt';
import { tokenStore } from '../lib/storage';

const inputClass =
  'w-full rounded-xl border border-graphite-700 bg-graphite-950 px-3.5 py-2.5 text-sm text-graphite-50 outline-none placeholder:text-graphite-600 focus:border-bronze-500 focus:ring-2 focus:ring-bronze-500/40';

const currencyOptions = ['USD', 'SYP', 'TRY'];

function InfoCell({ label, value }: { label: string; value: string }) {
  return (
    <div className="rounded-lg bg-graphite-950/60 px-3 py-2">
      <p className="text-[11px] text-graphite-500">{label}</p>
      <p className="mt-0.5 font-medium text-graphite-200">{value}</p>
    </div>
  );
}

function Field({ label, htmlFor, children }: { label: string; htmlFor: string; children: ReactNode }) {
  return (
    <div>
      <label htmlFor={htmlFor} className="mb-1.5 block text-xs text-graphite-300">
        {label}
      </label>
      {children}
    </div>
  );
}

export default function RfqDetailPage() {
  const { rfqId } = useParams<{ rfqId: string }>();

  const [result, setResult] = useState<RfqDetailResult | null>(null);
  const [bids, setBids] = useState<RfqBid[]>([]);
  const [error, setError] = useState<string | null>(null);
  const [actionError, setActionError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  const [extendLocal, setExtendLocal] = useState('');
  const [price, setPrice] = useState('');
  const [currency, setCurrency] = useState('USD');
  const [validUntil, setValidUntil] = useState('');
  const [leadTime, setLeadTime] = useState('');
  const [bidNote, setBidNote] = useState('');

  const claims = useMemo(() => decodeJwtClaims(tokenStore.get()?.accessToken ?? ''), []);
  const isAdmin = claims?.role === 'Admin';

  const load = useCallback(async () => {
    if (!rfqId) return;
    setError(null);
    try {
      const detail = await getRfq(rfqId);
      setResult(detail);
      if (detail.isBuyerOwner || isAdmin) {
        const bidPage = await getRfqBids(rfqId, { pageNumber: 1, pageSize: 50 });
        setBids(bidPage.items);
      }
    } catch (cause) {
      setError(cause instanceof ApiError ? cause.message : 'تعذّر تحميل الطلب');
    }
  }, [rfqId, isAdmin]);

  useEffect(() => {
    void load();
  }, [load]);

  const runAction = useCallback(
    async (action: () => Promise<unknown>) => {
      setBusy(true);
      setActionError(null);
      try {
        await action();
        await load();
      } catch (cause) {
        setActionError(cause instanceof ApiError ? cause.message : 'تعذّر تنفيذ العملية');
      } finally {
        setBusy(false);
      }
    },
    [load],
  );

  const handleSubmitBid = (event: FormEvent) => {
    event.preventDefault();
    if (!result) return;
    void runAction(() =>
      submitQuotation(result.rfq.id, {
        unitPrice: Number(price),
        currency,
        validUntil: new Date(validUntil).toISOString(),
        leadTimeDays: Number(leadTime),
        note: bidNote.trim() || null,
      }),
    );
  };

  if (error) {
    return (
      <section className="space-y-4">
        <Link to="/rfqs" className="inline-flex items-center gap-1 text-xs text-graphite-400 transition hover:text-graphite-200">
          <span aria-hidden="true">›</span> الطلبات
        </Link>
        <div role="alert" className="rounded-2xl border border-red-400/30 bg-red-500/10 p-4 text-sm text-red-300">
          {error}
        </div>
      </section>
    );
  }

  if (!result) {
    return (
      <section className="space-y-4">
        <div className="h-40 animate-pulse rounded-2xl border border-graphite-800 bg-graphite-900" />
        <div className="h-24 animate-pulse rounded-2xl border border-graphite-800 bg-graphite-900" />
      </section>
    );
  }

  const rfq = result.rfq;
  const remaining = daysLeft(rfq.closingDate);
  const isSupplier = claims?.role === 'Supplier';
  const myQuotation = result.myQuotation;
  const canBid = isSupplier && !myQuotation && result.canReceiveBids && rfq.status === 'Open';

  return (
    <section className="space-y-4">
      <Link
        to="/rfqs"
        className="inline-flex items-center gap-1 text-xs text-graphite-400 transition hover:text-graphite-200"
      >
        <span aria-hidden="true">›</span> {result.isBuyerOwner ? 'طلباتي' : 'الطلبات'}
      </Link>

      <div className="space-y-3 rounded-2xl border border-graphite-800 bg-graphite-900 p-4">
        <div className="flex items-start justify-between gap-3">
          <h2 className="text-lg font-bold leading-7 text-graphite-50">{rfq.title}</h2>
          <StatusChip label={rfqStatusLabel(rfq.status)} tone={rfqTone(rfq.status)} />
        </div>
        <p className="text-xs text-graphite-400">
          {rfq.categoryName} · المشتري: {rfq.buyerCompanyName}
        </p>
        {rfq.description ? <p className="text-sm leading-6 text-graphite-300">{rfq.description}</p> : null}
        <div className="grid grid-cols-2 gap-2">
          <InfoCell label="الكمية" value={`${formatNumber(rfq.quantityNeeded)} ${unitLabel(rfq.unitOfMeasure)}`} />
          <InfoCell label="التسليم المطلوب" value={formatDate(rfq.targetDeliveryDate)} />
          <InfoCell
            label="إغلاق العروض"
            value={
              rfq.status === 'Open' && remaining > 0
                ? `${formatDate(rfq.closingDate)} (بعد ${remaining} يوم)`
                : formatDate(rfq.closingDate)
            }
          />
          <InfoCell label="العروض" value={String(result.bidCount)} />
        </div>
      </div>

      {actionError ? (
        <p role="alert" className="rounded-lg bg-red-500/10 px-3 py-2 text-xs leading-5 text-red-300">
          {actionError}
        </p>
      ) : null}

      {result.isBuyerOwner && rfq.status === 'Open' ? (
        <div className="space-y-3 rounded-2xl border border-graphite-800 bg-graphite-900 p-4">
          <h3 className="text-sm font-bold text-graphite-100">إدارة الطلب</h3>
          <div className="flex flex-wrap items-end gap-2">
            <div className="min-w-[220px] flex-1">
              <Field label="تمديد الإغلاق حتى" htmlFor="extend-date">
                <input
                  id="extend-date"
                  type="datetime-local"
                  value={extendLocal}
                  onChange={(event) => setExtendLocal(event.target.value)}
                  className={inputClass}
                />
              </Field>
            </div>
            <button
              type="button"
              disabled={busy || !extendLocal}
              onClick={() => void runAction(() => extendRfq(rfq.id, new Date(extendLocal).toISOString()))}
              className="rounded-xl border border-graphite-600 px-3.5 py-2.5 text-xs font-semibold text-graphite-200 transition hover:border-bronze-600/60 disabled:opacity-50"
            >
              تمديد
            </button>
          </div>
          <button
            type="button"
            disabled={busy}
            onClick={() => {
              if (window.confirm('إغلاق الطلب الآن ومنع العروض الجديدة؟')) {
                void runAction(() => closeRfq(rfq.id));
              }
            }}
            className="w-full rounded-xl border border-red-400/40 bg-red-500/10 py-2.5 text-xs font-semibold text-red-300 transition hover:bg-red-500/15 disabled:opacity-50"
          >
            إغلاق الطلب الآن
          </button>
        </div>
      ) : null}

      {result.isBuyerOwner || isAdmin ? (
        <div className="space-y-3 rounded-2xl border border-graphite-800 bg-graphite-900 p-4">
          <div className="flex items-center justify-between">
            <h3 className="text-sm font-bold text-graphite-100">العروض المستلمة</h3>
            <span className="text-[11px] text-graphite-500">سرّية — لا تظهر إلا لك</span>
          </div>
          {bids.length === 0 ? (
            <p className="rounded-xl border border-dashed border-graphite-700 bg-graphite-950/60 p-4 text-center text-xs text-graphite-400">
              لا توجد عروض حتى الآن
            </p>
          ) : (
            bids.map((bid) => {
              const canAct = rfq.status === 'Open' && bid.status === 'Submitted';
              return (
                <div key={bid.quotationId} className="rounded-xl border border-graphite-800 bg-graphite-950/60 p-3.5">
                  <div className="flex items-start justify-between gap-3">
                    <p className="text-sm font-semibold text-graphite-100">{bid.supplierName}</p>
                    <StatusChip label={quotationStatusLabel(bid.status)} tone={quotationTone(bid.status)} />
                  </div>
                  <p className="mt-2 text-sm text-bronze-300">
                    {formatMoney(bid.unitPrice, bid.currency)}{' '}
                    <span className="text-xs text-graphite-400">/ {unitLabel(rfq.unitOfMeasure)}</span>
                  </p>
                  <p className="mt-1.5 text-[11px] text-graphite-500">
                    التوريد خلال {bid.leadTimeDays} يوم · صالح حتى {formatDate(bid.validUntil)}
                  </p>
                  {bid.note ? <p className="mt-2 text-xs leading-5 text-graphite-400">{bid.note}</p> : null}
                  {canAct ? (
                    <div className="mt-3 flex gap-2">
                      <button
                        type="button"
                        disabled={busy}
                        onClick={() => {
                          if (window.confirm(`قبول عرض ${bid.supplierName} وترسية الطلب؟`)) {
                            void runAction(() => acceptQuotation(bid.quotationId));
                          }
                        }}
                        className="rounded-lg border border-emerald-400/40 bg-emerald-500/15 px-3 py-1.5 text-xs font-semibold text-emerald-300 transition hover:bg-emerald-500/25 disabled:opacity-50"
                      >
                        قبول وترسية
                      </button>
                      <button
                        type="button"
                        disabled={busy}
                        onClick={() => void runAction(() => rejectQuotation(bid.quotationId))}
                        className="rounded-lg border border-red-400/40 bg-red-500/10 px-3 py-1.5 text-xs font-semibold text-red-300 transition hover:bg-red-500/15 disabled:opacity-50"
                      >
                        رفض
                      </button>
                    </div>
                  ) : null}
                </div>
              );
            })
          )}
        </div>
      ) : null}

      {isSupplier && myQuotation ? (
        <div className="rounded-2xl border border-graphite-800 bg-graphite-900 p-4">
          <div className="flex items-start justify-between gap-3">
            <h3 className="text-sm font-bold text-graphite-100">عرضك المقدَّم</h3>
            <StatusChip
              label={quotationStatusLabel(myQuotation.status)}
              tone={quotationTone(myQuotation.status)}
            />
          </div>
          <p className="mt-2 text-sm text-bronze-300">
            {formatMoney(myQuotation.unitPrice, myQuotation.currency)}{' '}
            <span className="text-xs text-graphite-400">/ {unitLabel(rfq.unitOfMeasure)}</span>
          </p>
          <p className="mt-1.5 text-[11px] text-graphite-500">
            التوريد خلال {myQuotation.leadTimeDays} يوم · صالح حتى {formatDate(myQuotation.validUntil)}
          </p>
          {myQuotation.note ? <p className="mt-2 text-xs text-graphite-400">{myQuotation.note}</p> : null}
          {myQuotation.status === 'Submitted' && rfq.status === 'Open' ? (
            <button
              type="button"
              disabled={busy}
              onClick={() => {
                if (window.confirm('سحب عرضك لهذا الطلب؟')) {
                  void runAction(() => withdrawQuotation(myQuotation.quotationId));
                }
              }}
              className="mt-3 rounded-lg border border-red-400/40 bg-red-500/10 px-3 py-1.5 text-xs font-semibold text-red-300 transition hover:bg-red-500/15 disabled:opacity-50"
            >
              سحب العرض
            </button>
          ) : null}
        </div>
      ) : canBid ? (
        <form onSubmit={handleSubmitBid} className="space-y-3 rounded-2xl border border-graphite-800 bg-graphite-900 p-4">
          <h3 className="text-sm font-bold text-graphite-100">تقديم عرضك</h3>
          <p className="text-[11px] leading-5 text-graphite-500">
            عروضك سرّية — لا يراها المشترون كعروض المنافسين، ولا يرى الموردون عروض بعضهم بعضاً.
          </p>
          <div className="grid grid-cols-2 gap-3">
            <Field label="سعر الوحدة" htmlFor="bid-price">
              <input
                id="bid-price"
                type="number"
                inputMode="decimal"
                min={0}
                step="any"
                value={price}
                onChange={(event) => setPrice(event.target.value)}
                className={inputClass}
                required
              />
            </Field>
            <Field label="العملة" htmlFor="bid-currency">
              <select
                id="bid-currency"
                value={currency}
                onChange={(event) => setCurrency(event.target.value)}
                className={inputClass}
              >
                {currencyOptions.map((option) => (
                  <option key={option} value={option}>
                    {option}
                  </option>
                ))}
              </select>
            </Field>
          </div>
          <div className="grid grid-cols-2 gap-3">
            <Field label="صالح حتى" htmlFor="bid-valid">
              <input
                id="bid-valid"
                type="date"
                value={validUntil}
                onChange={(event) => setValidUntil(event.target.value)}
                className={inputClass}
                required
              />
            </Field>
            <Field label="مدة التوريد (يوم)" htmlFor="bid-lead">
              <input
                id="bid-lead"
                type="number"
                inputMode="numeric"
                min={1}
                value={leadTime}
                onChange={(event) => setLeadTime(event.target.value)}
                className={inputClass}
                required
              />
            </Field>
          </div>
          <Field label="ملاحظة (اختياري)" htmlFor="bid-note">
            <textarea
              id="bid-note"
              rows={2}
              maxLength={1000}
              value={bidNote}
              onChange={(event) => setBidNote(event.target.value)}
              className={`${inputClass} resize-none`}
            />
          </Field>
          <button
            type="submit"
            disabled={busy || !(Number(price) > 0 && validUntil !== '' && Number(leadTime) > 0)}
            className="w-full rounded-xl bg-bronze-500 py-3 text-sm font-bold text-graphite-950 transition hover:bg-bronze-400 disabled:opacity-50"
          >
            {busy ? 'جارٍ التقديم…' : 'تقديم العرض'}
          </button>
        </form>
      ) : isSupplier ? (
        <div className="rounded-2xl border border-dashed border-graphite-700 bg-graphite-900/50 p-5 text-center text-xs text-graphite-400">
          انتهت فترة استقبال العروض لهذا الطلب أو لم يعد بالإمكان التقديم
        </div>
      ) : null}
    </section>
  );
}
