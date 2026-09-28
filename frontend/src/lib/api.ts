import { Api } from '@/api/devdesk-api';

// Same-origin: the Vite dev server proxies /api to DevDesk.Server, and Server hosts the built app in production.
const SAME_ORIGIN = '';

export const api = new Api({ baseUrl: SAME_ORIGIN });
