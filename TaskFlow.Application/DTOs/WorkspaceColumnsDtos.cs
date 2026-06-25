namespace TaskFlow.Application.DTOs;

public record WorkspaceColumnDto(Guid Id, Guid WorkspaceId, string Name, int Order, DateTime CreatedAt);
public record CreateColumnRequest(string Name);
public record UpdateColumnRequest(string Name);
public record ReorderColumnsRequest(IReadOnlyList<Guid> ColumnIds);
