namespace TaskFlow.Application.DTOs;

public record PagedResult<T>(IReadOnlyList<T> Items, string? NextCursor, bool HasMore);
