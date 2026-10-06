using ST.DotNetSolutionKit.Samples.Common.Domain.Messaging;

namespace ST.DotNetSolutionKit.Samples.Common.Contracts.Messaging.Notifications;

/// <summary>
/// Asks the service that owns notifications to send an email. Any service sends it; the owner, the one
/// generated with <c>--Notify email</c>, consumes it and sends through its transport, sandbox included.
/// </summary>
/// <remarks>
/// The message goes out as it is: a subject and plain text.
/// An attachment travels as a key in object storage, not as bytes in the message: the sender puts the file
/// there first. When the file cannot be read, the email goes out without it.
/// </remarks>
public sealed record SendEmailCommandV1 : IBusCommand
{
    public required Guid Id { get; init; }
    public required DateTimeOffset OccurredOnUtc { get; init; }

    public required string ToEmail { get; init; }
    public required string Subject { get; init; }

    /// <summary>Plain text.</summary>
    public required string Body { get; init; }

    /// <summary>The key of the attachment in object storage; null for an email without one.</summary>
    public string? AttachmentKey { get; init; }

    /// <summary>The file name the recipient sees; required with <see cref="AttachmentKey"/>.</summary>
    public string? AttachmentName { get; init; }
}
