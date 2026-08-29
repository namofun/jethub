namespace Xylab.Management.WebDeploy.UnitTests.Manual;

using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public sealed class ReferenceProxyManualTests
{
    [TestMethod]
    [TestCategory("Manual")]
    public async Task CapturesReferenceServerCapabilityResponse()
    {
        string endpoint = Environment.GetEnvironmentVariable("MSDEPLOY_REFERENCE_ENDPOINT");
        string username = Environment.GetEnvironmentVariable("MSDEPLOY_REFERENCE_USERNAME");
        string password = Environment.GetEnvironmentVariable("MSDEPLOY_REFERENCE_PASSWORD");
        string captureDirectory = Environment.GetEnvironmentVariable("MSDEPLOY_CAPTURE_DIRECTORY");

        if (string.IsNullOrWhiteSpace(endpoint)
            || string.IsNullOrWhiteSpace(username)
            || string.IsNullOrWhiteSpace(password)
            || string.IsNullOrWhiteSpace(captureDirectory))
        {
            Assert.Inconclusive(
                "Set the MSDEPLOY_REFERENCE_ENDPOINT, MSDEPLOY_REFERENCE_USERNAME, "
                + "MSDEPLOY_REFERENCE_PASSWORD, and MSDEPLOY_CAPTURE_DIRECTORY "
                + "environment variables to run this manual test.");
            return;
        }

        Assert.IsTrue(
            Uri.TryCreate(endpoint, UriKind.Absolute, out Uri endpointUri),
            "MSDEPLOY_REFERENCE_ENDPOINT must be an absolute URI.");

        string fullCaptureDirectory = Path.GetFullPath(captureDirectory);
        Directory.CreateDirectory(fullCaptureDirectory);
        int captureCount = Directory.EnumerateFiles(fullCaptureDirectory, "*.json").Count();
        ServiceCollection services = new();
        services.AddHttpClient(ReferenceProxy.ClientName, client => client.Timeout = TimeSpan.FromMinutes(10));
        await using ServiceProvider serviceProvider = services.BuildServiceProvider();

        ReferenceProxy proxy = new(
            serviceProvider.GetRequiredService<IHttpClientFactory>(),
            new ReferenceProxyOptions
            {
                Endpoint = endpointUri,
                Username = username,
                Password = password,
                CaptureDirectory = fullCaptureDirectory
            },
            new ProtocolCaptureStore());

        DefaultHttpContext context = new();
        context.Request.Method = HttpMethods.Head;
        context.Request.Path = "/MsDeployAgentService";
        context.Request.Headers["MSDeploy.VersionMin"] = "7.1.600.0";
        context.Request.Headers["MSDeploy.VersionMax"] = "9.0.1987.0";
        context.Response.Body = new MemoryStream();

        await proxy.ForwardAsync(context, CancellationToken.None);

        Assert.IsTrue(
            context.Response.StatusCode is >= 200 and < 300,
            $"The reference server returned HTTP {context.Response.StatusCode}.");

        Assert.IsTrue(
            context.Response.Headers.TryGetValue("MSDeploy.Response", out var responseVersion) && responseVersion.Any(value => value == "v1"),
            "The reference server did not return an MSDeploy capability response.");

        Assert.IsTrue(
            Directory.EnumerateFiles(fullCaptureDirectory, "*.json").Count() > captureCount,
            "The reference response metadata was not captured.");
    }
}
