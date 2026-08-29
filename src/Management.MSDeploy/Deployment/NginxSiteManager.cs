using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Xylab.Management.WebDeploy.Deployment;

public interface INginxCommandRunner
{
    Task RunAsync(
        IReadOnlyList<string> arguments,
        CancellationToken cancellationToken);
}

public sealed class NginxCommandRunner(
    IOptions<WebDeployOptions> options,
    ILogger<NginxCommandRunner> logger) : INginxCommandRunner
{
    private readonly string _executable = options.Value.Nginx.Executable;
    private readonly IReadOnlyList<string> _argumentsPrefix =
        options.Value.Nginx.ArgumentsPrefix;

    public async Task RunAsync(
        IReadOnlyList<string> arguments,
        CancellationToken cancellationToken)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = _executable,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };

        foreach (var argument in _argumentsPrefix.Concat(arguments))
        {
            startInfo.ArgumentList.Add(argument);
        }

        using var process = Process.Start(startInfo) ??
            throw new InvalidOperationException($"Failed to start '{_executable}'.");
        var standardOutput = process.StandardOutput.ReadToEndAsync(cancellationToken);
        var standardError = process.StandardError.ReadToEndAsync(cancellationToken);
        await process.WaitForExitAsync(cancellationToken);
        var output = await standardOutput;
        var error = await standardError;

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException(
                $"{_executable} {string.Join(' ', arguments)} failed with exit code " +
                $"{process.ExitCode}: {error}");
        }

        logger.LogDebug(
            "{Executable} {Arguments}: {Output}",
            _executable,
            string.Join(' ', arguments),
            output);
    }
}

public sealed class NginxSiteManager(
    IOptions<WebDeployOptions> options,
    INginxCommandRunner commandRunner,
    ILogger<NginxSiteManager> logger)
{
    private static readonly SemaphoreSlim ConfigurationLock = new(1, 1);
    private static readonly Regex RootDirective = new(
        "^\\s*root\\s+\"(?<path>[^\"]+)\";\\s*$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Multiline);
    private readonly NginxOptions _options = options.Value.Nginx;
    private readonly string _deploymentRoot =
        Path.GetFullPath(options.Value.DeploymentRoot);

    public string? GetConfiguredRoot(string siteName)
    {
        if (!_options.Enabled)
        {
            return null;
        }

        var configurationPath = Path.Combine(
            Path.GetFullPath(_options.ConfigurationDirectory),
            siteName);
        if (!File.Exists(configurationPath))
        {
            return null;
        }

        string? configuredVersion = null;
        if ((File.GetAttributes(configurationPath) & FileAttributes.ReparsePoint) != 0)
        {
            var target = new FileInfo(configurationPath).ResolveLinkTarget(
                returnFinalTarget: true);
            if (target is null ||
                !IsVersionedConfigurationPath(target.FullName, siteName))
            {
                return null;
            }

            configuredVersion = Path.GetFileNameWithoutExtension(target.FullName);
        }

        var match = RootDirective.Match(File.ReadAllText(configurationPath));
        if (!match.Success)
        {
            return null;
        }

        var configuredRoot = Path.GetFullPath(match.Groups["path"].Value);
        var rootVersion = Path.GetFileName(configuredRoot);
        return IsVersionRoot(configuredRoot, siteName) &&
               (configuredVersion is null ||
                string.Equals(
                    configuredVersion,
                    rootVersion,
                    StringComparison.Ordinal))
            ? configuredRoot
            : null;
    }

    public async Task EnsureSiteAsync(
        string siteName,
        string siteRoot,
        CancellationToken cancellationToken)
    {
        if (!_options.Enabled)
        {
            return;
        }

        var configurationDirectory =
            Path.GetFullPath(_options.ConfigurationDirectory);
        var configurationPath = Path.Combine(configurationDirectory, siteName);
        var fullSiteRoot = Path.GetFullPath(siteRoot);
        var siteDirectory = Path.GetDirectoryName(fullSiteRoot)!;
        var version = Path.GetFileName(fullSiteRoot);
        if (version.Length == 0 ||
            !version.All(char.IsAsciiDigit) ||
            !string.Equals(
                siteDirectory,
                Path.Combine(_deploymentRoot, siteName),
                StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException(
                "The Nginx site root must be the site's numeric deployment version.");
        }

        var versionedConfigurationPath = Path.Combine(
            siteDirectory,
            $"{version}.conf");
        var configuration = RenderConfiguration(
            siteName,
            fullSiteRoot,
            _options.ListenPort);

        await ConfigurationLock.WaitAsync(cancellationToken);
        try
        {
            Directory.CreateDirectory(configurationDirectory);
            Directory.CreateDirectory(siteDirectory);
            var previousConfiguration = await CaptureActiveConfigurationAsync(
                configurationPath,
                cancellationToken);
            var previousVersion = GetActiveVersion(
                configurationPath,
                siteDirectory);
            if (previousVersion is not null)
            {
                var previousVersionPath = Path.Combine(
                    siteDirectory,
                    $"{previousVersion}.conf");
                if (!File.Exists(previousVersionPath))
                {
                    await File.WriteAllBytesAsync(
                        previousVersionPath,
                        previousConfiguration.Content!,
                        cancellationToken);
                }
            }

            var temporaryVersionPath =
                $"{versionedConfigurationPath}.{Guid.NewGuid():N}.tmp";
            await File.WriteAllTextAsync(
                temporaryVersionPath,
                configuration,
                new UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
                cancellationToken);
            File.Move(
                temporaryVersionPath,
                versionedConfigurationPath,
                overwrite: true);
            ActivateConfiguration(
                configurationDirectory,
                configurationPath,
                versionedConfigurationPath,
                siteName);

            try
            {
                await commandRunner.RunAsync(["-t"], cancellationToken);
                await commandRunner.RunAsync(["-s", "reload"], cancellationToken);
            }
            catch
            {
                RestoreActiveConfiguration(
                    configurationPath,
                    previousConfiguration);
                try
                {
                    await commandRunner.RunAsync(["-t"], CancellationToken.None);
                    await commandRunner.RunAsync(
                        ["-s", "reload"],
                        CancellationToken.None);
                }
                catch (Exception rollbackException)
                {
                    logger.LogError(
                        rollbackException,
                        "Failed to reload the previous Nginx configuration for site {Site}",
                        siteName);
                }

                File.Delete(versionedConfigurationPath);
                throw;
            }

            PruneVersionedConfigurations(
                siteDirectory,
                version,
                previousVersion);
        }
        finally
        {
            ConfigurationLock.Release();
        }
    }

    private static async Task<ActiveConfigurationSnapshot>
        CaptureActiveConfigurationAsync(
            string configurationPath,
            CancellationToken cancellationToken)
    {
        if (!File.Exists(configurationPath))
        {
            return new ActiveConfigurationSnapshot(false, null, null);
        }

        if ((File.GetAttributes(configurationPath) & FileAttributes.ReparsePoint) != 0)
        {
            return new ActiveConfigurationSnapshot(
                true,
                new FileInfo(configurationPath).LinkTarget,
                await File.ReadAllBytesAsync(configurationPath, cancellationToken));
        }

        return new ActiveConfigurationSnapshot(
            true,
            null,
            await File.ReadAllBytesAsync(configurationPath, cancellationToken));
    }

    private static void ActivateConfiguration(
        string configurationDirectory,
        string configurationPath,
        string versionedConfigurationPath,
        string siteName)
    {
        var temporaryPath = Path.Combine(
            configurationDirectory,
            $".{siteName}.{Guid.NewGuid():N}.tmp");
        if (OperatingSystem.IsWindows())
        {
            File.Copy(versionedConfigurationPath, temporaryPath);
        }
        else
        {
            File.CreateSymbolicLink(
                temporaryPath,
                Path.GetRelativePath(
                    configurationDirectory,
                    versionedConfigurationPath));
        }

        File.Move(temporaryPath, configurationPath, overwrite: true);
    }

    private static void RestoreActiveConfiguration(
        string configurationPath,
        ActiveConfigurationSnapshot snapshot)
    {
        File.Delete(configurationPath);
        if (!snapshot.Exists)
        {
            return;
        }

        if (snapshot.LinkTarget is not null)
        {
            File.CreateSymbolicLink(configurationPath, snapshot.LinkTarget);
            return;
        }

        File.WriteAllBytes(configurationPath, snapshot.Content!);
    }

    private static string? GetActiveVersion(
        string configurationPath,
        string siteDirectory)
    {
        if (!File.Exists(configurationPath))
        {
            return null;
        }

        var match = RootDirective.Match(File.ReadAllText(configurationPath));
        if (!match.Success)
        {
            return null;
        }

        var rootVersion = Path.GetFileName(
            Path.GetFullPath(match.Groups["path"].Value));
        return rootVersion.All(char.IsAsciiDigit) &&
               string.Equals(
                   Path.GetDirectoryName(Path.GetFullPath(match.Groups["path"].Value)),
                   Path.GetFullPath(siteDirectory),
                   StringComparison.OrdinalIgnoreCase) &&
               Directory.Exists(Path.Combine(siteDirectory, rootVersion))
            ? rootVersion
            : null;
    }

    private static void PruneVersionedConfigurations(
        string siteDirectory,
        string currentVersion,
        string? previousVersion)
    {
        foreach (var configuration in Directory.EnumerateFiles(
                     siteDirectory,
                     "*.conf"))
        {
            var version = Path.GetFileNameWithoutExtension(configuration);
            if (!version.All(char.IsAsciiDigit) ||
                version == currentVersion ||
                version == previousVersion)
            {
                continue;
            }

            File.Delete(configuration);
        }
    }

    private bool IsVersionedConfigurationPath(string path, string siteName)
    {
        var fullPath = Path.GetFullPath(path);
        var version = Path.GetFileNameWithoutExtension(fullPath);
        return string.Equals(
                   Path.GetDirectoryName(fullPath),
                   Path.Combine(_deploymentRoot, siteName),
                   StringComparison.OrdinalIgnoreCase) &&
               string.Equals(
                   Path.GetExtension(fullPath),
                   ".conf",
                   StringComparison.OrdinalIgnoreCase) &&
               version.Length > 0 &&
               version.All(char.IsAsciiDigit);
    }

    private bool IsVersionRoot(string path, string siteName)
    {
        var fullPath = Path.GetFullPath(path);
        var version = Path.GetFileName(fullPath);
        return string.Equals(
                   Path.GetDirectoryName(fullPath),
                   Path.Combine(_deploymentRoot, siteName),
                   StringComparison.OrdinalIgnoreCase) &&
               version.Length > 0 &&
               version.All(char.IsAsciiDigit);
    }

    private sealed record ActiveConfigurationSnapshot(
        bool Exists,
        string? LinkTarget,
        byte[]? Content);

    public static string RenderConfiguration(
        string siteName,
        string siteRoot,
        int listenPort)
    {
        if (siteRoot.IndexOfAny(['"', '\r', '\n', '{', '}', ';']) >= 0)
        {
            throw new InvalidDataException("The site root contains invalid Nginx characters.");
        }

        return $$"""
            server {
                listen {{listenPort}};
                listen [::]:{{listenPort}};
                server_name {{siteName}};

                root "{{siteRoot}}";
                index index.html;

                location / {
                    try_files $uri $uri/ =404;
                }
            }

            """;
    }
}
