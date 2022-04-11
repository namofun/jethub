using Microsoft.Azure.Workflows.Data.Entities;
using Microsoft.WindowsAzure.ResourceStack.Common.Json;
using System;

namespace Xylab.Workflows.LogicApps.Engine
{
    public class WorkflowEngineOptions
    {
        public Uri? EndpointUri { get; set; }

        public string? AppDirectoryPath { get; set; }

        public string? AzureStorageAccountConnectionString { get; set; }

        public EdgeConnectionsDetails? Connections { get; set; }

        public void InitializeConnectionsFromJson(string json)
        {
            Connections = json.FromJson<EdgeConnectionsDetails>();
        }
    }
}
