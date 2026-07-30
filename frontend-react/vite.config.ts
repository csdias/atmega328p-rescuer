import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';

/**
 * O build vai para o wwwroot da API: em produção é a API que serve o React, na mesma
 * origem, e assim não há CORS nem porta separada para quem está à bancada.
 *
 * Em desenvolvimento o Vite serve na 5173 e encaminha /api e /hub para o Kestrel — o
 * código do front end usa sempre caminhos relativos e não sabe onde a API está.
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
