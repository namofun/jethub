using System;
using System.IO;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Identity.Web;
using Xylab.Management;
using Xylab.Management.Automation.Cmdlets;
using Xylab.Management.Services;
using Xylab.Management.WebDeploy;
using Xylab.Management.WebDeploy.Deployment;
using Xylab.Remoting.PowerShellWebService;
using Xylab.Workflows.LogicApps;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAuthentication("Default")
    .AddPolicyScheme("Default", null, options =>
    {
        options.ForwardChallenge = JwtBearerDefaults.AuthenticationScheme;
        options.ForwardForbid = JwtBearerDefaults.AuthenticationScheme;
        options.ForwardDefaultSelector = context =>
        {
            var authorization = context.Request.Headers.Authorization.ToString();
            return authorization.Equals("Bearer", StringComparison.OrdinalIgnoreCase)
                || authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
                ? JwtBearerDefaults.AuthenticationScheme
                : OpenIdConnectDefaults.AuthenticationScheme;
        };
    });

builder.Services.AddAuthentication()
    .AddMicrosoftIdentityWebApp(builder.Configuration);

builder.Services.AddAuthentication()
    .AddMicrosoftIdentityWebApi(builder.Configuration);

builder.Services.AddAuthorizationBuilder()
    .AddFallbackPolicy("DefaultRequireAuth", new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());

builder.Services.AddAntiforgery();

builder.Services.ConfigureHttpJsonOptions(json => json.SerializerOptions.WriteIndented = true);

builder.Services.ConfigureServiceVersion(typeof(Program).Assembly);

builder.Services.AddSignalR(o => o.EnableDetailedErrors = true);

builder.Services.AddVirtualFileSystem();

builder.Services.AddHostSystemApi();

builder.Services.AddLogStreamWebSocket()
    .WithFileNameSelector(_ => Path.Combine(Environment.CurrentDirectory, "playground", "null.log"));

builder.Services.AddWorkflowEngine()
    .WithAzureStorageAccountConnectionString(builder.Configuration.GetValue<string>("WorkflowEngineStorage"));

builder.Services.AddPowerShellWebService()
    .WithWebSocketSupport()
    .ImportAssembly(typeof(SayHelloWorld).Assembly);

builder.Services.AddWebDeploy()
    .WithDeploymentTarget<NginxStaticSite>();

var app = builder.Build();

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.MapServiceVersion("/");
app.MapLoginAndLogout();
app.MapSystemInformation("/sysinfo");
app.MapWorkflows("/workflows");
app.MapVirtualFileSystem("/files", fileSystemRoot: "/");
app.MapLogStream("/logstream", new() { Path = Path.Combine(Environment.CurrentDirectory, "playground") });
app.MapLogStreamWebSocket("/logstream/ws").AllowAnonymous();
app.MapWebDeploy();
app.MapPowerShellHttpRequest("/powershell");
app.MapPowerShellWebSocket("/powershell/stream");

app.Run();
