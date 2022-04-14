using Microsoft.AspNetCore.Mvc;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JetHub.Controllers
{
    public class AdhocController : ControllerBase
    {
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
