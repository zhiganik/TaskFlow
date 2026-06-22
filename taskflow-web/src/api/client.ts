import axios from 'axios'

// In Docker: VITE_API_URL is "" — Nginx proxies /api/* to the API container.
// In local dev (non-Docker): Vite's dev server proxy forwards /api to the API.
const BASE_URL = import.meta.env.VITE_API_URL ?? ''

export const apiClient = axios.create({
  baseURL: `${BASE_URL}/api/v1`,
  headers: { 'Content-Type': 'application/json' },
})
