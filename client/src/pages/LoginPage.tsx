import { useState } from 'react';
import type { FormEvent } from 'react';
import { Navigate, useNavigate } from 'react-router-dom';
import { useAuth } from '../auth/AuthContext';
import { ApiError, NetworkError } from '../lib/api/client';

function describeError(cause: unknown): string {
  if (cause instanceof NetworkError) {
    return 'تعذّر الاتصال بالخادم — تحقّق من اتصالك بالإنترنت';
  }
  if (cause instanceof ApiError) {
    return cause.message;
  }
  return 'حدث خطأ غير متوقع، حاول مجدداً';
}

export default function LoginPage() {
  const { isAuthenticated, pendingPhone, requestOtp, verifyOtp, backToPhone } = useAuth();
  const navigate = useNavigate();

  const [phone, setPhone] = useState('');
  const [code, setCode] = useState('');
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  if (isAuthenticated) {
    return <Navigate to="/" replace />;
  }

  const handleRequestOtp = async (event: FormEvent) => {
    event.preventDefault();
    setError(null);
    setBusy(true);
    try {
      await requestOtp(phone.trim());
    } catch (cause) {
      setError(describeError(cause));
    } finally {
      setBusy(false);
    }
  };

  const handleVerify = async (event: FormEvent) => {
    event.preventDefault();
    setError(null);
    setBusy(true);
    try {
      await verifyOtp(code.trim());
      navigate('/', { replace: true });
    } catch (cause) {
      setError(describeError(cause));
    } finally {
      setBusy(false);
    }
  };

  return (
    <div className="flex min-h-dvh flex-col items-center justify-center bg-graphite-950 px-4 py-10">
      <div className="w-full max-w-sm">
        <div className="mb-8 text-center">
          <div
            className="mx-auto mb-3 flex h-14 w-14 items-center justify-center rounded-2xl bg-bronze-500/15 text-2xl"
            aria-hidden="true"
          >
            🧵
          </div>
          <h1 className="text-2xl font-bold text-graphite-50">خيوط</h1>
          <p className="mt-1 text-sm leading-6 text-graphite-400">
            سوق الأقمشة والنسيج — منصة B2B لسلسلة التوريد السورية
          </p>
        </div>

        <div className="rounded-2xl border border-graphite-800 bg-graphite-900 p-5">
          {pendingPhone === null ? (
            <form onSubmit={handleRequestOtp} className="space-y-4">
              <div>
                <label htmlFor="phone" className="mb-1.5 block text-sm text-graphite-200">
                  رقم الهاتف
                </label>
                <input
                  id="phone"
                  type="tel"
                  inputMode="tel"
                  dir="ltr"
                  autoComplete="tel"
                  placeholder="+963 9XX XXX XXX"
                  value={phone}
                  onChange={(event) => setPhone(event.target.value)}
                  required
                  minLength={9}
                  maxLength={20}
                  className="w-full rounded-xl border border-graphite-700 bg-graphite-950 px-3.5 py-2.5 text-left text-graphite-50 outline-none placeholder:text-graphite-500 focus:border-bronze-500 focus:ring-2 focus:ring-bronze-500/40"
                />
              </div>

              {error ? (
                <p role="alert" className="rounded-lg bg-red-500/10 px-3 py-2 text-xs leading-5 text-red-300">
                  {error}
                </p>
              ) : null}

              <button
                type="submit"
                disabled={busy}
                className="w-full rounded-xl bg-bronze-500 py-3 text-sm font-bold text-graphite-950 transition hover:bg-bronze-400 disabled:opacity-50"
              >
                {busy ? 'جارٍ الإرسال…' : 'إرسال رمز التحقق'}
              </button>
            </form>
          ) : (
            <form onSubmit={handleVerify} className="space-y-4">
              <p className="text-sm leading-6 text-graphite-200">
                أدخل رمز التحقق المكوّن من 6 أرقام المُرسل إلى
                <span dir="ltr" className="mx-1 font-semibold text-bronze-300">
                  {pendingPhone}
                </span>
              </p>

              <input
                aria-label="رمز التحقق"
                type="text"
                inputMode="numeric"
                autoComplete="one-time-code"
                dir="ltr"
                placeholder="······"
                value={code}
                onChange={(event) => setCode(event.target.value.replace(/\D/g, ''))}
                required
                minLength={6}
                maxLength={6}
                className="w-full rounded-xl border border-graphite-700 bg-graphite-950 px-3.5 py-3 text-center text-lg tracking-[0.5em] text-graphite-50 outline-none placeholder:text-graphite-600 focus:border-bronze-500 focus:ring-2 focus:ring-bronze-500/40"
              />

              {error ? (
                <p role="alert" className="rounded-lg bg-red-500/10 px-3 py-2 text-xs leading-5 text-red-300">
                  {error}
                </p>
              ) : null}

              <button
                type="submit"
                disabled={busy}
                className="w-full rounded-xl bg-bronze-500 py-3 text-sm font-bold text-graphite-950 transition hover:bg-bronze-400 disabled:opacity-50"
              >
                {busy ? 'جارٍ التحقق…' : 'تأكيد الرمز'}
              </button>

              <button
                type="button"
                onClick={backToPhone}
                className="w-full text-center text-xs text-graphite-400 transition hover:text-graphite-200"
              >
                تغيير رقم الهاتف
              </button>
            </form>
          )}
        </div>

        <p className="mt-6 text-center text-[11px] leading-5 text-graphite-600">
          في بيئة التطوير تظهر رموز التحقق في سجلّ الخادم (LoggingSmsSender)
        </p>
      </div>
    </div>
  );
}
