namespace Xylab.Workflows.LogicApps.Mvc;

using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

public class NewtonsoftJsonResult : IActionResult
{
    public object Content { get; }

    public int? StatusCode { get; set; }

    public NewtonsoftJsonResult(object content)
    {
        Content = content;
    }

    public async Task ExecuteResultAsync(ActionContext context)
    {
        string json = JsonConvert.SerializeObject(Content, jsonSerializerSettings);
        byte[] content = Encoding.UTF8.GetBytes(json);

        HttpResponse response = context.HttpContext.Response;
        response.ContentLength = content.Length;
        if (StatusCode.HasValue) response.StatusCode = StatusCode.Value;
        await response.Body.WriteAsync(content);
        await response.Body.FlushAsync();
    }

    private static readonly JsonSerializerSettings jsonSerializerSettings = new()
    {
        NullValueHandling = NullValueHandling.Ignore,
        Formatting = Formatting.Indented,
        ContractResolver = new CamelCasePropertyNamesContractResolver(),
        Converters = { new StringEnumConverter(new DefaultNamingStrategy()), }
    };
}
