const DB_NAME = 'khyout-offline';
const DB_VERSION = 1;

export const OUTBOX_STORE = 'outbox';

export function openDb(): Promise<IDBDatabase> {
  return new Promise((resolve, reject) => {
    const request = indexedDB.open(DB_NAME, DB_VERSION);
    request.onupgradeneeded = () => {
      const db = request.result;
      if (!db.objectStoreNames.contains(OUTBOX_STORE)) {
        db.createObjectStore(OUTBOX_STORE, { keyPath: 'id' });
      }
    };
    request.onsuccess = () => resolve(request.result);
    request.onerror = () => reject(request.error);
  });
}

/**
 * Runs a single request against the outbox object store and resolves with its
 * result once the transaction has fully completed.
 */
export function withStore<T>(mode: IDBTransactionMode, run: (store: IDBObjectStore) => IDBRequest<T>): Promise<T> {
  return openDb().then(
    (db) =>
      new Promise<T>((resolve, reject) => {
        const transaction = db.transaction(OUTBOX_STORE, mode);
        const request = run(transaction.objectStore(OUTBOX_STORE));
        let result!: T;
        request.onsuccess = () => {
          result = request.result;
        };
        transaction.oncomplete = () => {
          resolve(result);
          db.close();
        };
        transaction.onerror = () => {
          reject(transaction.error);
          db.close();
        };
        transaction.onabort = () => {
          reject(transaction.error);
          db.close();
        };
      }),
  );
}
