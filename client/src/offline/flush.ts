import { ApiError, NetworkError, request } from '../lib/api/client';
import { outbox } from './outbox';

export interface FlushResult {
  sent: number;
  dropped: number;
  failed: number;
}

/**
 * Replays queued offline mutations in creation order.
 * - success → item removed
 * - network failure → item marked failed, replay stops (still offline)
 * - permanent rejection (4xx, except 429) → item dropped — retrying cannot help
 * - transient server failure (5xx / 429) → item marked failed, replay stops
 */
export async function flushOutbox(): Promise<FlushResult> {
  const items = (await outbox.all()).sort((a, b) => a.createdAt.localeCompare(b.createdAt));
  const result: FlushResult = { sent: 0, dropped: 0, failed: 0 };

  for (const item of items) {
    try {
      await request(item.url, { method: item.method, body: item.body });
      await outbox.remove(item.id);
      result.sent += 1;
    } catch (cause) {
      if (cause instanceof NetworkError) {
        await outbox.markFailed(item.id, 'offline');
        result.failed += 1;
        break;
      }
      if (cause instanceof ApiError && cause.status < 500 && cause.status !== 429) {
        await outbox.remove(item.id);
        result.dropped += 1;
        continue;
      }
      await outbox.markFailed(item.id, cause instanceof ApiError ? String(cause.status) : 'error');
      result.failed += 1;
      break;
    }
  }

  return result;
}
