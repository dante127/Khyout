export type Tone = 'info' | 'success' | 'muted' | 'warn' | 'danger';

const toneClasses: Record<Tone, string> = {
  info: 'border-sky-400/40 bg-sky-400/10 text-sky-300',
  success: 'border-emerald-400/40 bg-emerald-400/10 text-emerald-300',
  muted: 'border-graphite-600 bg-graphite-800 text-graphite-300',
  warn: 'border-bronze-500/50 bg-bronze-500/10 text-bronze-300',
  danger: 'border-red-400/40 bg-red-500/10 text-red-300',
};

export function rfqTone(status: string): Tone {
  return status === 'Open' ? 'info' : status === 'Awarded' ? 'success' : 'muted';
}

export function quotationTone(status: string): Tone {
  switch (status) {
    case 'Submitted':
      return 'info';
    case 'Accepted':
      return 'success';
    case 'Rejected':
      return 'danger';
    default:
      return 'muted';
  }
}

export function sampleTone(status: string): Tone {
  switch (status) {
    case 'Requested':
      return 'info';
    case 'Approved':
      return 'success';
    case 'Shipped':
      return 'warn';
    case 'Received':
      return 'success';
    case 'Rejected':
      return 'danger';
    default:
      return 'muted';
  }
}

export default function StatusChip({ label, tone = 'muted' }: { label: string; tone?: Tone }) {
  return (
    <span className={`shrink-0 rounded-full border px-2.5 py-0.5 text-[11px] ${toneClasses[tone]}`}>
      {label}
    </span>
  );
}
