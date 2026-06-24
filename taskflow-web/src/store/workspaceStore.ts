import { create } from 'zustand'

interface WorkspaceState {
  selectedWorkspaceId: string | null
  selectWorkspace: (id: string | null) => void
}

// UI-only selection state — in-memory like authStore, lost on reload by design.
export const useWorkspaceStore = create<WorkspaceState>((set) => ({
  selectedWorkspaceId: null,
  selectWorkspace: (id) => set({ selectedWorkspaceId: id }),
}))
