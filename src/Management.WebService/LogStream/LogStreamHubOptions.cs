namespace Xylab.Management.LogStream;

using Microsoft.AspNetCore.Http;

public class LogStreamHubOptions
{
    /// <remarks>
    /// Sample code for selecting a file name:
    /// <code>
    ///     if (httpContext.Request.Query.TryGetValue("host", out var hosts) && hosts.Count == 1)
    ///     {
    ///         fileName = "/opt/domjudge/judgehost/log/judge." + hosts[0] + ".log";
    ///         if (!File.Exists(fileName))
    ///         {
    ///             fileName = null;
    ///         }
    ///
    ///         return fileName;
    ///     }
    /// </code>
    /// </remarks>
    public Func<IQueryCollection, string?> FileNameSelector { get; set; } = _ => null;
}