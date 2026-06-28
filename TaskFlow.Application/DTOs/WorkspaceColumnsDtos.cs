namespace TaskFlow.Application.DTOs;

public record WorkspaceColumnDto(Guid Id, Guid WorkspaceId, string Name, string Color, int Order, bool IsDoneColumn, DateTime CreatedAt);
public record CreateColumnRequest(string Name, string? Color = null);
public record UpdateColumnRequest(string Name, string? Color = null, bool? IsDoneColumn = null);
public record ReorderColumnsRequest(IReadOnlyList<Guid> ColumnIds);
