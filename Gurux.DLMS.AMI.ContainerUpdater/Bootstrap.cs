using System.Diagnostics;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Nodes;
using Gurux.Updater.Model;
using Gurux.Updater.Services;

namespace Gurux.DLMS.AMI.ContainerUpdater;

internal static class Bootstrap
{
    private const string Marker = "x-gurux-updater";
    internal static async Task InitializeAsync(string root, string project, bool permissions = true)
    {
        Directory.CreateDirectory(root);
        Directory.CreateDirectory(Path.Combine(root, "queue"));
        if (permissions && OperatingSystem.IsLinux())
        {
            await SetPermissionsAsync(root, "0:1654", UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute | UnixFileMode.GroupRead | UnixFileMode.GroupExecute);
            await SetPermissionsAsync(Path.Combine(root, "queue"), "0:1654", UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute | UnixFileMode.GroupRead | UnixFileMode.GroupWrite | UnixFileMode.GroupExecute | UnixFileMode.SetGroup);
        }
        var key = Path.Combine(root, "request.key");
        if (!File.Exists(key))
        {
            await using var file = new FileStream(key, FileMode.CreateNew, FileAccess.Write, FileShare.None);
            await file.WriteAsync(System.Text.Encoding.UTF8.GetBytes(Convert.ToHexString(RandomNumberGenerator.GetBytes(32))));
            file.Flush(true);
        }
        if ((await File.ReadAllTextAsync(key)).Trim().Length < 64)
        {
            throw new InvalidOperationException("The installed request key is invalid; refusing to rotate it automatically.");
        }
        var queueLock = Path.Combine(root, "queue", "queue.lock");
        using (new FileStream(queueLock, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None)) { }
        if (permissions && OperatingSystem.IsLinux())
        {
            await SetPermissionsAsync(key, "0:1654", UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.GroupRead);
            await SetPermissionsAsync(queueLock, "0:1654", UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.GroupRead | UnixFileMode.GroupWrite);
            if (Directory.Exists("/agent-data"))
            {
                await SetPermissionsAsync("/agent-data", "1654:1654", UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
            }
        }
        await SyncOverrideAsync(root, project);
    }

    internal static async Task SyncOverrideAsync(string root, string project)
    {
        var destination = Path.Combine(project, "compose.override.yaml");
        if (File.Exists(destination))
        {
            JsonNode? previous;
            try { previous = JsonNode.Parse(await File.ReadAllTextAsync(destination)); }
            catch (JsonException) { throw new InvalidOperationException("compose.override.yaml belongs to the installation; merge it before enabling the updater."); }
            if (previous?[Marker]?.GetValue<bool>() != true)
            {
                throw new InvalidOperationException("Refusing to overwrite an unrelated compose.override.yaml.");
            }
        }
        var source = Path.Combine(root, "compose.override.json");
        var value = File.Exists(source) ? JsonNode.Parse(await File.ReadAllTextAsync(source))! : new JsonObject { ["services"] = new JsonObject() };
        value[Marker] = true;
        var content = value.ToJsonString(new JsonSerializerOptions { WriteIndented = true }).Replace("\r\n", "\n").Replace("\n", "\r\n");
        if (File.Exists(destination) && await File.ReadAllTextAsync(destination) == content)
        {
            return;
        }
        var temporary = destination + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            await using (var output = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                await output.WriteAsync(System.Text.Encoding.UTF8.GetBytes(content));
                output.Flush(true);
            }
            File.Move(temporary, destination, true);
        }
        finally { File.Delete(temporary); }
    }

    internal static void MirrorProject(string hostProject, string mountedProject)
    {
        if (Path.GetFullPath(hostProject) == Path.GetFullPath(mountedProject))
        {
            return;
        }
        if (Path.GetFullPath(hostProject).StartsWith(Path.TrimEndingDirectorySeparator(Path.GetFullPath(mountedProject)) + Path.DirectorySeparatorChar, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("The host project path cannot be nested inside the updater's project mount.");
        }
        var directory = new DirectoryInfo(hostProject);
        if (directory.LinkTarget == mountedProject)
        {
            return;
        }
        if (Directory.Exists(hostProject) || File.Exists(hostProject))
        {
            throw new InvalidOperationException("The host project path conflicts with an existing updater-image directory.");
        }
        Directory.CreateDirectory(Path.GetDirectoryName(hostProject)!);
        Directory.CreateSymbolicLink(hostProject, mountedProject);
    }

    internal static async Task<string> ConfigureAsync(string root, CancellationToken token)
    {
        var kind = Environment.GetEnvironmentVariable("GURUX_APPLICATION") ?? "ami";
        if (kind is not ("ami" or "agent"))
        {
            throw new InvalidOperationException("Only the AMI and agent installations are supported.");
        }
        var runner = new GXComposeProcessRunner("/project");
        using var own = JsonDocument.Parse(await runner.RunAsync(new[] { "inspect", Environment.MachineName }, token));
        var container = own.RootElement[0];
        var labels = container.GetProperty("Config").GetProperty("Labels");
        var projectName = labels.GetProperty("com.docker.compose.project").GetString()!;
        var hostProject = container.GetProperty("Mounts").EnumerateArray().Single(m => m.GetProperty("Destination").GetString() == "/project").GetProperty("Source").GetString()!;
        // Desktop/WSL may translate the client working directory to a different daemon bind-source path.
        var clientProject = labels.GetProperty("com.docker.compose.project.working_dir").GetString()!;
        if (!Path.IsPathFullyQualified(hostProject) || !Path.IsPathFullyQualified(clientProject) || hostProject == Path.GetPathRoot(hostProject))
        {
            throw new InvalidOperationException("Start this Compose installation from a Linux/WSL filesystem directory.");
        }
        // Preserve host bind-source paths while allowing Compose to read env_file, secret and config files at those same paths.
        MirrorProject(hostProject, "/project");
        Environment.SetEnvironmentVariable("COMPOSE_PROJECT_NAME", projectName);
        await InitializeAsync(root, "/project");
        var files = new List<string>();
        foreach (var file in labels.GetProperty("com.docker.compose.project.config_files").GetString()!.Split(','))
        {
            var relative = Path.GetRelativePath(clientProject, file);
            if (relative.StartsWith("..", StringComparison.Ordinal) || Path.IsPathFullyQualified(relative))
            {
                throw new InvalidOperationException("All installation Compose files must be in the mounted project directory.");
            }
            files.Add(Path.Combine("/project", relative));
        }
        if (!files.Contains("/project/compose.override.yaml", StringComparer.Ordinal))
        {
            files.Add("/project/compose.override.yaml");
        }
        bool agent = kind == "agent";
        var publicUrl = agent ? "http://127.0.0.1/" : Environment.GetEnvironmentVariable("AMI_URL") ?? throw new InvalidOperationException("Configure AMI_URL with the public HTTPS address.");
        var options = new GXDeploymentOptions
        {
            Mode = "compose", Root = root, ProductId = agent ? "Gurux.DLMS.AMI.Agent" : "Gurux.DLMS.AMI",
            CatalogUrl = Environment.GetEnvironmentVariable("GURUX_CATALOG_URL") ?? "https://gurux.github.io/Gurux.AMI.Catalog/catalog.json",
            PublicUrl = publicUrl, ApiVersionUrl = agent ? publicUrl + "agent-version" : new Uri(new Uri(publicUrl), "/api/Configuration/Version").AbsoluteUri,
            IncludeBackend = !agent, RequestKeyEnvironmentVariable = "GURUX_UPDATE_REQUEST_KEY", CheckTimeoutSeconds = 180,
            Compose = new GXComposeOptions
            {
                ProjectName = projectName, ProjectDirectory = hostProject, Files = files,
                Services = new() { [kind] = kind }, AllowedImageRepositories = new() { agent ? "guruxorg/dlms-ami-agent" : "guruxorg/dlms-ami" },
                Platform = Environment.GetEnvironmentVariable("GURUX_PLATFORM") ?? "linux/amd64", WasmRole = kind
            }
        };
        Environment.SetEnvironmentVariable(options.RequestKeyEnvironmentVariable, (await File.ReadAllTextAsync(Path.Combine(root, "request.key"), token)).Trim());
        GXComposeDeploymentService.ValidateOptions(options);
        var configuration = Path.Combine(root, "container-updater.json");
        await File.WriteAllTextAsync(configuration, JsonSerializer.Serialize(options, new JsonSerializerOptions(JsonSerializerDefaults.Web)), token);
        return configuration;
    }

    internal static HttpClient CreateHttpClient(GXDeploymentOptions options)
    {
        var handler = new AgentVersionHandler(options.Root) { InnerHandler = new HttpClientHandler() };
        return new HttpClient(handler) { Timeout = TimeSpan.FromMinutes(10) };
    }

    private static async Task SetPermissionsAsync(string path, string owner, UnixFileMode mode)
    {
        if (OperatingSystem.IsLinux())
        {
            var info = new ProcessStartInfo("chown") { UseShellExecute = false };
            info.ArgumentList.Add(owner); info.ArgumentList.Add(path);
            using var process = Process.Start(info)!;
            await process.WaitForExitAsync();
            if (process.ExitCode != 0)
            {
                throw new IOException("Cannot initialize updater storage permissions.");
            }
            File.SetUnixFileMode(path, mode);
        }
    }
}
