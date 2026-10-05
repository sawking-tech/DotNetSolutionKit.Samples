using ST.DotNetSolutionKit.Samples.Common.Application.Notifications;
using ST.DotNetSolutionKit.Samples.Common.Infrastructure.Notifications.Adapters;

namespace ST.DotNetSolutionKit.Samples.Common.Infrastructure.Notifications.Factories;

internal interface IGraphClientFactory
{
    IGraphClientAdapter Create();
}

internal sealed class GraphClientFactory : IGraphClientFactory
{
    private readonly INotificationEmailSettings _settings;

    public GraphClientFactory(INotificationEmailSettings settings)
    {
        _settings = settings;
    }

    public IGraphClientAdapter Create()
    {
        var graph = _settings.GraphApi ?? throw new InvalidOperationException("GraphApi configuration is missing.");
        return new GraphClientAdapter(graph.TenantId, graph.ClientId, graph.ClientSecret);
    }
}
