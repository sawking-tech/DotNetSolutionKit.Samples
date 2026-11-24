using MailKit.Net.Smtp;

namespace ST.DotNetSolutionKit.Samples.Capabilities.Notifications.Factories;

internal interface ISmtpClientFactory
{
    ISmtpClient Create();
}

internal sealed class SmtpClientFactory : ISmtpClientFactory
{
    public ISmtpClient Create() => new SmtpClient();
}
