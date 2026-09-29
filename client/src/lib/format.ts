const unitLabels: Record<string, string> = {
  Meter: 'متر',
  Kg: 'كجم',
  Roll: 'لفة',
  Yard: 'ياردة',
};

const fiberLabels: Record<string, string> = {
  Cotton: 'قطن',
  Lycra: 'ليكرا',
  Viscose: 'فيسكوز',
  Polyester: 'بوليستر',
  Wool: 'صوف',
  Linen: 'كتان',
  Silk: 'حرير',
  Nylon: 'نايلون',
  Other: 'أخرى',
};

const weaveLabels: Record<string, string> = {
  Plain: 'سادة',
  Twill: 'مبرد',
  Satin: 'ساتان',
  Knit: 'تريكو',
  Denim: 'دنيم',
};

const rfqStatusLabels: Record<string, string> = {
  Open: 'مفتوح',
  Awarded: 'مُرسى',
  Cancelled: 'ملغى',
  Expired: 'منتهي',
};

export function unitLabel(unit: string): string {
  return unitLabels[unit] ?? unit;
}

export function fiberLabel(fiber: string): string {
  return fiberLabels[fiber] ?? fiber;
}

export function weaveLabel(weave: string | null | undefined): string {
  if (!weave) return '';
  return weaveLabels[weave] ?? weave;
}

const quotationStatusLabels: Record<string, string> = {
  Submitted: 'مقدَّم',
  Accepted: 'مقبول',
  Rejected: 'مرفوض',
  Expired: 'منتهي',
  Withdrawn: 'مسحوب',
};

export function rfqStatusLabel(status: string): string {
  return rfqStatusLabels[status] ?? status;
}

const sampleStatusLabels: Record<string, string> = {
  Requested: 'مطلوبة',
  Approved: 'معتمدة',
  Shipped: 'تم الشحن',
  Received: 'تم الاستلام',
  Rejected: 'مرفوضة',
};

export function quotationStatusLabel(status: string): string {
  return quotationStatusLabels[status] ?? status;
}

export function sampleStatusLabel(status: string): string {
  return sampleStatusLabels[status] ?? status;
}

export function formatNumber(value: number, maxFractionDigits = 2): string {
  return value.toLocaleString('en-US', { maximumFractionDigits: maxFractionDigits });
}

export function formatMoney(
  value: number | null | undefined,
  currency: string | null | undefined,
): string {
  if (value === null || value === undefined) return '—';
  const suffix = currency ? ` ${currency}` : '';
  return `${formatNumber(value)}${suffix}`;
}

export function formatDate(iso: string | null | undefined): string {
  if (!iso) return '—';
  const date = new Date(iso);
  if (Number.isNaN(date.getTime())) return '—';
  return new Intl.DateTimeFormat('ar-SY', {
    numberingSystem: 'latn',
    day: 'numeric',
    month: 'short',
    year: 'numeric',
  }).format(date);
}

export function daysLeft(iso: string): number {
  return Math.ceil((new Date(iso).getTime() - Date.now()) / 86_400_000);
}

/** Maps a stored media path to its public URL (served by the API at /media). */
export function mediaUrl(storagePath: string): string {
  if (/^https?:\/\//i.test(storagePath)) return storagePath;
  if (storagePath.startsWith('/media/')) return storagePath;
  return `/media/${storagePath.replace(/^\/+/, '')}`;
}
