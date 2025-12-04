import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

export default defineConfig({
  plugins: [react()],
  server: {
    // Esto le dice a Vite: "Si Aspire me da un puerto (process.env.PORT), úsalo. 
    // Si no, usa el 5173 de siempre".
    port: process.env.PORT ? Number(process.env.PORT) : 5173,
    strictPort: true, 
    host: true 
  }
})