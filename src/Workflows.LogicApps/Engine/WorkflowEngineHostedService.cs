namespace Xylab.Workflows.LogicApps.Engine;

using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

public sealed class WorkflowEngineHostedService : IHostedService
{
    private readonly WorkflowEngineProvider _workflowEngineProvider;

    public WorkflowEngineHostedService(WorkflowEngineProvider workflowEngineProvider)
    {
        _workflowEngineProvider = workflowEngineProvider;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        WorkflowEngine engine = await _workflowEngineProvider.CreateEngineAsync();
        await engine.JobsDispatcher.StartAsync();
        await engine.JobsDispatcher.ProvisionSystemJobsIfEnabledAsync();
        _workflowEngineProvider.SetEngine(engine);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _workflowEngineProvider.GetInstanceOrCancel()?.JobsDispatcher.Stop();
        return Task.CompletedTask;
    }
}
