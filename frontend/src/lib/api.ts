import { Api } from '@/api/devdesk-api';

// Same-origin: the Vite dev server proxies /api to DevDesk.Server, and Server hosts the built app in production.
export const api = new Api({ baseUrl: '' });
