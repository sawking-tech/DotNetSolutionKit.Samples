using ST.DotNetSolutionKit.Samples.Common.Application.Messaging.Consumers;
using ST.DotNetSolutionKit.Samples.Common.Domain.Messaging;
using MassTransit;

namespace ST.DotNetSolutionKit.Samples.Common.Infrastructure.Messaging.Consumers;

/// <summary>
/// Adapts MassTransit's <see cref="ConsumeContext{T}"/> to the platform's
/// <see cref="IMessageContext{T}"/> abstraction, decoupling consumer handlers
/// from the MassTransit transport layer.
/// </summary>
/// <typeparam name="T">The message type being consumed.</typeparam>
internal sealed class MassTransitMessageContext<T>(ConsumeContext<T> context)
    : IMessageContext<T>
    where T : class, IBusMessage
{
    /// <inheritdoc />
    public T Message => context.Message;

    /// <inheritdoc />
    public Guid? MessageId => context.MessageId;

    /// <inheritdoc />
    public Guid? CorrelationId => context.CorrelationId;

    /// <inheritdoc />
    public CancellationToken CancellationToken => context.CancellationToken;
}