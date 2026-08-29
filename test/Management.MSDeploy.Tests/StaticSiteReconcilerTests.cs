using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Xylab.Management.WebDeploy.Deployment;

namespace Xylab.Management.WebDeploy.UnitTests;

[TestClass]
public sealed class StaticSiteReconcilerTests
{
    private readonly string _root = Directory.CreateTempSubdirectory().FullName;

    [TestMethod]
    public async Task WritesFilesAndDeletesStaleContent()
    {
        var siteRoot = Path.Combine(_root, "example");
        Directory.CreateDirectory(siteRoot);
        await File.WriteAllTextAsync(Path.Combine(siteRoot, "stale.txt"), "stale");
        var payload = new DeploymentPayload(
            [new DeploymentDirectory(4, 2, "assets")],
            [
                new DeploymentFile(
                    5,
                    4,
                    "site.css",
                    7,
                    "body {}"u8.ToArray())
            ]);
        var reconciler = CreateReconciler();

        var result = await reconciler.ReconcileAsync(
            "example\\wwwroot",
            payload,
            writeContent: true,
            dryRun: false,
            CancellationToken.None);

        Assert.AreEqual("example", result.SiteName);
        Assert.AreEqual("body {}", await File.ReadAllTextAsync(
            Path.Combine(result.SiteRoot, "assets", "site.css")));
        Assert.IsFalse(File.Exists(Path.Combine(result.SiteRoot, "stale.txt")));
        Assert.IsTrue(Path.GetFileName(result.SiteRoot).All(char.IsAsciiDigit));
    }

    [TestMethod]
    public async Task PassOneDoesNotModifyExistingSite()
    {
        var siteRoot = Path.Combine(_root, "example");
        Directory.CreateDirectory(siteRoot);
        await File.WriteAllTextAsync(Path.Combine(siteRoot, "existing.txt"), "original");
        var payload = new DeploymentPayload(
            [],
            [new DeploymentFile(4, 2, "replacement.txt", 10, null)]);

        var result = await CreateReconciler().ReconcileAsync(
            "example\\wwwroot",
            payload,
            writeContent: false,
            dryRun: false,
            CancellationToken.None);

        Assert.AreEqual(
            "original",
            await File.ReadAllTextAsync(Path.Combine(siteRoot, "existing.txt")));
        Assert.IsFalse(File.Exists(Path.Combine(siteRoot, "replacement.txt")));
        Assert.AreEqual(0, result.FilesWritten);
        Assert.AreEqual(0, result.FilesDeleted);
    }

    [TestMethod]
    public async Task IncompleteFinalPassPreservesExistingSite()
    {
        var siteRoot = Path.Combine(_root, "example");
        Directory.CreateDirectory(siteRoot);
        await File.WriteAllTextAsync(Path.Combine(siteRoot, "existing.txt"), "original");
        var payload = new DeploymentPayload(
            [],
            [new DeploymentFile(4, 2, "replacement.txt", 10, null)]);

        await Assert.ThrowsExactlyAsync<InvalidDataException>(
            () => CreateReconciler().ReconcileAsync(
                "example\\wwwroot",
                payload,
                writeContent: true,
                dryRun: false,
                CancellationToken.None));

        Assert.AreEqual(
            "original",
            await File.ReadAllTextAsync(Path.Combine(siteRoot, "existing.txt")));
    }

    [TestMethod]
    public async Task NginxValidationFailureRollsBackSite()
    {
        var siteRoot = Path.Combine(_root, "example");
        Directory.CreateDirectory(siteRoot);
        await File.WriteAllTextAsync(Path.Combine(siteRoot, "existing.txt"), "original");
        var payload = new DeploymentPayload(
            [],
            [new DeploymentFile(4, 2, "replacement.txt", 3, "new"u8.ToArray())]);
        var options = Options.Create(new WebDeployOptions
        {
            DeploymentRoot = _root,
            Nginx = new NginxOptions
            {
                Enabled = true,
                ConfigurationDirectory = Path.Combine(_root, "nginx", "enabled")
            }
        });
        var reconciler = new StaticSiteReconciler(
            options,
            new NginxSiteManager(
                options,
                new FailingNginxRunner(),
                NullLogger<NginxSiteManager>.Instance),
            NullLogger<StaticSiteReconciler>.Instance);

        await Assert.ThrowsExactlyAsync<InvalidOperationException>(
            () => reconciler.ReconcileAsync(
                "example\\wwwroot",
                payload,
                writeContent: true,
                dryRun: false,
                CancellationToken.None));

        Assert.AreEqual(
            "original",
            await File.ReadAllTextAsync(Path.Combine(siteRoot, "existing.txt")));
        Assert.IsFalse(File.Exists(Path.Combine(siteRoot, "replacement.txt")));
    }

    [TestMethod]
    public async Task FailedNginxSwitchKeepsPreviousContentAndConfigurationVersion()
    {
        var runner = new ToggleNginxRunner();
        var options = Options.Create(new WebDeployOptions
        {
            DeploymentRoot = _root,
            Nginx = new NginxOptions
            {
                Enabled = true,
                ConfigurationDirectory = Path.Combine(_root, "nginx", "enabled")
            }
        });
        var manager = new NginxSiteManager(
            options,
            runner,
            NullLogger<NginxSiteManager>.Instance);
        var reconciler = new StaticSiteReconciler(
            options,
            manager,
            NullLogger<StaticSiteReconciler>.Instance);
        var first = await reconciler.ReconcileAsync(
            "example\\wwwroot",
            new DeploymentPayload(
                [],
                [new DeploymentFile(4, 2, "index.html", 3, "old"u8.ToArray())]),
            writeContent: true,
            dryRun: false,
            CancellationToken.None);

        runner.Fail = true;
        await Assert.ThrowsExactlyAsync<InvalidOperationException>(
            () => reconciler.ReconcileAsync(
                "example\\wwwroot",
                new DeploymentPayload(
                    [],
                    [new DeploymentFile(4, 2, "index.html", 3, "new"u8.ToArray())]),
                writeContent: true,
                dryRun: false,
                CancellationToken.None));

        var siteDirectory = Path.Combine(_root, "example");
        Assert.AreEqual(
            Path.GetFileName(first.SiteRoot),
            (await File.ReadAllTextAsync(
                Path.Combine(siteDirectory, ".current"))).Trim());
        Assert.AreEqual("old", await File.ReadAllTextAsync(
            Path.Combine(first.SiteRoot, "index.html")));
        Assert.AreEqual(first.SiteRoot, manager.GetConfiguredRoot("example"));
        Assert.AreEqual(
            1,
            Directory.EnumerateDirectories(siteDirectory)
                .Count(path => Path.GetFileName(path).All(char.IsAsciiDigit)));
        Assert.AreEqual(
            1,
            Directory.EnumerateFiles(siteDirectory, "*.conf").Count());
    }

    [TestMethod]
    public async Task RetainsOnlyCurrentAndPreviousVersions()
    {
        var reconciler = CreateReconciler();
        var roots = new List<string>();
        for (var version = 1; version <= 3; version++)
        {
            var content = $"version-{version}";
            var payload = new DeploymentPayload(
                [],
                [
                    new DeploymentFile(
                        4,
                        2,
                        "index.html",
                        content.Length,
                        System.Text.Encoding.UTF8.GetBytes(content))
                ]);
            var result = await reconciler.ReconcileAsync(
                "example\\wwwroot",
                payload,
                writeContent: true,
                dryRun: false,
                CancellationToken.None);
            roots.Add(result.SiteRoot);
        }

        var siteDirectory = Path.Combine(_root, "example");
        var versions = Directory.EnumerateDirectories(siteDirectory)
            .Where(path => Path.GetFileName(path).All(char.IsAsciiDigit))
            .OrderBy(path => path)
            .ToArray();

        Assert.AreEqual(2, versions.Length);
        CollectionAssert.DoesNotContain(versions, roots[0]);
        CollectionAssert.Contains(versions, roots[1]);
        CollectionAssert.Contains(versions, roots[2]);
        Assert.AreEqual(
            Path.GetFileName(roots[2]),
            (await File.ReadAllTextAsync(
                Path.Combine(siteDirectory, ".current"))).Trim());
    }

    [TestMethod]
    [DataRow("../escape")]
    [DataRow("/absolute")]
    [DataRow("example/other")]
    public async Task RejectsInvalidDestinations(string destination)
    {
        var reconciler = CreateReconciler();
        var payload = new DeploymentPayload([], []);

        await Assert.ThrowsExactlyAsync<InvalidDataException>(
            () => reconciler.ReconcileAsync(
                destination,
                payload,
                writeContent: true,
                dryRun: false,
                CancellationToken.None));
    }

    [TestMethod]
    public void RendersStaticNginxConfiguration()
    {
        var configuration = NginxSiteManager.RenderConfiguration(
            "example.test",
            "/var/www/html/example.test",
            80);

        StringAssert.Contains(configuration, "server_name example.test;");
        StringAssert.Contains(configuration, "root \"/var/www/html/example.test\";");
        StringAssert.Contains(configuration, "try_files $uri $uri/ =404;");
    }

    [TestMethod]
    public async Task VersionsNginxConfigurationAndRetainsTwo()
    {
        var options = Options.Create(new WebDeployOptions
        {
            DeploymentRoot = Path.Combine(_root, "content"),
            Nginx = new NginxOptions
            {
                Enabled = true,
                ConfigurationDirectory = Path.Combine(_root, "nginx", "enabled")
            }
        });
        var manager = new NginxSiteManager(
            options,
            new NoOpNginxRunner(),
            NullLogger<NginxSiteManager>.Instance);

        foreach (var version in new[] { "1000", "2000", "3000" })
        {
            var siteRoot = Path.Combine(
                _root,
                "content",
                "example",
                version);
            Directory.CreateDirectory(siteRoot);
            await manager.EnsureSiteAsync(
                "example",
                siteRoot,
                CancellationToken.None);
        }

        var versionDirectory = Path.Combine(_root, "content", "example");
        CollectionAssert.AreEqual(
            new[] { "2000.conf", "3000.conf" },
            Directory.EnumerateFiles(versionDirectory, "*.conf")
                .Select(path => Path.GetFileName(path)!)
                .OrderBy(name => name)
                .ToArray());
        StringAssert.EndsWith(
            manager.GetConfiguredRoot("example")!,
            Path.Combine("example", "3000"));
    }

    [TestMethod]
    public async Task MigratesActiveConfigurationIntoSiteDirectory()
    {
        var deploymentRoot = Path.Combine(_root, "content");
        var siteDirectory = Path.Combine(deploymentRoot, "example");
        var previousRoot = Path.Combine(siteDirectory, "1000");
        var currentRoot = Path.Combine(siteDirectory, "2000");
        var configurationDirectory = Path.Combine(_root, "nginx", "enabled");
        Directory.CreateDirectory(previousRoot);
        Directory.CreateDirectory(currentRoot);
        Directory.CreateDirectory(configurationDirectory);
        await File.WriteAllTextAsync(
            Path.Combine(configurationDirectory, "example"),
            NginxSiteManager.RenderConfiguration("example", previousRoot, 80));
        var options = Options.Create(new WebDeployOptions
        {
            DeploymentRoot = deploymentRoot,
            Nginx = new NginxOptions
            {
                Enabled = true,
                ConfigurationDirectory = configurationDirectory
            }
        });
        var manager = new NginxSiteManager(
            options,
            new NoOpNginxRunner(),
            NullLogger<NginxSiteManager>.Instance);

        await manager.EnsureSiteAsync(
            "example",
            currentRoot,
            CancellationToken.None);

        Assert.IsTrue(File.Exists(Path.Combine(siteDirectory, "1000.conf")));
        Assert.IsTrue(File.Exists(Path.Combine(siteDirectory, "2000.conf")));
        Assert.AreEqual(currentRoot, manager.GetConfiguredRoot("example"));
    }

    [TestCleanup]
    public void Cleanup() => Directory.Delete(_root, recursive: true);

    private StaticSiteReconciler CreateReconciler()
    {
        var options = Options.Create(new WebDeployOptions
        {
            DeploymentRoot = _root,
            Nginx = new NginxOptions { Enabled = false }
        });
        var nginx = new NginxSiteManager(
            options,
            new NoOpNginxRunner(),
            NullLogger<NginxSiteManager>.Instance);
        return new StaticSiteReconciler(
            options,
            nginx,
            NullLogger<StaticSiteReconciler>.Instance);
    }

    private sealed class NoOpNginxRunner : INginxCommandRunner
    {
        public Task RunAsync(
            IReadOnlyList<string> arguments,
            CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class FailingNginxRunner : INginxCommandRunner
    {
        public Task RunAsync(
            IReadOnlyList<string> arguments,
            CancellationToken cancellationToken) =>
            Task.FromException(new InvalidOperationException("Invalid Nginx configuration."));
    }

    private sealed class ToggleNginxRunner : INginxCommandRunner
    {
        public bool Fail { get; set; }

        public Task RunAsync(
            IReadOnlyList<string> arguments,
            CancellationToken cancellationToken) =>
            Fail
                ? Task.FromException(
                    new InvalidOperationException("Invalid Nginx configuration."))
                : Task.CompletedTask;
    }
}
