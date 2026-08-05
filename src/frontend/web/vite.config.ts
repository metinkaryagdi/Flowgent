import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

// Inside the docker-compose dev container the source tree is a bind mount from the
// Windows host, and inotify events do not cross that boundary. Without polling, Vite's
// watcher never fires: the dev server keeps serving the modules it transformed at
// startup, so edits appear to have no effect until the container is restarted -- which
// silently cost a full E2E run once. Polling is opt-in because it is pure overhead when
// running `npm run dev` directly on the host, where inotify works.
const usePolling = process.env.VITE_USE_POLLING === 'true'

// https://vite.dev/config/
export default defineConfig({
  plugins: [react()],
  server: {
    watch: usePolling ? { usePolling: true, interval: 400 } : undefined,
  },
})
