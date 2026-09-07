namespace Xylab.Workflows.LogicApps.WebApi;

using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

public class NewtonsoftJsonResult(object content) : IResult
{
    public object Content { get; } = content;

    public string? ETag { get; set; }

    public int? StatusCode { get; set; }

    public async Task ExecuteAsync(HttpContext httpContext)
    {
        string json = JsonConvert.SerializeObject(Content, jsonSerializerSettings);
        byte[] content = Encoding.UTF8.GetBytes(json);

        HttpResponse response = httpContext.Response;
        response.ContentLength = content.Length;
        if (StatusCode.HasValue) response.StatusCode = StatusCode.Value;
        if (ETag != null) response.Headers.ETag = ETag;
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
