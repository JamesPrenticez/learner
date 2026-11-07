import react from '@vitejs/plugin-react'
import { defineConfig } from 'vite'

// https://vite.dev/config/
export default defineConfig({
  plugins: [react()],
  // strictPort: fail instead of drifting to 3001, which is the API's port.
  server: { port: 3000, strictPort: true },
})
