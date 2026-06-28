using MassTransit;
using TaskFlow.Contracts.Messages;

namespace TaskFlow.FileLoader.Worker.Consumers;

public class FileUploadConsumer(ILogger<FileUploadConsumer> logger) : IConsumer<FileUploadMessage>
{
    public Task Consume(ConsumeContext<FileUploadMessage> context)
    {
        logger.LogInformation(
            "Received file upload job: FileId={FileId} FileName={FileName} TaskId={TaskId}",
            context.Message.FileId,
            context.Message.FileName,
            context.Message.TaskId);

        // File processing logic will be implemented here

        return Task.CompletedTask;
    }
}
