# Xylab.Management.WebDeploy

This project is a clean-room ASP.NET Core implementation of the server-side subset of Microsoft Web Deploy (MSDeploy) needed to publish static websites. It is designed for Linux hosts using Nginx and does not contain Microsoft Web Deploy source code.

**Implementation:** GitHub Copilot CLI

## How it was written

The protocol implementation was derived from documented HTTP behavior and authorized black-box interoperability testing with an unmodified `msdeploy.exe` client. Requests and responses were observed at the protocol boundary, then independently implemented in C#.

The implementation includes:

- MSDeploy capability negotiation over `HEAD`.
- `GetTraceStatus` streaming and two-pass `Sync` handling.
- Safe decoding of gzip/Base64 NRBF option headers with `System.Formats.Nrbf`; `BinaryFormatter` is not used.
- Compatible change-summary response headers.
- Static `contentPath` synchronization.
- Basic authentication, request-size limits, concurrency limits, bounded parsing, path validation, and symbolic-link rejection.
- Atomic content and Nginx configuration activation with rollback.

The supported surface is intentionally narrow. Package, manifest, IIS configuration, database, ACL, certificate, and other MSDeploy providers are not implemented.

## ASP.NET Core integration

Register the services from configuration and map both endpoints:

```csharp
using Xylab.Management.WebDeploy;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddWebDeploy()
    .Bind(builder.Configuration.GetSection(WebDeployOptions.SectionName));

var app = builder.Build();
app.MapWebDeploy();
app.Run();
```

The equivalent programmatic registration is:

```csharp
builder.Services.AddWebDeploy().Configure(options =>
{
    options.DeploymentRoot = "/var/www/msdeploy";
    options.Username = "publisher";
    options.Password = builder.Configuration["WebDeployPassword"];
    options.Nginx.Enabled = true;
    options.Nginx.Executable = "/usr/bin/sudo";
    options.Nginx.ArgumentsPrefix = ["-n", "/usr/sbin/nginx"];
});
```

Do not expose Basic authentication over unencrypted HTTP. Restrict the service to a trusted network or terminate TLS in front of it.

## Linux deployment layout

For a destination such as `example.test\wwwroot`, successful deployments use:

```text
/var/www/msdeploy/example.test/<epoch-ms>       # immutable site content
/var/www/msdeploy/example.test/<epoch-ms>.conf  # matching Nginx config
/var/www/msdeploy/example.test/.current         # active version marker
/etc/nginx/sites-enabled/example.test           # symlink to active .conf
```

The active Nginx entry is replaced atomically. Each site retains only the active version and its immediate predecessor, including the matching `.conf` files. If Nginx validation or reload fails, the previous symlink and active version marker are restored and the failed content/configuration pair is removed.

The service account needs write access to the deployment root and `/etc/nginx/sites-enabled`. If Nginx commands require elevation, grant only:

```text
/usr/sbin/nginx -t
/usr/sbin/nginx -s reload
```

## Invalid input behavior

Malformed Base64, gzip, NRBF, provider options, payload graphs, paths, file streams, or payload terminators are rejected before activation. The final pass must contain every requested file stream. A rejected or incomplete deployment does not replace the active site.

## Manual reference interoperability test

The forwarding proxy and redacted capture implementation used during clean-room research is intentionally kept out of the production package. It is located under `test/Management.MSDeploy.Tests/Manual`.

Set the reference endpoint credentials and run the `Manual` test category:

```powershell
$env:MSDEPLOY_REFERENCE_ENDPOINT = 'https://example/msdeploy.axd'
$env:MSDEPLOY_REFERENCE_USERNAME = 'publisher'
$env:MSDEPLOY_REFERENCE_PASSWORD = '...'
$env:MSDEPLOY_CAPTURE_DIRECTORY = '.\artifacts\msdeploy-captures'

dotnet test .\test\Management.MSDeploy.Tests\Management.WebDeploy.Tests.csproj `
  --filter 'TestCategory=Manual'
```

The test is reported as inconclusive when the required environment variables are absent. Capture metadata excludes authorization, cookie, and set-cookie values. Captures can still contain deployment content and must not be committed or shared.
