namespace TaskFlow.Application.DTOs;

public record WorkspaceColumnDto(Guid Id, Guid WorkspaceId, string Name, string Color, int Order, DateTime CreatedAt);
public record CreateColumnRequest(string Name, string? Color = null);
public record UpdateColumnRequest(string Name, string? Color = null);
public record ReorderColumnsRequest(IReadOnlyList<Guid> ColumnIds);
