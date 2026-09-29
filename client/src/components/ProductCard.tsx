import { Link } from 'react-router-dom';
import type { ProductSummary } from '../lib/api/catalog';
import { formatMoney, formatNumber, unitLabel } from '../lib/format';

export default function ProductCard({ product }: { product: ProductSummary }) {
  return (
    <Link
      to={`/catalog/${product.id}`}
      className="block rounded-2xl border border-graphite-800 bg-graphite-900 p-4 transition hover:border-bronze-600/60 hover:bg-graphite-800/60"
    >
      <div className="flex items-start justify-between gap-3">
        <h3 className="line-clamp-2 text-sm font-semibold text-graphite-100">{product.title}</h3>
        {product.gsm !== null ? (
          <span className="shrink-0 rounded-full bg-bronze-500/15 px-2 py-0.5 text-[11px] font-medium text-bronze-300">
            {product.gsm} GSM
          </span>
        ) : null}
      </div>

      <p className="mt-2 text-xs text-graphite-400">
        {product.categoryName} · {product.supplierName}
      </p>

      <div className="mt-3 flex items-baseline justify-between text-xs">
        <span className="text-graphite-300">
          أدنى كمية: {formatNumber(product.moq)} {unitLabel(product.unitOfMeasure)}
        </span>
        {product.indicativePrice !== null ? (
          <span className="font-semibold text-bronze-300">
            {formatMoney(product.indicativePrice, product.currency)}
          </span>
        ) : (
          <span className="text-graphite-500">السعر عند الطلب</span>
        )}
      </div>

      <p className="mt-2 text-[11px] text-graphite-500">{product.supplierCity}</p>
    </Link>
  );
}
