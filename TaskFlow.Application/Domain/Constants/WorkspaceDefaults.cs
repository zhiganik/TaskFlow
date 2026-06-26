using TaskFlow.Application.Domain.Entities;
using TaskFlow.Application.Domain.Enums;

namespace TaskFlow.Application.Domain.Constants;

public static class WorkspaceDefaults
{
    public static IReadOnlyList<WorkspaceColumn> Columns =>
    [
        new() { Name = "Todo",        Color = "#6366F1", Order = 0 },
        new() { Name = "In Progress", Color = "#F59E0B", Order = 1 },
        new() { Name = "Done",        Color = "#10B981", Order = 2 },
    ];

    public static IReadOnlyList<WorkspacePriorityConfig> PriorityConfigs =>
    [
        new() { Priority = TaskPriority.Low,    DisplayName = "Low",    Color = "#22c55e" },
        new() { Priority = TaskPriority.Medium, DisplayName = "Medium", Color = "#f59e0b" },
        new() { Priority = TaskPriority.High,   DisplayName = "High",   Color = "#ef4444" },
    ];

    public static IReadOnlyList<WorkspaceLabel> Labels =>
    [
        new() { Name = "Bug",      Color = "#ef4444" },
        new() { Name = "Frontend", Color = "#60a5fa" },
        new() { Name = "Backend",  Color = "#a78bfa" },
    ];
}
