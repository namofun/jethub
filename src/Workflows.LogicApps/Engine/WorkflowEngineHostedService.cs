namespace Xylab.Workflows.LogicApps.Engine;

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Workflows.Common.ErrorResponses;
using Microsoft.Extensions.Hosting;

public sealed class WorkflowEngineHostedService : IHostedService
{
    private readonly WorkflowEngineProvider _workflowEngineProvider;

    public WorkflowEngineHostedService(WorkflowEngineProvider workflowEngineProvider)
    {
        _workflowEngineProvider = workflowEngineProvider;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _ = StartAsyncCore();
        return Task.CompletedTask;
    }

    private async Task StartAsyncCore()
    {
        await Task.Yield();
        try
        {
            WorkflowEngine engine = await _workflowEngineProvider.CreateEngineAsync();
            await engine.JobsDispatcher.StartAsync();
            await engine.JobsDispatcher.ProvisionSystemJobsIfEnabledAsync();
            _workflowEngineProvider.SetEngine(engine);
        }
        catch (Exception ex)
        {
            _workflowEngineProvider.SetError(
                new ErrorResponseMessageException(
                    System.Net.HttpStatusCode.InternalServerError,
                    ErrorResponseCode.InternalServerError,
                    "Workflow engine failed to be initialized.",
                    innerException: ex));
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _workflowEngineProvider.GetInstanceOrCancel()?.JobsDispatcher.Stop();
        return Task.CompletedTask;
    }
}
