using Microsoft.Extensions.Logging;
using TaskFlow.Application.Domain.Entities;
using TaskFlow.Application.Domain.Enums;
using TaskFlow.Application.DTOs;
using TaskFlow.Application.Interfaces.Repositories;
using TaskFlow.Application.Interfaces.Services;

namespace TaskFlow.Application.Services;

public class DemoWorkspaceSeeder(
    IWorkspacesService workspacesService,
    IWorkspaceColumnsRepository columnsRepository,
    IWorkspaceLabelsRepository labelsRepository,
    IWorkspaceTasksRepository tasksRepository,
    ILogger<DemoWorkspaceSeeder> logger) : IDemoWorkspaceSeeder
{
    public async Task SeedAsync(string userId, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;

        // ── 1. Create workspace ───────────────────────────────────────────────────
        // auto-creates: owner member, columns (Todo/In Progress/Done), labels (Bug/Frontend/Backend), priority configs
        var workspace = await workspacesService.CreateAsync(
            userId, new CreateWorkspaceRequest("My First Workspace"), ct);
        var wsId = workspace.Id;

        // ── 2. Reorder default columns and add Backlog + Review ──────────────────
        var existing = await columnsRepository.GetByWorkspaceIdAsync(wsId, ct);
        var todoCol  = existing.First(c => c.Name == "Todo");
        var ipCol    = existing.First(c => c.Name == "In Progress");
        var doneCol  = existing.First(c => c.Name == "Done");

        todoCol.Order = 1;
        ipCol.Order   = 2;
        doneCol.Order = 4;
        await columnsRepository.UpdateRangeAsync([todoCol, ipCol, doneCol], ct);

        var backlog = await columnsRepository.AddAsync(new WorkspaceColumn
        {
            WorkspaceId = wsId, Name = "Backlog", Color = "#6b7280", Order = 0, CreatedAt = now,
        }, ct);

        var review = await columnsRepository.AddAsync(new WorkspaceColumn
        {
            WorkspaceId = wsId, Name = "Review", Color = "#8b5cf6", Order = 3, CreatedAt = now,
        }, ct);

        // ── 3. Add Improvement + Documentation labels ────────────────────────────
        var existingLabels = await labelsRepository.GetByWorkspaceIdAsync(wsId, ct);
        var bugLabel       = existingLabels.First(l => l.Name == "Bug");
        var frontendLabel  = existingLabels.First(l => l.Name == "Frontend");
        var backendLabel   = existingLabels.First(l => l.Name == "Backend");

        var improvementLabel = await labelsRepository.AddAsync(new WorkspaceLabel
        {
            WorkspaceId = wsId, Name = "Improvement", Color = "#8b5cf6",
        }, ct);
        var docsLabel = await labelsRepository.AddAsync(new WorkspaceLabel
        {
            WorkspaceId = wsId, Name = "Documentation", Color = "#6b7280",
        }, ct);

        // ── 4. Create tasks ───────────────────────────────────────────────────────
        var taskNumber     = 1;
        var orderPerColumn = new Dictionary<Guid, int>();

        int NextOrder(Guid colId)
        {
            orderPerColumn.TryGetValue(colId, out var o);
            orderPerColumn[colId] = o + 1;
            return o;
        }

        async Task<WorkspaceTask> Add(
            string title, string description, Guid columnId,
            TaskPriority priority, string? assigneeId, DateTime? dueDate,
            WorkspaceTaskStatus status, DateTime? completedAt, DateTime? closedAt)
        {
            return await tasksRepository.AddAsync(new WorkspaceTask
            {
                WorkspaceId = wsId,
                ColumnId    = columnId,
                Number      = taskNumber++,
                Title       = title,
                Description = description,
                Order       = NextOrder(columnId),
                Priority    = priority,
                AssigneeId  = assigneeId,
                DueDate     = dueDate,
                CreatedById = userId,
                Status      = status,
                CompletedAt = completedAt,
                ClosedAt    = closedAt,
                CreatedAt   = now,
                UpdatedAt   = now,
            }, ct);
        }

        // Backlog
        var t1 = await Add(
            "Research competitor tools",
            "Investigate similar task management tools in the market.\n\n" +
            "- Compare Jira, Linear, Notion, and Trello\n" +
            "- Identify feature gaps and opportunities\n" +
            "- Summarise findings in a shared document",
            backlog.Id, TaskPriority.Low, null, null, WorkspaceTaskStatus.Active, null, null);
        await tasksRepository.AddLabelsAsync(t1.Id, [docsLabel.Id], ct);

        var t2 = await Add(
            "Set up CI/CD pipeline",
            "Configure automated testing and deployment for the project.\n\n" +
            "- Set up GitHub Actions workflow\n" +
            "- Add test, build, and deploy stages\n" +
            "- Integrate with Railway for auto-deploys on the main branch",
            backlog.Id, TaskPriority.Medium, null, null, WorkspaceTaskStatus.Active, null, null);
        await tasksRepository.AddLabelsAsync(t2.Id, [backendLabel.Id], ct);

        // Todo
        var t3 = await Add(
            "Fix authentication bug",
            "Users report being unexpectedly logged out after a few minutes.\n\n" +
            "- Reproduce with a fresh account\n" +
            "- Investigate JWT expiry and refresh token rotation\n" +
            "- Add a regression test covering the silent refresh flow",
            todoCol.Id, TaskPriority.High, userId, now.AddDays(-1), WorkspaceTaskStatus.Active, null, null);
        await tasksRepository.AddLabelsAsync(t3.Id, [bugLabel.Id], ct);

        var t4 = await Add(
            "Write API documentation",
            "Document all REST endpoints using Swagger XML annotations.\n\n" +
            "- Add summary comments to every controller action\n" +
            "- Verify request/response schemas in Swagger UI\n" +
            "- Export the OpenAPI spec and commit it to `/docs`",
            todoCol.Id, TaskPriority.Low, null, null, WorkspaceTaskStatus.Active, null, null);
        await tasksRepository.AddLabelsAsync(t4.Id, [docsLabel.Id], ct);

        // In Progress
        var t5 = await Add(
            "Implement task board",
            "Build the Kanban board with column-based task layout.\n\n" +
            "- Render tasks grouped by column with cursor pagination\n" +
            "- Wire up task creation and move-to-column mutations\n" +
            "- Show priority left-border colour and label chips on cards",
            ipCol.Id, TaskPriority.High, userId, now.AddDays(1), WorkspaceTaskStatus.Active, null, null);
        await tasksRepository.AddLabelsAsync(t5.Id, [frontendLabel.Id], ct);

        var t6 = await Add(
            "Add dark mode support",
            "Implement a light/dark theme toggle using Tailwind CSS.\n\n" +
            "- Use `darkMode: 'class'` strategy and a Zustand toggle store\n" +
            "- Persist the preference in `localStorage`\n" +
            "- Audit all components to ensure consistent token usage",
            ipCol.Id, TaskPriority.Medium, null, null, WorkspaceTaskStatus.Active, null, null);
        await tasksRepository.AddLabelsAsync(t6.Id, [improvementLabel.Id], ct);

        // Review
        var t7 = await Add(
            "Code review: auth module",
            "Review the JWT authentication and refresh token implementation.\n\n" +
            "- Verify signing algorithm and token validation parameters\n" +
            "- Check for token leakage in logs or error responses\n" +
            "- Approve or request changes with inline comments",
            review.Id, TaskPriority.Medium, null, null, WorkspaceTaskStatus.Active, null, null);
        await tasksRepository.AddLabelsAsync(t7.Id, [backendLabel.Id], ct);

        var t8 = await Add(
            "Deploy to staging",
            "Prepare and validate the staging environment before the production release.\n\n" +
            "- Run database migrations against the staging PostgreSQL instance\n" +
            "- Smoke-test register, create workspace, and add task flows\n" +
            "- Confirm real-time notifications fire correctly on staging",
            review.Id, TaskPriority.High, userId, now.AddDays(3), WorkspaceTaskStatus.Active, null, null);

        // Done (visible on board, green badge)
        await Add(
            "Project kickoff",
            "Initial team sync to align on project goals and milestones.\n\n" +
            "All stakeholders attended. Tech stack and delivery timeline agreed.",
            doneCol.Id, TaskPriority.Low, null, null,
            WorkspaceTaskStatus.Done, now.AddDays(-14), null);

        // Closed — invisible on board, visible in Archive
        var t10 = await Add(
            "Define project scope",
            "Document the project requirements and delivery boundaries.\n\n" +
            "Completed during the discovery phase and archived after stakeholder sign-off.",
            backlog.Id, TaskPriority.Low, null, null,
            WorkspaceTaskStatus.Closed, now.AddDays(-21), now.AddDays(-7));
        await tasksRepository.AddLabelsAsync(t10.Id, [docsLabel.Id], ct);

        logger.LogInformation(
            "Demo workspace {WorkspaceId} seeded for user {UserId} ({TaskCount} tasks)",
            wsId, userId, taskNumber - 1);
    }
}
