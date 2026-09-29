# Khyout Client (PWA) — خيوط

Mobile-first, Arabic (RTL) web client for the Khyout B2B textile marketplace —
links garment workshops (buyers) with fabric/yarn suppliers. Built with Vite,
React 19, TypeScript, and Tailwind CSS v4, designed for low-bandwidth,
intermittent-connectivity use (offline queue scaffold included).

## Requirements

- Node.js 20+ (developed against Node 22)
- The Khyout API running locally (see the repository root `README.md`)

## Quick start

1. Start the API from the repository root — pin the port so the dev proxy below works:

   ```sh
   dotnet run --project src/Khyout.Api --urls http://localhost:5000
   ```

   Development login uses SMS OTP; the 6-digit code is written to the API console
   log by `LoggingSmsSender` (no real SMS is sent in dev).

2. Install and start the client:

   ```sh
   cd client
   npm install
   npm run dev
   ```

3. Open <http://localhost:5173>.

The dev server proxies `/api/*` to `VITE_API_PROXY_TARGET` (default
`http://localhost:5000`), so no CORS setup is needed in development. In
production the app is served behind the same nginx origin as the API (see
`deploy/nginx/`).

## Environment variables

| Variable | Default | Purpose |
| --- | --- | --- |
| `VITE_API_BASE_URL` | *(empty)* | Absolute API origin for cross-origin setups. Leave empty to use relative URLs (dev proxy / production nginx). |
| `VITE_API_PROXY_TARGET` | `http://localhost:5000` | Dev-only proxy target for `/api`. |

Copy `.env.example` to `.env.local` to override. `.env.local` is git-ignored.

## Scripts

| Command | Purpose |
| --- | --- |
| `npm run dev` | Vite dev server with the API proxy on port 5173 |
| `npm run build` | Typecheck (`tsc -b`) + production build to `dist/` |
| `npm run preview` | Serve the production build locally |
| `npm test` | Run the Vitest suite once |
| `npm run test:watch` | Vitest in watch mode |

## Architecture notes

- `src/lib/api/` — typed fetch wrapper for the API envelope
  `{ success, data, error }`: bearer auth, single-flight refresh-token rotation
  on 401, typed `ApiError`/`NetworkError`.
- `src/auth/` — `AuthContext` state machine: phone → OTP request → verify →
  persisted token pair; session restore and logout.
- `src/offline/` — `useOnline` hook, IndexedDB-backed outbox queue
  (`enqueue`/`all`/`count`/`remove`/`markFailed`). The replay wiring
  (`flush`, background sync) lands in Phase 4b-2 and is intentionally stubbed.
- `src/app/` + `src/components/` — shell: header, bottom navigation, offline
  banner, RTL layout.
- Tests live in colocated `__tests__/` folders.

## Phase status

- **4b-1 (this milestone):** scaffold, app shell, auth flow, API layer with
  refresh rotation, offline queue skeleton, test suite.
- **4b-2a:** catalog browse with filters + pagination, product detail (specs,
  composition, gallery), owner image-upload UI, typed catalog API modules.
- **4b-2b (this milestone):** RFQ create form (prefilled from products), buyer
  my-RFQs list with status filters, role-aware RFQ detail with the full
  quotation flow (blind bids: buyer sees all bids with accept/reject; supplier
  submits, tracks and withdraws own bid), quotation action API modules.
- **4b-2c (next):** samples screens + typed module, offline flush + Workbox
  background sync.
