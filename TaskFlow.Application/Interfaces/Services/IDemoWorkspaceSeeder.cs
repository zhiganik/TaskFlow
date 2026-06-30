namespace TaskFlow.Application.Interfaces.Services;

public interface IDemoWorkspaceSeeder
{
    Task SeedAsync(string userId, CancellationToken ct = default);
}
