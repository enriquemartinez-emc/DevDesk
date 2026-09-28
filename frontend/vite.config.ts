import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';
import tailwindcss from '@tailwindcss/vite';
import path from 'node:path';

const serverTarget = process.env.SERVER_HTTPS || process.env.SERVER_HTTP;

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
      // Ticket API and the AG-UI event stream, both on DevDesk.Server
      '/api': {
        target: serverTarget,
        changeOrigin: true,
        secure: false,
      },
      '/agui': {
        target: serverTarget,
        changeOrigin: true,
        secure: false,
      },
    },
  },
});
