import { defineConfig } from 'vite';
import vue from '@vitejs/plugin-vue';
// @ts-ignore - Node.js built-in module
import { fileURLToPath, URL } from 'node:url';
// @ts-ignore - Node.js built-in module  
import { dirname, resolve } from 'node:path';

// @ts-ignore - import.meta.url is available in ESM
const __filename = fileURLToPath(import.meta.url);
const __dirname = dirname(__filename);

export default defineConfig({
  plugins: [vue()],
  resolve: {
    alias: {
      '@': resolve(__dirname, 'src')
    }
  },
  server: {
    host: '0.0.0.0', // Bind to all network interfaces
    port: 5173, // Use default Vite port (80 requires admin on Windows)
    strictPort: false, // Allow Vite to use another port if 5173 is taken
    // HMR will automatically use the same host/port as the server
    // No need to configure it explicitly - Vite will auto-detect from the request
  }
});


