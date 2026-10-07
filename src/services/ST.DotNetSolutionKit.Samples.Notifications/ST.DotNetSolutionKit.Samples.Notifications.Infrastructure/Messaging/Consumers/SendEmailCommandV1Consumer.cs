using Microsoft.Extensions.Logging;
using ST.DotNetSolutionKit.Samples.Common.Application.Messaging.Consumers;
using ST.DotNetSolutionKit.Samples.Capabilities.Notifications;
using ST.DotNetSolutionKit.Samples.Common.Contracts.Messaging.Notifications;
using ST.DotNetSolutionKit.Samples.Common.Infrastructure.Messaging.Consumers;

namespace ST.DotNetSolutionKit.Samples.Notifications.Infrastructure.Messaging.Consumers;

/// <summary>
/// Sends the email another service asked for, through the transport of this service's settings, so the
/// sandbox applies to it as to any other message.
/// </summary>
public sealed class SendEmailCommandV1Consumer(
    INotificationEmailSender emailSender,
    ILogger<SendEmailCommandV1Consumer> logger)
    : BusCommandConsumer<SendEmailCommandV1>(logger)
{
    protected override async Task HandleAsync(IMessageContext<SendEmailCommandV1> context)
    {
        var command = context.Message;

        await emailSender.SendEmailAsync(command.ToEmail, command.Subject, command.Body, context.CancellationToken);
    }
}
