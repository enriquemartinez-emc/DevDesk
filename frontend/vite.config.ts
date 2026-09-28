import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';
import tailwindcss from '@tailwindcss/vite';
import path from 'node:path';

const serverTarget = process.env.SERVER_HTTPS || process.env.SERVER_HTTP;
const agentTarget = process.env.AGENT_HTTPS || process.env.AGENT_HTTP;

// https://vite.dev/config/
export default defineConfig({
  plugins: [react(), tailwindcss()],
  resolve: {
    alias: {
      '@': path.resolve(import.meta.dirname, './src'),
    },
  },
  server: {
    proxy: {
      // Ticket API on DevDesk.Server
      '/api': {
        target: serverTarget,
        changeOrigin: true,
        secure: false,
      },
      // AG-UI event stream on DevDesk.Agent (mapped at its root)
      '/agui': {
        target: agentTarget,
        changeOrigin: true,
        secure: false,
        rewrite: (requestPath) => requestPath.replace(/^\/agui/, '') || '/',
      },
    },
  },
});
