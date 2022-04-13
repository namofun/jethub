using Microsoft.Extensions.Hosting;
using System.Threading;
using System.Threading.Tasks;

namespace Xylab.Workflows.LogicApps.Engine
{
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
            engine.JobsDispatcher.Start();
            engine.JobsDispatcher.ProvisionSystemJobs();
            _workflowEngineProvider.SetEngine(engine);
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _workflowEngineProvider.GetInstanceOrCancel()?.JobsDispatcher.Stop();
            return Task.CompletedTask;
        }
    }
}
