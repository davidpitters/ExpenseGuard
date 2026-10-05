import { defineConfig } from 'vitest/config';
import react from '@vitejs/plugin-react';
import tailwindcss from '@tailwindcss/vite';
import { existsSync, readFileSync } from 'node:fs';
import { fileURLToPath } from 'node:url';

const certificate = fileURLToPath(
  new URL('../.local/localhost.pem', import.meta.url),
);
const privateKey = fileURLToPath(
  new URL('../.local/localhost.key', import.meta.url),
);

export default defineConfig({
  plugins: [react(), tailwindcss()],
  server: {
    port: 5173,
    strictPort: true,
    https:
      existsSync(certificate) && existsSync(privateKey)
        ? { cert: readFileSync(certificate), key: readFileSync(privateKey) }
        : undefined,
    proxy: {
      '/api': {
        target: 'http://localhost:5100',
        changeOrigin: true,
        configure(proxy) {
          proxy.on('proxyReq', (proxyRequest, request) => {
            proxyRequest.setHeader(
              'X-Forwarded-Proto',
              'encrypted' in request.socket && request.socket.encrypted === true
                ? 'https'
                : 'http',
            );
          });
        },
      },
    },
  },
  test: {
    environment: 'jsdom',
    setupFiles: ['./src/test/setup.ts'],
    restoreMocks: true,
  },
});
