namespace Xylab.Management.Controllers;

using Microsoft.AspNetCore.Mvc;
using Xylab.Workflows.LogicApps.Engine;
using Xylab.Workflows.LogicApps.Mvc;

[Route("workflows")]
public class WorkflowsController(WorkflowEngineProvider workflowEngineProvider)
    : WorkflowsControllerBase(workflowEngineProvider)
{
}
