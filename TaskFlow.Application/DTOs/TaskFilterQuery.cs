using TaskFlow.Application.Domain.Enums;

namespace TaskFlow.Application.DTOs;

public record TaskFilterQuery(
    string?                      Search,
    string?                      AssigneeId,
    IReadOnlyList<TaskPriority>? Priorities);
