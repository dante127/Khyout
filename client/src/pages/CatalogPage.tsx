import { useCallback, useEffect, useState } from 'react';
import ProductCard from '../components/ProductCard';
import { getCategoryTree, searchProducts } from '../lib/api/catalog';
import type { CategoryNode, ProductSearchParams, ProductSummary } from '../lib/api/catalog';
import { ApiError } from '../lib/api/client';

type SortOption = 'newest' | 'moqAsc' | 'gsmAsc';

const sortOptions: { value: SortOption; label: string }[] = [
  { value: 'newest', label: 'الأحدث' },
  { value: 'moqAsc', label: 'أقل كمية' },
  { value: 'gsmAsc', label: 'الأخف وزناً' },
];

function chipClass(active: boolean): string {
  return `shrink-0 rounded-full border px-3.5 py-1.5 text-xs transition ${
    active
      ? 'border-bronze-500 bg-bronze-500/15 text-bronze-300'
      : 'border-graphite-700 bg-graphite-900 text-graphite-400 hover:text-graphite-200'
  }`;
}

export default function CatalogPage() {
  const [categories, setCategories] = useState<CategoryNode[]>([]);
  const [categoryId, setCategoryId] = useState<string | null>(null);
  const [sort, setSort] = useState<SortOption>('newest');

  const [items, setItems] = useState<ProductSummary[]>([]);
  const [page, setPage] = useState(1);
  const [totalPages, setTotalPages] = useState(1);
  const [totalCount, setTotalCount] = useState(0);

  const [loading, setLoading] = useState(true);
  const [loadingMore, setLoadingMore] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    getCategoryTree()
      .then(setCategories)
      .catch(() => setCategories([]));
  }, []);

  const load = useCallback(
    async (targetPage: number, append: boolean) => {
      if (append) {
        setLoadingMore(true);
      } else {
        setLoading(true);
      }
      setError(null);

      try {
        const params: ProductSearchParams = { sort, pageNumber: targetPage, pageSize: 20 };
        if (categoryId) params.categoryId = categoryId;

        const result = await searchProducts(params);
        setItems((previous) => (append ? [...previous, ...result.items] : result.items));
        setPage(result.pageNumber);
        setTotalPages(result.totalPages);
        setTotalCount(result.totalCount);
      } catch (cause) {
        setError(cause instanceof ApiError ? cause.message : 'تعذّر تحميل المنتجات');
      } finally {
        setLoading(false);
        setLoadingMore(false);
      }
    },
    [categoryId, sort],
  );

  useEffect(() => {
    void load(1, false);
  }, [load]);

  return (
    <section className="space-y-4">
      <div className="flex items-center justify-between">
        <h2 className="text-xl font-bold text-graphite-50">الكتالوج الفني</h2>
        {!loading && !error && totalCount > 0 ? (
          <span className="text-xs text-graphite-500">{totalCount} نتيجة</span>
        ) : null}
      </div>

      <div className="flex gap-2 overflow-x-auto pb-1">
        <button type="button" onClick={() => setCategoryId(null)} className={chipClass(categoryId === null)}>
          الكل
        </button>
        {categories.map((category) => (
          <button
            key={category.id}
            type="button"
            onClick={() => setCategoryId(category.id)}
            className={chipClass(categoryId === category.id)}
          >
            {category.nameAr || category.nameEn}
          </button>
        ))}
      </div>

      <div className="flex items-center gap-2">
        <label htmlFor="catalog-sort" className="text-xs text-graphite-400">
          ترتيب:
        </label>
        <select
          id="catalog-sort"
          value={sort}
          onChange={(event) => setSort(event.target.value as SortOption)}
          className="rounded-lg border border-graphite-700 bg-graphite-900 px-2.5 py-1.5 text-xs text-graphite-200 outline-none focus:border-bronze-500"
        >
          {sortOptions.map((option) => (
            <option key={option.value} value={option.value}>
              {option.label}
            </option>
          ))}
        </select>
      </div>

      {loading ? (
        <div className="space-y-3">
          {[0, 1, 2].map((key) => (
            <div key={key} className="h-28 animate-pulse rounded-2xl border border-graphite-800 bg-graphite-900" />
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
          لا توجد منتجات مطابقة بعد
        </div>
      ) : (
        <>
          <div className="grid gap-3 sm:grid-cols-2">
            {items.map((product) => (
              <ProductCard key={product.id} product={product} />
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
