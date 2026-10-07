using MassTransit;
using MassTransit.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using ST.DotNetSolutionKit.Samples.Capabilities.Notifications;
using ST.DotNetSolutionKit.Samples.Common.Contracts.Messaging.Notifications;
using ST.DotNetSolutionKit.Samples.Notifications.Infrastructure.Messaging.Consumers;

namespace ST.DotNetSolutionKit.Samples.Notifications.Tests.Tests;

/// <summary>
/// The email another service asks for by a bus command goes out through this service's port as it came.
/// </summary>
public sealed class SendEmailCommandV1ConsumerTests
{
    private readonly Mock<INotificationEmailSender> _email = new();

    private SendEmailCommandV1Consumer Consumer() => new(
        _email.Object,
        NullLogger<SendEmailCommandV1Consumer>.Instance);

    private static ConsumeContext<SendEmailCommandV1> Context(SendEmailCommandV1 command) =>
        Mock.Of<ConsumeContext<SendEmailCommandV1>>(c => c.Message == command && c.CancellationToken == CancellationToken.None);

    private static SendEmailCommandV1 Command() => new()
    {
        Id = Guid.NewGuid(),
        OccurredOnUtc = DateTimeOffset.UtcNow,
        ToEmail = "customer@example.test",
        Subject = "Your code",
        Body = "123456",
    };

    [Test(Description = "A command sent on the bus reaches its consumer, which sends the email")]
    public async Task Should_ConsumeTheCommand_SentOnTheBus()
    {
        await using var provider = new ServiceCollection()
            .AddSingleton(_email.Object)
            .AddSingleton(typeof(ILogger<>), typeof(NullLogger<>))
            .AddMassTransitTestHarness(bus =>
            {
                // a command's queue has no service prefix: AddMessaging names it in kebab case, as here
                bus.SetKebabCaseEndpointNameFormatter();
                bus.AddConsumer<SendEmailCommandV1Consumer>();
            })
            .BuildServiceProvider(true);
        var harness = provider.GetRequiredService<ITestHarness>();
        await harness.Start();

        // the address a sender's IMessageBus.SendAsync resolves for the command
        var queue = new Uri($"queue:{new KebabCaseEndpointNameFormatter(false).Message<SendEmailCommandV1>()}");
        await (await harness.Bus.GetSendEndpoint(queue)).Send(Command());

        (await harness.GetConsumerHarness<SendEmailCommandV1Consumer>().Consumed.Any<SendEmailCommandV1>()).ShouldBeTrue();
        _email.Verify(e => e.SendEmailAsync("customer@example.test", "Your code", "123456", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test(Description = "A command without an attachment goes out as the email it describes")]
    public async Task Should_SendTheEmail()
    {
        await Consumer().Consume(Context(Command()));

        _email.Verify(e => e.SendEmailAsync("customer@example.test", "Your code", "123456", CancellationToken.None), Times.Once);
        _email.VerifyNoOtherCalls();
    }
}
