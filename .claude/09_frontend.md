# 09 — Frontend: React + Vite + TypeScript

## What the Frontend Is For

The React app (`taskflow-web/`) is a testing and demo interface for the TaskFlow API. It is not a full-featured product UI — it covers enough to validate every API route: auth, workspaces, projects, tasks, file upload with status polling. Scope is kept tight deliberately.

---

## Tech Choices

```
Framework     React 19
Language      TypeScript (strict mode)
Build tool    Vite 6
Routing       React Router v7
State         React Query (TanStack Query v5) — server state only
HTTP          Axios (wrapped in src/api/ — never used directly in components)
Styling       Tailwind CSS v4
Forms         React Hook Form + Zod (mirrors FluentValidation on the backend)
```

---

## Project Structure

```
taskflow-web/
├── public/
├── src/
│   ├── api/                    ← ALL HTTP calls live here
│   │   ├── client.ts           ← Axios instance, JWT interceptor, error handling
│   │   ├── auth.api.ts
│   │   ├── workspaces.api.ts
│   │   ├── projects.api.ts
│   │   ├── tasks.api.ts
│   │   └── attachments.api.ts
│   │
│   ├── hooks/                  ← React Query hooks wrapping api/ calls
│   │   ├── useAuth.ts
│   │   ├── useWorkspaces.ts
│   │   ├── useTasks.ts
│   │   └── useAttachments.ts
│   │
│   ├── components/             ← Reusable UI components (no API calls)
│   │   ├── ui/                 ← Generic: Button, TextField, Alert, Spinner, Modal, Icons
│   │   ├── workspaces/         ← WorkspaceSwitcher, WorkspaceFormModal, DeleteWorkspaceDialog
│   │   ├── tasks/              ← TaskCard, TaskStatusBadge, AttachmentList
│   │   └── layout/             ← Sidebar, Header, PageWrapper
│   │
│   ├── pages/                  ← Route-level components (one per route)
│   │   ├── LoginPage.tsx
│   │   ├── RegisterPage.tsx
│   │   ├── WorkspaceRedirectPage.tsx  ← "/" → redirects into /workspaces/:workspaceId
│   │   ├── DashboardPage.tsx          ← Sidebar + workspace-scoped content
│   │   ├── ProjectsPage.tsx
│   │   ├── TasksPage.tsx
│   │   └── TaskDetailPage.tsx  ← includes file upload + status polling
│   │
│   ├── store/                  ← Auth state, persisted to localStorage via zustand persist
│   │   └── authStore.ts
│   │
│   ├── validation/              ← Zod schemas mirroring FluentValidation rules
│   │   ├── auth.schema.ts
│   │   └── workspace.schema.ts
│   │
│   ├── lib/                    ← Small framework-free helpers (no API calls, no React)
│   │   └── lastWorkspace.ts    ← last-visited workspace id, for the "/" redirect only
│   │
│   ├── types/                  ← TypeScript types mirroring backend DTOs
│   │   └── api.types.ts
│   │
│   ├── router.tsx              ← React Router routes + protected route wrapper
│   └── main.tsx
│
├── .env.local.example          ← committed template (no real values)
├── .env.local                  ← never committed
├── vite.config.ts
├── tsconfig.json
└── package.json
```

---

## API Client Pattern

All HTTP calls go through one Axios instance in `src/api/client.ts`. Components never call `fetch` or `axios` directly.

```typescript
// src/api/client.ts
import axios from 'axios'

// In Docker: VITE_API_URL is "" — Nginx proxies /api/* to the API container
// In local dev (non-Docker): VITE_API_URL = "http://localhost:5000"
const BASE_URL = import.meta.env.VITE_API_URL ?? ''

export const apiClient = axios.create({
  baseURL: `${BASE_URL}/api/v1`,
  headers: { 'Content-Type': 'application/json' },
})

// Attach JWT from memory on every request
apiClient.interceptors.request.use(config => {
  const token = getTokenFromMemory()   // never localStorage
  if (token) config.headers.Authorization = `Bearer ${token}`
  return config
})

// Translate Axios errors to typed app errors
apiClient.interceptors.response.use(
  res => res,
  err => {
    const status = err.response?.status
    if (status === 401) redirectToLogin()
    return Promise.reject(err.response?.data ?? err)
  }
)
```

```typescript
// src/api/tasks.api.ts
import { apiClient } from './client'
import type { TaskDto, CreateTaskRequest, PagedResult } from '../types/api.types'

export const tasksApi = {
  list: (projectId: string, params?: TaskFilter) =>
    apiClient.get<PagedResult<TaskDto>>(`/projects/${projectId}/tasks`, { params })
      .then(r => r.data),

  getById: (projectId: string, id: string) =>
    apiClient.get<TaskDto>(`/projects/${projectId}/tasks/${id}`)
      .then(r => r.data),

  create: (projectId: string, data: CreateTaskRequest) =>
    apiClient.post<TaskDto>(`/projects/${projectId}/tasks`, data)
      .then(r => r.data),

  patchStatus: (projectId: string, id: string, status: TaskStatus) =>
    apiClient.patch<TaskDto>(`/projects/${projectId}/tasks/${id}/status`, { status })
      .then(r => r.data),
}
```

---

## React Query Hooks

```typescript
// src/hooks/useTasks.ts
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { tasksApi } from '../api/tasks.api'

export const useTasks = (projectId: string, filter?: TaskFilter) =>
  useQuery({
    queryKey: ['tasks', projectId, filter],
    queryFn: () => tasksApi.list(projectId, filter),
  })

export const useCreateTask = (projectId: string) => {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (data: CreateTaskRequest) => tasksApi.create(projectId, data),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['tasks', projectId] }),
  })
}
```

---

## Auth — Persisted via zustand `persist`

Tokens are persisted to `localStorage` through zustand's `persist` middleware, so a page reload
doesn't force a re-login. This is a deliberate tradeoff for this app (not the in-memory-only
default): the access token is short-lived and the refresh token rotates — single-use, invalidated
server-side on every refresh — which bounds how long a value pulled out of storage stays useful.

```typescript
// src/store/authStore.ts
import { create } from 'zustand'
import { persist } from 'zustand/middleware'

interface AuthState {
  accessToken: string | null
  refreshToken: string | null
  expiresAt: string | null
  user: UserDto | null
  setAuth: (tokens: AuthTokens, user: UserDto) => void
  clearAuth: () => void
}

export const useAuthStore = create<AuthState>()(
  persist(
    (set) => ({
      accessToken: null,
      refreshToken: null,
      expiresAt: null,
      user: null,
      setAuth: (tokens, user) => set({ ...tokens, user }),
      clearAuth: () => set({ accessToken: null, refreshToken: null, expiresAt: null, user: null }),
    }),
    { name: 'taskflow.auth' },
  ),
)

// Plain accessors for use outside React (e.g. the Axios interceptor in api/client.ts)
export const getAccessToken = () => useAuthStore.getState().accessToken
export const getRefreshToken = () => useAuthStore.getState().refreshToken
```

On app boot (`App.tsx`), if the persisted access token has already expired, the app silently calls
`POST /auth/refresh` with the persisted refresh token before rendering the router — no login-page
flash, no backend changes (the API stays token-based, no cookies/sessions). If the token is still
valid, nothing is fetched. Mid-session 401s are handled the same way by the existing Axios
response interceptor in `api/client.ts`, which retries the failed request once after refreshing.

---

## File Upload + Status Polling

```typescript
// src/hooks/useAttachments.ts
export const useUploadAttachment = (taskId: string) => {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (file: File) => {
      const form = new FormData()
      form.append('file', file)
      return apiClient.post<AttachmentDto>(
        `/tasks/${taskId}/attachments`,
        form,
        { headers: { 'Content-Type': 'multipart/form-data' } }
      ).then(r => r.data)
    },
    onSuccess: () => qc.invalidateQueries({ queryKey: ['attachments', taskId] }),
  })
}

// Poll every 2 seconds until status is Ready or Failed
export const useAttachment = (taskId: string, attachmentId: string) =>
  useQuery({
    queryKey: ['attachments', taskId, attachmentId],
    queryFn: () => attachmentsApi.getById(taskId, attachmentId),
    refetchInterval: data =>
      data?.status === 'Pending' || data?.status === 'Processing' ? 2000 : false,
  })
```

---

## TypeScript Types (mirror backend DTOs)

```typescript
// src/types/api.types.ts

export interface UserDto {
  userId: string
  email: string
  displayName: string
}

export interface TaskDto {
  id: string
  projectId: string
  title: string
  description: string | null
  priority: TaskPriority
  status: TaskStatus
  dueDate: string | null
  createdAt: string
  updatedAt: string
  assignee: UserDto | null
  attachments: AttachmentDto[]   // populated on single task GET only
}

export interface AttachmentDto {
  id: string
  taskId: string
  originalFileName: string
  contentType: string
  fileSizeBytes: number
  status: AttachmentStatus
  processingError: string | null
  uploadedAt: string
  processedAt: string | null
  uploadedBy: UserDto
}

export type TaskPriority  = 'Low' | 'Medium' | 'High' | 'Critical'
export type TaskStatus    = 'Todo' | 'InProgress' | 'Done' | 'Cancelled'
export type AttachmentStatus = 'Pending' | 'Processing' | 'Ready' | 'Failed'

export interface PagedResult<T> {
  items: T[]
  totalCount: number
  page: number
  pageSize: number
  totalPages: number
}
```

---

## vite.config.ts

```typescript
import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

export default defineConfig({
  plugins: [react()],
  server: {
    port: 3000,
    proxy: {
      // Local dev (non-Docker): proxy /api to the API on port 5000
      // In Docker: Nginx handles this — this config is ignored
      '/api': {
        target: 'http://localhost:5000',
        changeOrigin: true,
      }
    }
  }
})
```

---

## .env.local.example

```bash
# Copy to .env.local and fill in — never commit .env.local
# In Docker: VITE_API_URL is set to "" (Nginx proxies /api/* automatically)
# In local dev without Docker: point directly at the API
VITE_API_URL=http://localhost:5000
```

---

## Frontend Hard Rules

1. **All API calls in `src/api/`** — zero `fetch`/`axios` in components or hooks
2. **JWT persisted via the `authStore` zustand `persist` middleware** — see "Auth" above for the rotation/expiry tradeoffs that make this acceptable
3. **TypeScript strict** — no `any`, enable `strictNullChecks`, `noUncheckedIndexedAccess`
4. **No business logic in components** — components render, hooks fetch, `api/` calls the server
5. **Zod for form validation** — mirrors FluentValidation rules on the backend (same constraints)
6. **`.env.local` never committed** — `.env.local.example` is the committed template
7. **`VITE_API_URL = ""`** in docker-compose — relative paths so Nginx proxy works correctly

---

## Pages in Scope

| Page | Route | What It Tests |
|------|-------|---------------|
| `LoginPage` | `/login` | POST /auth/login, token storage |
| `RegisterPage` | `/register` | POST /auth/register |
| `WorkspaceRedirectPage` | `/` | Redirects to the last-visited (or first) workspace; empty state when none exist |
| `DashboardPage` | `/workspaces/:workspaceId` | Workspace switcher in the `Sidebar` (create/select/rename/delete) — covers GET/POST/PUT/DELETE workspaces |
| `ProjectsPage` | `/workspaces/:id/projects` | GET/POST projects, task summary |
| `TasksPage` | `/projects/:id/tasks` | GET tasks, filter, pagination, status patch |
| `TaskDetailPage` | `/tasks/:id` | Full task detail, file upload, status polling, download |
