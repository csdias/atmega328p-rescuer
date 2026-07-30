import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';

/**
 * The build goes into the API's wwwroot: in production it is the API that serves the React
 * app, on the same origin, so there is no CORS and no separate port for whoever is at the
 * bench.
 *
 * In development Vite serves on 5173 and forwards /api and /hub to Kestrel — the front end
 * code always uses relative paths and does not know where the API is.
 */
export default defineConfig({
  plugins: [react()],
  build: {
    outDir: '../ATMegaPestaV1.Api/wwwroot',
    emptyOutDir: true,
  },
  server: {
    port: 5173,
    proxy: {
      '/api': { target: 'http://localhost:5099', changeOrigin: true },
      // ws porque o hub de progresso sobe a WebSocket.
      '/hub': { target: 'http://localhost:5099', changeOrigin: true, ws: true },
    },
  },
});
