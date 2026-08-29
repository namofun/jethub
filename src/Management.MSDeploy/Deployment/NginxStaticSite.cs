using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Xylab.Management.WebDeploy.Deployment;

public sealed class NginxStaticSite(
    IOptions<NginxStaticSiteOptions> options,
    NginxSiteManager nginx,
    ILogger<NginxStaticSite> logger) : IWebDeployDeploymentTarget
{
    private const string CurrentVersionFile = ".current";
    private readonly SemaphoreSlim _deploymentLock = new(1, 1);
    private readonly string _deploymentRoot = Path.GetFullPath(options.Value.DeploymentRoot);

    public static void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<INginxCommandRunner, NginxCommandRunner>();
        services.AddSingleton<NginxSiteManager>();
        services.AddOptions<NginxStaticSiteOptions>()
            .ValidateDataAnnotations()
            .ValidateOnStart();
    }

    public async Task<WebDeployResult> DeployAsync(
        string destination,
        DeploymentPayload payload,
        bool writeContent,
        bool dryRun,
        CancellationToken cancellationToken)
    {
        var (siteName, siteDirectory) = ResolveDestination(destination);
        await _deploymentLock.WaitAsync(cancellationToken);
        try
        {
            return await DeployLockedAsync(
                siteName,
                siteDirectory,
                payload,
                writeContent,
                dryRun,
                cancellationToken);
        }
        finally
        {
            _deploymentLock.Release();
        }
    }

    private async Task<WebDeployResult> DeployLockedAsync(
        string siteName,
        string siteDirectory,
        DeploymentPayload payload,
        bool writeContent,
        bool dryRun,
        CancellationToken cancellationToken)
    {
        var directories = payload.ResolveDirectories();
        var expectedDirectories = directories.Values
            .Select(NormalizeRelativePath)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var expectedFiles = payload.Files
            .ToDictionary(
                file => NormalizeRelativePath(
                    payload.ResolveFilePath(file, directories)),
                StringComparer.OrdinalIgnoreCase);

        EnsureNoSymbolicLinks(siteDirectory);
        var currentRoot = GetCurrentRoot(siteName, siteDirectory);
        var existingFiles =
            currentRoot is not null && Directory.Exists(currentRoot)
            ? Directory.EnumerateFiles(currentRoot, "*", SearchOption.AllDirectories)
                .Select(path => NormalizeRelativePath(Path.GetRelativePath(currentRoot, path)))
                .ToHashSet(StringComparer.OrdinalIgnoreCase)
            : [];
        var filesDeleted = existingFiles.Count(path => !expectedFiles.ContainsKey(path));
        var directoriesCreated = expectedDirectories.Count(
            relativePath =>
                currentRoot is null
                || !Directory.Exists(ResolveChild(currentRoot, relativePath)));
        var filesWritten = writeContent || dryRun
            ? payload.Files.Count(file => dryRun || file.Content is not null)
            : 0;
        var bytesWritten = writeContent || dryRun
            ? payload.Files.Sum(file => dryRun ? file.Length : file.Content?.Length ?? 0)
            : 0;

        if (!writeContent && !dryRun)
        {
            return new WebDeployResult(
                siteName,
                currentRoot ?? siteDirectory,
                0,
                0,
                0,
                0);
        }

        if (dryRun)
        {
            return new WebDeployResult(
                siteName,
                currentRoot ?? siteDirectory,
                filesWritten,
                filesDeleted,
                directoriesCreated,
                bytesWritten);
        }

        var incompleteFile = payload.Files.FirstOrDefault(file => file.Content is null);
        if (incompleteFile is not null)
        {
            throw new InvalidDataException(
                $"File object {incompleteFile.Id} is missing stream content in the final pass.");
        }

        Directory.CreateDirectory(_deploymentRoot);
        EnsureNoSymbolicLinks(_deploymentRoot);
        Directory.CreateDirectory(siteDirectory);
        EnsureNoSymbolicLinks(siteDirectory);
        var version = CreateVersion(siteDirectory);
        var versionRoot = Path.Combine(siteDirectory, version);
        var stagingRoot = Path.Combine(siteDirectory, $".{version}.{Guid.NewGuid():N}.staging");
        Directory.CreateDirectory(stagingRoot);
        var previousState = ReadCurrentVersion(siteDirectory);
        var versionPublished = false;

        try
        {
            foreach (var relativeDirectory in expectedDirectories.OrderBy(path => path.Length))
            {
                Directory.CreateDirectory(ResolveChild(stagingRoot, relativeDirectory));
            }

            foreach (var (relativePath, file) in expectedFiles)
            {
                var fullPath = ResolveChild(stagingRoot, relativePath);
                Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
                await File.WriteAllBytesAsync(
                    fullPath,
                    file.Content!.Value.ToArray(),
                    cancellationToken);
            }

            Directory.Move(stagingRoot, versionRoot);
            versionPublished = true;
            WriteCurrentVersion(siteDirectory, version);
            try
            {
                await nginx.EnsureSiteAsync(siteName, versionRoot, CancellationToken.None);
            }
            catch
            {
                RestoreCurrentVersion(siteDirectory, previousState);
                throw;
            }

            PruneVersions(siteDirectory, version, currentRoot);
        }
        finally
        {
            if (Directory.Exists(stagingRoot))
            {
                Directory.Delete(stagingRoot, recursive: true);
            }

            if (versionPublished
                && !string.Equals(ReadCurrentVersion(siteDirectory), version, StringComparison.Ordinal)
                && Directory.Exists(versionRoot))
            {
                Directory.Delete(versionRoot, recursive: true);
            }
        }

        return new WebDeployResult(
            siteName,
            versionRoot,
            filesWritten,
            filesDeleted,
            directoriesCreated,
            bytesWritten);
    }

    private string? GetCurrentRoot(string siteName, string siteDirectory)
    {
        var configuredRoot = nginx.GetConfiguredRoot(siteName);
        if (configuredRoot is not null && IsVersionRoot(configuredRoot, siteDirectory))
        {
            return configuredRoot;
        }

        var currentVersion = ReadCurrentVersion(siteDirectory);
        if (currentVersion is null)
        {
            return null;
        }

        var currentRoot = Path.Combine(siteDirectory, currentVersion);
        return Directory.Exists(currentRoot) ? currentRoot : null;
    }

    private static string CreateVersion(string siteDirectory)
    {
        var version = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        while (Directory.Exists(Path.Combine(siteDirectory, version.ToString()))
            || File.Exists(Path.Combine(siteDirectory, $"{version}.conf")))
        {
            version++;
        }

        return version.ToString();
    }

    private static string? ReadCurrentVersion(string siteDirectory)
    {
        var path = Path.Combine(siteDirectory, CurrentVersionFile);
        if (!File.Exists(path) || IsSymbolicLink(path))
        {
            return null;
        }

        var version = File.ReadAllText(path).Trim();
        return version.Length > 0 && version.All(char.IsAsciiDigit)
            ? version
            : null;
    }

    private static void WriteCurrentVersion(string siteDirectory, string version)
    {
        var path = Path.Combine(siteDirectory, CurrentVersionFile);
        var temporaryPath = $"{path}.{Guid.NewGuid():N}.tmp";
        File.WriteAllText(temporaryPath, version);
        File.Move(temporaryPath, path, overwrite: true);
    }

    private static void RestoreCurrentVersion(string siteDirectory, string? previousVersion)
    {
        var path = Path.Combine(siteDirectory, CurrentVersionFile);
        if (previousVersion is null)
        {
            File.Delete(path);
            return;
        }

        WriteCurrentVersion(siteDirectory, previousVersion);
    }

    private void PruneVersions(string siteDirectory, string currentVersion, string? previousRoot)
    {
        var preserved = new HashSet<string>([currentVersion], StringComparer.Ordinal);
        if (previousRoot is not null && IsVersionRoot(previousRoot, siteDirectory))
        {
            preserved.Add(Path.GetFileName(previousRoot));
        }

        foreach (var directory in Directory.EnumerateDirectories(siteDirectory))
        {
            var name = Path.GetFileName(directory);
            if (!name.All(char.IsAsciiDigit) || preserved.Contains(name))
            {
                continue;
            }

            try
            {
                Directory.Delete(directory, recursive: true);
            }
            catch (IOException exception)
            {
                logger.LogWarning(
                    exception,
                    "Could not prune deployment version {Version} for site {Site}",
                    name,
                    Path.GetFileName(siteDirectory));
            }
            catch (UnauthorizedAccessException exception)
            {
                logger.LogWarning(
                    exception,
                    "Could not prune deployment version {Version} for site {Site}",
                    name,
                    Path.GetFileName(siteDirectory));
            }
        }
    }

    private static bool IsVersionRoot(string path, string siteDirectory)
    {
        var fullPath = Path.GetFullPath(path);
        var parent = Path.GetDirectoryName(fullPath);
        var version = Path.GetFileName(fullPath);
        return string.Equals(parent, Path.GetFullPath(siteDirectory), StringComparison.OrdinalIgnoreCase)
            && version.Length > 0
            && version.All(char.IsAsciiDigit);
    }

    private static void EnsureNoSymbolicLinks(string siteRoot)
    {
        if (!Directory.Exists(siteRoot))
        {
            return;
        }

        if (IsSymbolicLink(siteRoot))
        {
            throw new InvalidDataException("The site root cannot be a symbolic link.");
        }

        var pending = new Stack<string>();
        pending.Push(siteRoot);
        while (pending.TryPop(out var directory))
        {
            foreach (var entry in Directory.EnumerateFileSystemEntries(directory))
            {
                if (IsSymbolicLink(entry))
                {
                    throw new InvalidDataException(
                        $"Symbolic links are not allowed in a managed site: '{entry}'.");
                }

                if (Directory.Exists(entry))
                {
                    pending.Push(entry);
                }
            }
        }
    }

    private static bool IsSymbolicLink(string path)
    {
        return (File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0;
    }

    private (string SiteName, string SiteRoot) ResolveDestination(string destination)
    {
        if (string.IsNullOrWhiteSpace(destination) || Path.IsPathRooted(destination))
        {
            throw new InvalidDataException("The deployment destination must be a relative path.");
        }

        var segments = destination.Replace('\\', '/').Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length is < 1 or > 2
            || (segments.Length == 2
                && !string.Equals(segments[1], "wwwroot", StringComparison.OrdinalIgnoreCase))
            || !IsSafeSiteName(segments[0]))
        {
            throw new InvalidDataException("The destination must be '<siteName>' or '<siteName>\\wwwroot'.");
        }

        var siteName = segments[0];
        return (siteName, ResolveChild(_deploymentRoot, siteName));
    }

    private static string NormalizeRelativePath(string path)
    {
        return path.Replace('\\', Path.DirectorySeparatorChar)
                   .Replace('/', Path.DirectorySeparatorChar);
    }

    private static string ResolveChild(string root, string relativePath)
    {
        var fullPath = Path.GetFullPath(relativePath, root);
        var prefix = root.EndsWith(Path.DirectorySeparatorChar)
            ? root
            : root + Path.DirectorySeparatorChar;
        if (!fullPath.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException("The deployment path escapes the configured root.");
        }

        return fullPath;
    }

    private static bool IsSafeSiteName(string siteName)
    {
        if (siteName.Length is < 1 or > 128 || !char.IsAsciiLetterOrDigit(siteName[0]))
        {
            return false;
        }

        return siteName.All(character => char.IsAsciiLetterOrDigit(character) || character is '.' or '-');
    }
}
