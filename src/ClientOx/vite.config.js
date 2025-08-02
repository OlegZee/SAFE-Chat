import { defineConfig } from 'vite'

const port = process.env.SUAVE_FABLE_PORT || "8083";

export default defineConfig({
  root: "./",
  publicDir: "./static",
  server: {
    port: 8080,
    hmr: {
      port: 24678, // Use a specific port for HMR to avoid conflicts
    },
    watch: {
        ignored: [
            "**/*.fs" // Don't watch F# files
        ]
    },
    proxy: {
      "/api/socket": {
        target: `ws://localhost:${port}`,
        ws: true,
      },
      "/api": {
        target: `http://localhost:${port}`,
        changeOrigin: true,
      },
      "/logon": {
        target: `http://localhost:${port}`,
        changeOrigin: true,
      },
      "/logoff": {
        target: `http://localhost:${port}`,
        changeOrigin: true,
      },
      "/logonfast": {
        target: `http://localhost:${port}`,
        changeOrigin: true,
      },
    },
  },
  build: {
    outDir: "./public",
    emptyOutDir: false, // Keep static files like images
  },
  css: {
    preprocessorOptions: {
      scss: {
        api: 'modern-compiler' // Fix Sass deprecation warning
      }
    }
  },
  optimizeDeps: {
    include: ['react', 'react-dom']
  },
  resolve: {
    extensions: ['.js', '.ts', '.jsx', '.tsx', '.json']
  }
})