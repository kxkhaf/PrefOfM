import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react-swc'
import * as fs from "node:fs";
import * as path from "node:path";

export default defineConfig({
  server: {
    https: {
      cert: fs.readFileSync(path.resolve(__dirname, 'certs/crt', 'nginx.crt')),
      key: fs.readFileSync(path.resolve(__dirname, 'certs/private', 'nginx.key')),
    },
    proxy: {
      '/auth': {
        target: 'https://localhost:7404',
        rewrite: (path) => path.replace(/^\/auth/, ''),
        changeOrigin: true,
      },
    },
    port: 1111,
    hmr: {
      protocol: 'wss',
      host: 'localhost',
    },
  },
  plugins: [react()],
  resolve: {
    alias: {
      '@': path.resolve(__dirname, './src'),
      '@api': path.resolve(__dirname, './src/api')
    }
  }
})
