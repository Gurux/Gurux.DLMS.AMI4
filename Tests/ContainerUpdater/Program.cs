using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using System.Net;
using Gurux.DLMS.AMI.Agent;
using Gurux.Updater.Model;
using Gurux.Updater.Services;
using System.Text.Json;
using Gurux.DLMS.AMI.ContainerUpdater;
using System.Text.Json.Nodes;

if (args is ["--health-reporter", var healthRoot])
{
    Directory.CreateDirectory(Path.Combine(healthRoot, "queue"));
    using var cancellation = new CancellationTokenSource();
    using var signal = System.Runtime.InteropServices.PosixSignalRegistration.Create(System.Runtime.InteropServices.PosixSignal.SIGTERM,
        context => { context.Cancel = true; cancellation.Cancel(); });
    var version = AgentRuntimeVersion.Resolve(AppContext.BaseDirectory, null,
        System.Diagnostics.FileVersionInfo.GetVersionInfo(typeof(Settings).Assembly.Location).ProductVersion) ?? "";
    GXAgentContainerHealth.MarkDrained();
    try { await GXAgentContainerHealth.RunAsync(healthRoot, version, () => false, cancellation.Token); }
    catch (OperationCanceledException) when (cancellation.IsCancellationRequested) { }
    return;
}

var directory = Directory.CreateTempSubdirectory("gurux-container-tests-").FullName;
try
{
    var state = Path.Combine(directory, "state");
    var project = Path.Combine(directory, "project");
    Directory.CreateDirectory(project);
    await Bootstrap.InitializeAsync(state, project, false);
    var key = await File.ReadAllTextAsync(Path.Combine(state, "request.key"));
    Check(key.Length >= 64, "Bootstrap must create a strong request key.");
    Check(Directory.Exists(Path.Combine(state, "queue")), "Bootstrap must create the writable queue.");
    await Bootstrap.InitializeAsync(state, project, false);
    Check(await File.ReadAllTextAsync(Path.Combine(state, "request.key")) == key, "Bootstrap must never rotate an installed key.");
    await File.WriteAllTextAsync(Path.Combine(state, "compose.override.json"), "{\"services\":{\"ami\":{\"image\":\"test@sha256:abc\"}}}");
    await Bootstrap.SyncOverrideAsync(state, project);
    var saved = JsonNode.Parse(await File.ReadAllTextAsync(Path.Combine(project, "compose.override.yaml")))!;
    Check(saved["services"]!["ami"]!["image"]!.GetValue<string>() == "test@sha256:abc", "Manual Compose restarts must retain the last managed image.");
    await File.WriteAllTextAsync(Path.Combine(project, "compose.override.yaml"), "{\"services\":{\"custom\":{}}}");
    try
    {
        await Bootstrap.SyncOverrideAsync(state, project);
        throw new Exception("An unrelated override was overwritten.");
    }
    catch (InvalidOperationException)
    {
    }
    Check((await File.ReadAllTextAsync(Path.Combine(project, "compose.override.yaml"))).Contains("custom"), "Existing user configuration must remain intact.");
    var queueRoot = Path.Combine(directory, "agent-state");
    Directory.CreateDirectory(Path.Combine(queueRoot, "queue"));
    var agentKey = Path.Combine(queueRoot, "request.key");
    await File.WriteAllTextAsync(agentKey, key);
    await GXAgentContainerUpdate.QueueAsync(queueRoot, agentKey, "4.1.2611.1001", CancellationToken.None);
    var request = JsonSerializer.Deserialize<GXDeploymentRequest>(await File.ReadAllTextAsync(Path.Combine(queueRoot, "queue", "request.json")), new JsonSerializerOptions(JsonSerializerDefaults.Web))!;
    GXDeploymentAuthentication.Verify(request, key);
    Check(File.Exists(Path.Combine(queueRoot, "queue", request.RequestId + ".ready")), "A drained agent must mark its request ready.");
    try
    {
        await GXAgentContainerUpdate.QueueAsync(queueRoot, agentKey, "4.1.2611.1002", CancellationToken.None);
        throw new Exception("A pending update was overwritten.");
    }
    catch (IOException)
    {
    }
    Check(!GXAgentContainerHealth.IsHealthy(queueRoot, "4.1.2611.1001"), "An absent heartbeat must be unhealthy.");
    await GXAgentContainerHealth.WriteAsync(queueRoot, "4.1.2611.1001", true, CancellationToken.None);
    Check(GXAgentContainerHealth.IsHealthy(queueRoot, "4.1.2611.1001"), "A connected current runtime must be healthy.");
    Check(!GXAgentContainerHealth.IsHealthy(queueRoot, "4.1.2611.1002"), "An older process heartbeat must not validate a new image.");
    try
    {
        GXDeploymentAuthentication.Verify(request with { Version = "4.1.2611.1002" }, key);
        throw new Exception("An altered version was accepted.");
    }
    catch (UnauthorizedAccessException)
    {
    }
    using (var probe = Bootstrap.CreateHttpClient(new GXDeploymentOptions { Root = queueRoot }))
    {
        Check(await probe.GetStringAsync("http://127.0.0.1/agent-version") == "4.1.2611.1001", "The updater version probe must verify the connected runtime.");
        await File.WriteAllTextAsync(Path.Combine(queueRoot, "queue", "agent-health.json"), "{}");
        using var missing = await probe.GetAsync("http://127.0.0.1/agent-version");
        Check(missing.StatusCode == HttpStatusCode.ServiceUnavailable, "Malformed health data must not pass verification.");
    }
    GXAgentContainerHealth.MarkDrained();
    await GXAgentContainerHealth.WriteAsync(queueRoot, "4.1.2611.1001", false, CancellationToken.None);
    Check(GXAgentContainerHealth.IsHealthy(queueRoot, "4.1.2611.1001"), "A live drained process must remain healthy until Docker replaces it.");
    var wait = GXAgentContainerUpdate.WaitForFailureAsync(queueRoot, request.RequestId!, CancellationToken.None);
    await Task.Delay(30);
    Check(!wait.IsCompleted, "Handoff must keep the drained process alive rather than restart its worker.");
    await File.WriteAllTextAsync(Path.Combine(queueRoot, "status.json"), JsonSerializer.Serialize(new { requestId = request.RequestId, state = "Failed" }));
    await Task.Delay(30);
    Check(!wait.IsCompleted, "The agent must not resume while its update request is still queued.");
    File.Delete(Path.Combine(queueRoot, "queue", "request.json"));
    await wait.WaitAsync(TimeSpan.FromSeconds(3));
    GXAgentContainerHealth.Resume();
    var healthFile = Path.Combine(queueRoot, "queue", "agent-health.json");
    var staleProcess = JsonNode.Parse(await File.ReadAllTextAsync(healthFile))!;
    staleProcess["processStartedAt"] = 1;
    await File.WriteAllTextAsync(healthFile, staleProcess.ToJsonString());
    Check(!GXAgentContainerHealth.IsHealthy(queueRoot, "4.1.2611.1001"), "A restarted process must not inherit an older drained heartbeat.");
    if (!OperatingSystem.IsWindows())
    {
        var mounted = Path.Combine(directory, "mounted-project");
        var host = Path.Combine(directory, "host", "installation");
        Directory.CreateDirectory(mounted);
        await File.WriteAllTextAsync(Path.Combine(mounted, "extra.env"), "EXTRA=present");
        Bootstrap.MirrorProject(host, mounted);
        Check(await File.ReadAllTextAsync(Path.Combine(host, "extra.env")) == "EXTRA=present", "Compose env_file and secrets must be readable through the real host project path.");
        Bootstrap.MirrorProject(host, mounted);
    }
    var dockerHealth = new HealthRunner();
    await AgentRelease.WaitForHealthyContainerAsync(dockerHealth, new GXDeploymentOptions
    {
        ProductId = "Gurux.DLMS.AMI.Agent", Root = queueRoot, CheckTimeoutSeconds = 3,
        Compose = new GXComposeOptions { ProjectDirectory = directory, Services = new() { ["agent"] = "agent" } }
    }, CancellationToken.None);
    Check(dockerHealth.Checks == 2, "The updater must wait for Docker's drained-container health refresh before installation.");
    var configured = Gurux.DLMS.AMI.Agent.Program.ReadConfiguredAgent(new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
    {
        ["Agent:Address"] = "https://ami.test", ["Agent:Token"] = "test-token", ["Agent:Id"] = "11111111-2222-3333-4444-555555555555"
    }).Build(), "4.1.2611.1001")!;
    Check(configured.Id != Guid.Empty && configured.Address == "https://ami.test", "Compose registration must not require interactive input.");
    await File.WriteAllTextAsync(Path.Combine(queueRoot, "agent-heartbeat"), "ready");
    var settings = GXAgentUpdaterOptions.Read(new ConfigurationBuilder().Build());
    settings.DeploymentRoot = queueRoot;
    settings.RequestKeyFile = agentKey;
    settings.StagingDirectory = Path.Combine(directory, "verified-packages");
    var requests = new MetadataHandler();
    var service = new GXAgentUpdateService(settings, NullLogger<GXAgentUpdateService>.Instance, () => new HttpClient(requests, false));
    Check(service.UnavailableReason == null, "A configured live sidecar must enable container handoff without a local installer.");
    try
    {
        await service.PrepareAsync("9.9.9.9", new[] { "https://packages.test/agent.zip" }, CancellationToken.None);
        throw new Exception("An unaudited image was allowed to drain the agent.");
    }
    catch (InvalidOperationException)
    {
    }
    Check(requests.Count == 1 && service.PendingUpdate == null, "Maintenance metadata must be rejected before package download or worker drain.");
    Console.WriteLine("PASS bootstrap/key persistence, image override protection, signed agent queue, runtime health, noninteractive registration and pre-drain maintenance rejection.");
}
finally
{
    Directory.Delete(directory, true);
}
static void Check(bool condition, string message)
{
    if (!condition)
    {
        throw new Exception(message);
    }
}

sealed class MetadataHandler : HttpMessageHandler
{
    public int Count;
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
    {
        ++Count;
        if (!request.RequestUri!.AbsolutePath.EndsWith(".compose.json", StringComparison.Ordinal))
        {
            throw new Exception("A package was requested before container compatibility validation.");
        }
        var json = new JsonObject
        {
            ["schemaVersion"] = 1, ["version"] = "9.9.9.9", ["compose"] = new JsonObject
            {
                ["channel"] = "stable", ["platforms"] = new JsonArray("linux/amd64"),
                ["images"] = new JsonObject { ["agent"] = "guruxorg/dlms-ami-agent@sha256:" + new string('a', 64) },
                ["containsWasm"] = false, ["maintenanceRequired"] = true
            }
        };
        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(json.ToJsonString()) });
    }
}

sealed class HealthRunner : IGXComposeRunner
{
    public int Checks;
    public Task<string> RunAsync(IReadOnlyList<string> arguments, CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
        if (arguments[0] == "compose")
        {
            return Task.FromResult(new string('a', 64));
        }
        ++Checks;
        var state = Checks == 1 ? "starting" : "healthy";
        return Task.FromResult("[{\"State\":{\"Running\":true,\"Health\":{\"Status\":\"" + state + "\"}}}]");
    }
}
