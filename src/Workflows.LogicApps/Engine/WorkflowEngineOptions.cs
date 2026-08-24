namespace Xylab.Workflows.LogicApps.Engine;

using System;

public class WorkflowEngineOptions
{
    public Uri? EndpointUri { get; set; }

    public string? AppDirectoryPath { get; set; }

    public string? AzureStorageAccountConnectionString { get; set; }
}
