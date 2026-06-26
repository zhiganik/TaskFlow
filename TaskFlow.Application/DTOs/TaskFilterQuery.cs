using TaskFlow.Application.Domain.Enums;

namespace TaskFlow.Application.DTOs;

public record TaskFilterQuery(
    string?                      Search,
    IReadOnlyList<string>?       AssigneeIds,
    IReadOnlyList<TaskPriority>? Priorities,
    IReadOnlyList<Guid>?         LabelIds = null);
