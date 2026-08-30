namespace Xylab.Workflows.LogicApps.Engine;

using System;
using System.ComponentModel.DataAnnotations;

public class WorkflowEngineOptions
{
    public Uri? EndpointUri { get; set; }

    public string? AppDirectoryPath { get; set; }

    [Required]
    public string? AzureStorageAccountConnectionString { get; set; }
}
