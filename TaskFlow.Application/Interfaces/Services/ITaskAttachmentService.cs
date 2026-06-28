using TaskFlow.Application.DTOs;

namespace TaskFlow.Application.Interfaces.Services;

public interface ITaskAttachmentService
{
    Task<AttachmentDto>                GetByIdAsync(Guid workspaceId, Guid taskId, Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<AttachmentDto>> GetByTaskIdAsync(Guid workspaceId, Guid taskId, CancellationToken ct = default);

    Task<AttachmentDto> UploadAsync(
        Guid workspaceId, Guid taskId, string uploadedById,
        UploadFileRequest file,
        CancellationToken ct = default);

    Task<(Stream Stream, string FileName, string ContentType)> DownloadAsync(
        Guid workspaceId, Guid taskId, Guid id, CancellationToken ct = default);

    Task DeleteAsync(Guid workspaceId, Guid taskId, Guid id, CancellationToken ct = default);
}
