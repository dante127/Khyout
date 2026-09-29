import { useEffect, useMemo, useState } from 'react';
import type { ChangeEvent } from 'react';
import { Link, useNavigate, useParams } from 'react-router-dom';
import { getProduct, uploadProductImage } from '../lib/api/catalog';
import type { ProductDetail } from '../lib/api/catalog';
import { ApiError } from '../lib/api/client';
import { fiberLabel, formatDate, formatMoney, formatNumber, mediaUrl, unitLabel, weaveLabel } from '../lib/format';
import { decodeJwtClaims } from '../lib/jwt';
import { tokenStore } from '../lib/storage';

function BackLink() {
  return (
    <Link
      to="/catalog"
      className="inline-flex items-center gap-1 text-xs text-graphite-400 transition hover:text-graphite-200"
    >
      <span aria-hidden="true">›</span> العودة إلى الكتالوج
    </Link>
  );
}

function SpecRow({ label, value }: { label: string; value: string | null }) {
  if (!value) return null;
  return (
    <div className="flex items-center justify-between gap-2 rounded-lg bg-graphite-950/60 px-3 py-2">
      <dt className="text-graphite-500">{label}</dt>
      <dd className="font-medium text-graphite-200">{value}</dd>
    </div>
  );
}

export default function ProductDetailPage() {
  const { productId } = useParams<{ productId: string }>();
  const navigate = useNavigate();

  const [product, setProduct] = useState<ProductDetail | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [activeImage, setActiveImage] = useState(0);
  const [uploading, setUploading] = useState(false);
  const [uploadError, setUploadError] = useState<string | null>(null);

  useEffect(() => {
    if (!productId) return;
    let cancelled = false;
    setProduct(null);
    setError(null);
    setActiveImage(0);

    getProduct(productId)
      .then((loaded) => {
        if (!cancelled) setProduct(loaded);
      })
      .catch((cause: unknown) => {
        if (!cancelled) {
          setError(cause instanceof ApiError ? cause.message : 'تعذّر تحميل المنتج');
        }
      });

    return () => {
      cancelled = true;
    };
  }, [productId]);

  const claims = useMemo(() => decodeJwtClaims(tokenStore.get()?.accessToken ?? ''), []);
  const isOwner =
    product !== null && claims?.role === 'Supplier' && claims.companyId === product.supplierCompanyId;

  const handleFileChange = async (event: ChangeEvent<HTMLInputElement>) => {
    const file = event.target.files?.[0];
    event.target.value = '';
    if (!file || !product) return;

    setUploading(true);
    setUploadError(null);
    try {
      const image = await uploadProductImage(product.id, file);
      const isNew = !product.images.some((existing) => existing.id === image.id);
      setProduct((previous) =>
        previous
          ? { ...previous, images: isNew ? [...previous.images, image] : previous.images }
          : previous,
      );
      if (isNew) setActiveImage(product.images.length);
    } catch (cause) {
      setUploadError(cause instanceof ApiError ? cause.message : 'تعذّر رفع الصورة');
    } finally {
      setUploading(false);
    }
  };

  if (error) {
    return (
      <section className="space-y-4">
        <BackLink />
        <div role="alert" className="rounded-2xl border border-red-400/30 bg-red-500/10 p-4 text-sm text-red-300">
          {error}
        </div>
      </section>
    );
  }

  if (!product) {
    return (
      <section className="space-y-4">
        <BackLink />
        <div className="h-64 animate-pulse rounded-2xl border border-graphite-800 bg-graphite-900" />
        <div className="h-24 animate-pulse rounded-2xl border border-graphite-800 bg-graphite-900" />
      </section>
    );
  }

  const active = product.images[activeImage] ?? null;

  return (
    <section className="space-y-5">
      <BackLink />

      <div className="overflow-hidden rounded-2xl border border-graphite-800 bg-graphite-900">
        <div className="flex aspect-[4/3] items-center justify-center bg-graphite-950">
          {active ? (
            <img src={mediaUrl(active.storagePath)} alt={product.title} className="h-full w-full object-cover" />
          ) : (
            <span className="text-4xl" aria-hidden="true">
              🧵
            </span>
          )}
        </div>
        {product.images.length > 1 ? (
          <div className="flex gap-2 overflow-x-auto p-3">
            {product.images.map((image, index) => (
              <button
                key={image.id}
                type="button"
                onClick={() => setActiveImage(index)}
                aria-label={`صورة ${index + 1}`}
                className={`shrink-0 overflow-hidden rounded-lg border ${
                  index === activeImage ? 'border-bronze-500' : 'border-graphite-700'
                }`}
              >
                <img src={mediaUrl(image.storagePath)} alt="" className="h-14 w-14 object-cover" />
              </button>
            ))}
          </div>
        ) : null}
      </div>

      <div className="space-y-3 rounded-2xl border border-graphite-800 bg-graphite-900 p-4">
        <div className="flex items-start justify-between gap-3">
          <h2 className="text-lg font-bold leading-7 text-graphite-50">{product.title}</h2>
          <span className="shrink-0 rounded-full bg-graphite-800 px-2.5 py-1 text-[11px] text-graphite-300">
            {product.status === 'Active' ? 'نشط' : product.status === 'Draft' ? 'مسودة' : 'مؤرشف'}
          </span>
        </div>
        <p className="text-xs text-graphite-400">{product.categoryName}</p>
        <div className="flex items-baseline justify-between gap-3">
          <span className="text-sm text-graphite-300">
            أدنى كمية:{' '}
            <b className="text-graphite-100">
              {formatNumber(product.moq)} {unitLabel(product.unitOfMeasure)}
            </b>
          </span>
          <span className="text-sm font-semibold text-bronze-300">
            {product.indicativePrice !== null
              ? formatMoney(product.indicativePrice, product.currency)
              : 'السعر عند الطلب'}
          </span>
        </div>
        {product.description ? (
          <p className="text-sm leading-6 text-graphite-300">{product.description}</p>
        ) : null}
      </div>

      <div className="rounded-2xl border border-graphite-800 bg-graphite-900 p-4">
        <h3 className="mb-3 text-sm font-bold text-graphite-100">المواصفات الفنية</h3>
        <dl className="grid grid-cols-2 gap-2 text-xs">
          <SpecRow
            label="الوزن"
            value={
              product.gsm !== null
                ? `${product.gsm} GSM${product.gsmTolerancePct ? ` ±${product.gsmTolerancePct}%` : ''}`
                : null
            }
          />
          <SpecRow label="النسيج" value={weaveLabel(product.weaveStructure)} />
          <SpecRow label="العرض" value={product.widthCm !== null ? `${product.widthCm} سم` : null} />
          <SpecRow
            label="وزن المتر"
            value={product.weightPerMeterG !== null ? `${product.weightPerMeterG} غ` : null}
          />
          <SpecRow label="اللون" value={product.colorFamily} />
          <SpecRow label="العناية" value={product.careNotes} />
        </dl>
        {product.composition.length > 0 ? (
          <div className="mt-4 space-y-2">
            <p className="text-xs font-semibold text-graphite-300">التركيب</p>
            {product.composition.map((fiber) => (
              <div key={fiber.fiberType} className="flex items-center gap-3 text-xs">
                <span className="w-14 shrink-0 text-graphite-300">{fiberLabel(fiber.fiberType)}</span>
                <div className="h-1.5 flex-1 overflow-hidden rounded-full bg-graphite-800">
                  <div
                    className="h-full rounded-full bg-bronze-500"
                    style={{ width: `${Math.min(100, fiber.percentage)}%` }}
                  />
                </div>
                <span className="w-12 shrink-0 text-left text-graphite-400" dir="ltr">
                  {formatNumber(fiber.percentage, 1)}%
                </span>
              </div>
            ))}
          </div>
        ) : null}
      </div>

      <div className="rounded-2xl border border-graphite-800 bg-graphite-900 p-4">
        <h3 className="text-sm font-bold text-graphite-100">{product.supplierName}</h3>
        <p className="mt-1 text-xs text-graphite-400">
          {product.supplierCity} · نُشر {formatDate(product.createdAt)}
        </p>
      </div>

      {isOwner ? (
        <div className="space-y-2 rounded-2xl border border-graphite-800 bg-graphite-900 p-4">
          <p className="text-sm font-semibold text-graphite-100">صور المنتج</p>
          <p className="text-xs leading-5 text-graphite-500">
            تُحوَّل الصور تلقائياً إلى WebP بحجم لا يزيد عن 150 كيلوبايت وبأبعاد تصل إلى 1600 بكسل.
          </p>
          <label
            className={`inline-flex cursor-pointer items-center justify-center rounded-xl border border-bronze-600/50 bg-bronze-500/10 px-4 py-2.5 text-xs font-semibold text-bronze-300 transition hover:bg-bronze-500/15 ${
              uploading ? 'pointer-events-none opacity-50' : ''
            }`}
          >
            <input
              type="file"
              accept="image/jpeg,image/png,image/webp"
              className="sr-only"
              disabled={uploading}
              onChange={(event) => void handleFileChange(event)}
            />
            {uploading ? 'جارٍ المعالجة والرفع…' : 'إضافة صورة'}
          </label>
          {uploadError ? (
            <p role="alert" className="text-xs text-red-300">
              {uploadError}
            </p>
          ) : null}
        </div>
      ) : claims?.role === 'Buyer' ? (
        <button
          type="button"
          disabled={product.status !== 'Active'}
          onClick={() => navigate(`/rfqs/new?productId=${product.id}`)}
          className="w-full rounded-xl bg-bronze-500 py-3.5 text-sm font-bold text-graphite-950 transition hover:bg-bronze-400 disabled:opacity-50"
        >
          طلب عرض سعر
        </button>
      ) : null}
    </section>
  );
}
