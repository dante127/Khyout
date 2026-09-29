import { useEffect, useMemo, useState } from 'react';
import type { FormEvent, ReactNode } from 'react';
import { Link, useNavigate, useSearchParams } from 'react-router-dom';
import { getCategoryTree, getProduct } from '../lib/api/catalog';
import type { CategoryNode, ProductDetail, UnitOfMeasure } from '../lib/api/catalog';
import { createRfq } from '../lib/api/rfqs';
import { ApiError } from '../lib/api/client';
import { unitLabel } from '../lib/format';

const unitOptions: UnitOfMeasure[] = ['Meter', 'Kg', 'Roll', 'Yard'];

const inputClass =
  'w-full rounded-xl border border-graphite-700 bg-graphite-950 px-3.5 py-2.5 text-sm text-graphite-50 outline-none placeholder:text-graphite-600 focus:border-bronze-500 focus:ring-2 focus:ring-bronze-500/40';

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

export default function RfqCreatePage() {
  const navigate = useNavigate();
  const [searchParams] = useSearchParams();
  const productId = searchParams.get('productId');

  const [categories, setCategories] = useState<{ id: string; label: string }[]>([]);
  const [sourceProduct, setSourceProduct] = useState<ProductDetail | null>(null);

  const [title, setTitle] = useState('');
  const [categoryId, setCategoryId] = useState('');
  const [quantity, setQuantity] = useState('');
  const [unit, setUnit] = useState<UnitOfMeasure>('Meter');
  const [targetDate, setTargetDate] = useState('');
  const [closingLocal, setClosingLocal] = useState('');
  const [description, setDescription] = useState('');

  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    getCategoryTree()
      .then((tree: CategoryNode[]) => {
        const flat: { id: string; label: string }[] = [];
        for (const root of tree) {
          flat.push({ id: root.id, label: root.nameAr || root.nameEn });
          for (const child of root.children) {
            flat.push({ id: child.id, label: `— ${child.nameAr || child.nameEn}` });
          }
        }
        setCategories(flat);
      })
      .catch(() => setCategories([]));
  }, []);

  useEffect(() => {
    if (!productId) return;
    let cancelled = false;
    getProduct(productId)
      .then((product) => {
        if (cancelled) return;
        setSourceProduct(product);
        setCategoryId((current) => current || product.categoryId);
        setUnit(product.unitOfMeasure);
        setTitle((current) => current || `توريد ${product.title}`);
      })
      .catch((cause: unknown) => {
        if (!cancelled) {
          setError(cause instanceof ApiError ? cause.message : 'تعذّر تحميل المنتج المرجعي');
        }
      });
    return () => {
      cancelled = true;
    };
  }, [productId]);

  const canSubmit = useMemo(
    () =>
      title.trim().length > 0 &&
      categoryId !== '' &&
      Number(quantity) > 0 &&
      targetDate !== '' &&
      closingLocal !== '',
    [title, categoryId, quantity, targetDate, closingLocal],
  );

  const handleSubmit = async (event: FormEvent) => {
    event.preventDefault();
    setBusy(true);
    setError(null);
    try {
      const created = await createRfq({
        categoryId,
        title: title.trim(),
        description: description.trim() || null,
        quantityNeeded: Number(quantity),
        unitOfMeasure: unit,
        targetDeliveryDate: targetDate,
        closingDate: new Date(closingLocal).toISOString(),
      });
      navigate(`/rfqs/${created.id}`, { replace: true });
    } catch (cause) {
      setError(cause instanceof ApiError ? cause.message : 'تعذّر إنشاء الطلب');
      setBusy(false);
    }
  };

  return (
    <section className="space-y-4">
      <div className="flex items-center justify-between">
        <h2 className="text-xl font-bold text-graphite-50">طلب عرض سعر جديد</h2>
        <Link to="/rfqs" className="text-xs text-graphite-400 transition hover:text-graphite-200">
          ‹ طلباتي
        </Link>
      </div>
      <p className="text-xs leading-5 text-graphite-500">
        حدّد المواصفات والكمية والمهلة؛ ستصل العروض سرّياً من الموردين الموثوقين حتى موعد الإغلاق.
      </p>

      {sourceProduct ? (
        <div className="rounded-xl border border-bronze-600/40 bg-bronze-500/10 px-3.5 py-2.5 text-xs text-bronze-200">
          مرتبط بالمنتج: <b>{sourceProduct.title}</b>
        </div>
      ) : null}

      <form onSubmit={handleSubmit} className="space-y-4 rounded-2xl border border-graphite-800 bg-graphite-900 p-4">
        <Field label="عنوان الطلب" htmlFor="rfq-title">
          <input
            id="rfq-title"
            type="text"
            value={title}
            onChange={(event) => setTitle(event.target.value)}
            className={inputClass}
            required
            maxLength={300}
          />
        </Field>

        <Field label="الفئة" htmlFor="rfq-category">
          <select
            id="rfq-category"
            value={categoryId}
            onChange={(event) => setCategoryId(event.target.value)}
            className={inputClass}
            required
          >
            <option value="" disabled>
              اختر الفئة…
            </option>
            {categories.map((category) => (
              <option key={category.id} value={category.id}>
                {category.label}
              </option>
            ))}
          </select>
        </Field>

        <div className="grid grid-cols-2 gap-3">
          <Field label="الكمية" htmlFor="rfq-qty">
            <input
              id="rfq-qty"
              type="number"
              inputMode="decimal"
              min={1}
              step="any"
              value={quantity}
              onChange={(event) => setQuantity(event.target.value)}
              className={inputClass}
              required
            />
          </Field>
          <Field label="الوحدة" htmlFor="rfq-unit">
            <select
              id="rfq-unit"
              value={unit}
              onChange={(event) => setUnit(event.target.value as UnitOfMeasure)}
              className={inputClass}
            >
              {unitOptions.map((option) => (
                <option key={option} value={option}>
                  {unitLabel(option)}
                </option>
              ))}
            </select>
          </Field>
        </div>

        <div className="grid grid-cols-2 gap-3">
          <Field label="تاريخ التسليم المطلوب" htmlFor="rfq-target">
            <input
              id="rfq-target"
              type="date"
              value={targetDate}
              onChange={(event) => setTargetDate(event.target.value)}
              className={inputClass}
              required
            />
          </Field>
          <Field label="موعد إغلاق العروض" htmlFor="rfq-closing">
            <input
              id="rfq-closing"
              type="datetime-local"
              value={closingLocal}
              onChange={(event) => setClosingLocal(event.target.value)}
              className={inputClass}
              required
            />
          </Field>
        </div>

        <Field label="تفاصيل إضافية (اختياري)" htmlFor="rfq-description">
          <textarea
            id="rfq-description"
            rows={3}
            maxLength={4000}
            value={description}
            onChange={(event) => setDescription(event.target.value)}
            className={`${inputClass} resize-none`}
          />
        </Field>

        {error ? (
          <p role="alert" className="rounded-lg bg-red-500/10 px-3 py-2 text-xs leading-5 text-red-300">
            {error}
          </p>
        ) : null}

        <button
          type="submit"
          disabled={busy || !canSubmit}
          className="w-full rounded-xl bg-bronze-500 py-3 text-sm font-bold text-graphite-950 transition hover:bg-bronze-400 disabled:opacity-50"
        >
          {busy ? 'جارٍ الإرسال…' : 'نشر الطلب'}
        </button>
      </form>
    </section>
  );
}
