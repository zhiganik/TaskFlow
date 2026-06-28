using MassTransit;
using TaskFlow.Application.Interfaces.Services;

namespace TaskFlow.Infrastructure.Messaging;

public class MassTransitPublisher(IPublishEndpoint publishEndpoint) : IMessagePublisher
{
    public Task PublishAsync<T>(T message, CancellationToken ct = default) where T : class
        => publishEndpoint.Publish(message, ct);
}
