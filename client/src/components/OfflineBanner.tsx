import { useEffect, useState } from 'react';
import { useOnline } from '../offline/useOnline';
import { outbox } from '../offline/outbox';

export default function OfflineBanner() {
  const online = useOnline();
  const [pending, setPending] = useState(0);

  useEffect(() => {
    if (online) return;
    let cancelled = false;
    outbox
      .count()
      .then((n) => {
        if (!cancelled) setPending(n);
      })
      .catch(() => {
        /* outbox storage unavailable — banner still shows offline state */
      });
    return () => {
      cancelled = true;
    };
  }, [online]);

  if (online) return null;

  return (
    <div
      role="status"
      className="bg-bronze-700/20 px-4 py-2 text-center text-xs leading-5 text-bronze-300"
    >
      غير متصل بالإنترنت — يمكنك التصفح، وستُرسل تغييراتك تلقائياً عند عودة الاتصال
      {pending > 0 ? ` (${pending} بانتظار الإرسال)` : ''}
    </div>
  );
}
