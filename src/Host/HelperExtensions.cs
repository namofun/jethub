using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;
using System;
using System.IO.Abstractions;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading.Tasks;

namespace JetHub
{
    public static class HelperExtensions
    {
        public static string Action(this IUrlHelper url, object values)
        {
            return url.Action(url.ActionContext.ActionDescriptor.RouteValues["action"], values);
        }

        public static EntityTagHeaderValue CreateEntityTag(this IFileSystemInfo sysInfo)
        {
            byte[] etag = BitConverter.GetBytes(sysInfo.LastWriteTimeUtc.Ticks);

            var result = new StringBuilder(2 + etag.Length * 2);
            result.Append('"');
            foreach (byte b in etag) result.AppendFormat("{0:x2}", b);
            result.Append('"');

            return new EntityTagHeaderValue(result.ToString());
        }

        public static IEndpointConventionBuilder MapFileServer(this IEndpointRouteBuilder endpoints, string prefix, IFileProvider fileProvider)
        {
            prefix = prefix.TrimEnd('/');

            FileServerOptions options = new()
            {
                EnableDirectoryBrowsing = true,
                EnableDefaultFiles = false,
                RequestPath = prefix,
                FileProvider = fileProvider,
            };

            return endpoints.Map(
                prefix + "/{**slug}",
                endpoints.CreateApplicationBuilder()
                    .UseMiddleware<FileServerV2Middleware>(Options.Create(options))
                    .Build());
        }

        private class FileServerV2Middleware
        {
            private readonly IWebHostEnvironment _environment;
            private readonly IOptions<FileServerOptions> _options;
            private readonly ILoggerFactory _loggerFactory;
            private readonly HtmlEncoder _htmlEncoder;

            public FileServerV2Middleware(
                RequestDelegate next,
                IWebHostEnvironment environment,
                ILoggerFactory loggerFactory,
                HtmlEncoder htmlEncoder,
                IOptions<FileServerOptions> options)
            {
                this._htmlEncoder = HtmlEncoder.Default;
                this._loggerFactory = loggerFactory;
                this._environment = environment;
                this._options = options;
            }

            public Task Invoke(HttpContext context)
            {
                context.SetEndpoint(null);
                return this.InvokeDirectoryBrowser(context);
            }

            private Task InvokeDirectoryBrowser(HttpContext context)
            {
                return new DirectoryBrowserMiddleware(
                    this.InvokeStaticFile,
                    this._environment,
                    this._htmlEncoder,
                    Options.Create(this._options.Value.DirectoryBrowserOptions))
                    .Invoke(context);
            }

            private Task InvokeStaticFile(HttpContext context)
            {
                return new StaticFileMiddleware(
                    this.InvokeEopl,
                    this._environment,
                    Options.Create(this._options.Value.StaticFileOptions),
                    this._loggerFactory)
                    .Invoke(context);
            }

            private Task InvokeEopl(HttpContext context)
            {
                context.Response.StatusCode = 404;
                return Task.CompletedTask;
            }
        }
    }
}
