# Workspace Settings

A dedicated settings page for workspace configuration, accessible at `/workspaces/:workspaceId/settings`.

## Access

- **URL:** `/workspaces/:workspaceId/settings`
- **Sidebar link:** "Settings" (visible to all workspace members)
- **Read access:** All workspace members
- **Write access:** Admin and Owner roles only

## Sections

### Labels

Full CRUD management of workspace-scoped labels. See [`labels.md`](./labels.md) for the data model and API.

- Create labels with a name and color (chosen from the shared color palette)
- Inline edit of name and color
- Delete with confirmation (cascades off all tasks)
- Live preview of label chip appearance before saving

### Priority Display

Customize how each of the three priority levels (Low / Medium / High) is displayed:

- Change the **display name** (e.g., rename "High" to "Critical")
- Change the **color** (chosen from the shared color palette)
- Changes are reflected immediately on the Kanban board (task card left border), filter bar chips, and task detail panel badge

Priority values are still stored as the enum (`Low` / `Medium` / `High`) — only the display layer changes. Existing tasks do not need to be updated.

## Color Palette

Both labels and priority configs share the same 10-color palette defined in `src/lib/priority.ts`:

`#6366F1`, `#F59E0B`, `#10B981`, `#EF4444`, `#3B82F6`, `#8B5CF6`, `#F97316`, `#06B6D4`, `#84CC16`, `#EC4899`
