const KEY = 'taskflow.lastWorkspaceId'

// Remembers the last workspace visited so "/" can redirect there instead of
// always landing on whichever workspace happens to be first in the list.
export const getLastWorkspaceId = () => localStorage.getItem(KEY)
export const setLastWorkspaceId = (id: string) => localStorage.setItem(KEY, id)
