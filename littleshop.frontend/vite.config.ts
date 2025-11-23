import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

// https://vite.dev/config/
export default defineConfig({
    plugins: [react()],
    server: {
        // CORRECCIÓN: Si el Dashboard (.NET Aspire) nos da un puerto, lo usamos.
        // Si no, usamos el 5173 por defecto.
        port: process.env.PORT ? Number(process.env.PORT) : 5173,
        strictPort: true,
        host: true // Esto ayuda a que el Dashboard detecte bien el host
    }
})