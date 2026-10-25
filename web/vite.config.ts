import react from '@vitejs/plugin-react'
import { defineConfig } from 'vite'

export default defineConfig({
  base: '/pidlab/',
  plugins: [react()],
  worker: { format: 'es' },
})
