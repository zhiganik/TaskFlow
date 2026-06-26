namespace TaskFlow.Application.DTOs;

public record LabelDto(Guid Id, string Name, string Color);
public record CreateLabelRequest(string Name, string Color);
public record UpdateLabelRequest(string Name, string Color);
public record SetTaskLabelsRequest(IReadOnlyList<Guid> LabelIds);
