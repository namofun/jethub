using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Workflows.Data.Definitions;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Primitives;
using Newtonsoft.Json;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Xylab.Workflows.LogicApps.Engine;

namespace JetHub.Controllers
{
    public class WorkflowsController : ControllerBase
    {
        [Route("[controller]/{name}")]
        public async Task Invoke(
            [FromRoute] string name,
            [FromServices] IWebHostEnvironment environment)
        {
            IFileInfo file = environment.ContentRootFileProvider.GetFileInfo("Workflows/" + name + ".json");
            if (!file.Exists)
            {
                Response.StatusCode = 404;
                return;
            }

            string workflowDefinition = await file.ReadAsync();

            using HttpRequestMessage req = new();
            req.RequestUri = new System.Uri($"http://localhost{Request.Path}{Request.QueryString}");
            req.Method = new HttpMethod(Request.Method);

            if (Request.ContentLength.HasValue)
            {
                MemoryStream stream = new();
                await Request.Body.CopyToAsync(stream);
                req.Content = new StreamContent(stream);
            }

            foreach (var header in Request.Headers)
            {
                if (header.Key.Contains(':')) continue;
                if (header.Key.StartsWith("content-", System.StringComparison.OrdinalIgnoreCase))
                {
                    req.Content.Headers.Add(header.Key, header.Value.ToArray());
                }
                else
                {
                    req.Headers.Add(header.Key, header.Value.ToArray());
                }
            }

            var config = EdgeFlowConfigurationSource.CreateDefault(new System.Uri("http://localhost"), environment.ContentRootPath);
            config.SetAzureStorageAccountCredentials("UseDevelopmentStorage=true");
            WorkflowEngine engine = await WorkflowEngine.CreateEngine(config);
            await engine.ValidateAndCreateFlow(name, JsonConvert.DeserializeObject<FlowPropertiesDefinition>(workflowDefinition));
            using HttpResponseMessage resp = await engine.InvokeFlow(engine.FlowDefinitions[name], req, User);
            Response.StatusCode = (int)resp.StatusCode;
            foreach (var header in resp.Headers.Concat(resp.Content.Headers))
            {
                Response.Headers[header.Key] = new StringValues(header.Value.ToArray());
            }

            using var respStream = await resp.Content.ReadAsStreamAsync();
            await respStream.CopyToAsync(Response.Body);
        }

        [Route("[action]/{**slug}")]
        public async Task<IActionResult> PingPong()
        {
            string body = null;
            if (Request.ContentLength.HasValue)
            {
                MemoryStream stream = new();
                await Request.Body.CopyToAsync(stream);
                body = Encoding.UTF8.GetString(stream.ToArray());
            }

            return new JsonResult(new
            {
                headers = Request.Headers.ToDictionary(k => k.Key, v => v.Value.Count == 1 ? v.Value.Single() : (object)v.Value.ToArray()),
                body,
                method = Request.Method,
                url = $"http://localhost{Request.Path}{Request.QueryString}",
            });
        }
    }
}
