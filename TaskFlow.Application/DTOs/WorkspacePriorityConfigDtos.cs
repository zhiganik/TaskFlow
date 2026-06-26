using TaskFlow.Application.Domain.Enums;

namespace TaskFlow.Application.DTOs;

public record PriorityConfigDto(TaskPriority Priority, string DisplayName, string Color);
public record UpdatePriorityConfigRequest(string DisplayName, string Color);
