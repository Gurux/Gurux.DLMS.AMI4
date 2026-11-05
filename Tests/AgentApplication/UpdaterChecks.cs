// --------------------------------------------------------------------------
// Gurux Ltd
//
// Copyright (c) Gurux Ltd.
//
// This file is part of Gurux Device Framework.
//
// Commit: $Format:%H$
// Date:   $Format:%cI$
//
// This software is available under either of the following licenses:
//
// 1. GNU General Public License, version 2 only (GPL-2.0-only).
//    See the LICENSE file included with this distribution.
//
// 2. A commercial license granted by Gurux Ltd under a separate written
//    agreement. Use under this option requires a valid commercial license
//    and is governed by the terms of that agreement.
//
// If you have not obtained a commercial license from Gurux Ltd,
// use of this software is governed by GPL-2.0-only.
//
// Commercial licensing: https://www.gurux.fi/
//
// SPDX-License-Identifier: GPL-2.0-only OR LicenseRef-Gurux-Commercial
// --------------------------------------------------------------------------

using Gurux.DLMS.AMI.Agent;
using Microsoft.Extensions.Logging.Abstractions;
using System.Diagnostics;
using System.IO.Compression;
using System.Net;
using System.Text;
using System.Security.Cryptography;
using System.Text.Json;

internal static class UpdaterChecks
{
    internal static async Task RunAsync()
    {
        string root = Path.Combine(Path.GetTempPath(), "unified-agent-" + Guid.NewGuid().ToString("N"));
        string install = Path.Combine(root, "agent install");
        string tool = Path.Combine(root, "external updater.dll");
        Directory.CreateDirectory(install);
        await File.WriteAllTextAsync(tool, "test launcher; never executed");
        string app = Path.Combine(install, OperatingSystem.IsWindows() ? "Gurux.DLMS.AMI.Agent.exe" : "Gurux.DLMS.AMI.Agent");
        await File.WriteAllTextAsync(app, "current executable");
        var options = new GXAgentUpdaterOptions { Executable = tool, ApplicationPath = app,
            InstallationDirectory = install, StagingDirectory = Path.Combine(root, "staging outside install") };
        string version = FileVersionInfo.GetVersionInfo(typeof(Settings).Assembly.Location).ProductVersion!.Split('+')[0];
        using var signingKey = RSA.Create(2048);
        options.PublicKeyPem = signingKey.ExportSubjectPublicKeyInfoPem();
        byte[] Package(string? badEntry = null, bool oldWorkerOnly = false)
        {
            using var data = new MemoryStream();
            using (var zip = new ZipArchive(data, ZipArchiveMode.Create, true))
            {
                if (oldWorkerOnly) {
                    Write(zip, "Gurux.DLMS.AMI.Agent.Worker.dll", [1, 2, 3]);
                }
                else
                {
                    Write(zip, "Gurux.DLMS.AMI.Agent.dll", File.ReadAllBytes(typeof(Settings).Assembly.Location));
                    Write(zip, "Gurux.DLMS.AMI.Agent.deps.json", Encoding.UTF8.GetBytes("{}"));
                    Write(zip, "Gurux.DLMS.AMI.Agent.runtimeconfig.json", Encoding.UTF8.GetBytes("{}"));
                    Write(zip, Path.GetFileName(app), [1]);
                }
                if (badEntry != null) {
                    Write(zip, badEntry, [1]);
                }
            }
            return data.ToArray();
        }
        GXAgentUpdateService Service(byte[] bytes) => new(options, NullLogger<GXAgentUpdateService>.Instance,
            () => new HttpClient(new BytesHandler(bytes, version, signingKey)));
        try
        {
            var service = Service(Package());
            if (service.UnavailableReason != null) {
                throw new Exception(service.UnavailableReason);
            }
            await service.PrepareAsync(version, ["https://releases.test/agent.zip"], CancellationToken.None);
            if (Directory.GetFiles(install).Length != 1 || await File.ReadAllTextAsync(app) != "current executable")
                {
                throw new Exception("Preparing an update changed the live installation.");
            }
            if (service.PendingUpdate?.Version != version || !File.Exists(service.PendingUpdate.PackagePath)) {
                throw new Exception("Full package was not staged.");
            }
            int starts = 0;
            service.StartProcess = (info, _) =>
            {
                starts++;
                var args = info.ArgumentList.ToArray();
                if (info.UseShellExecute || !args.Contains("--process-id") || args[Array.IndexOf(args, "--process-id") + 1] != "123" ||
                    args[Array.IndexOf(args, "--destination") + 1] != install || !args.Contains("--sha256") || !args.Contains("--local") ||
                    args.Contains("--no-restart") || info.WorkingDirectory.StartsWith(install, StringComparison.OrdinalIgnoreCase))
                    {
                    throw new Exception("Updater launch does not coordinate application replacement/restart outside the install.");
                }
                return Task.CompletedTask;
            };
            await service.LaunchAsync(123, CancellationToken.None);
            if (starts != 1) {
                throw new Exception("Updater did not receive the staged application package.");
            }
            await Reject(Service(Package("../escape.txt")), version);
            await Reject(Service(Package("settings.json")), version);
            await Reject(Service(Package("appsettings.json")), version);
            await Reject(Service(Package(oldWorkerOnly: true)), version);
            await Reject(Service(Package()), "999.0.0");
            var limits = Service(Package());
            options.MaxPackageBytes = 8;
            await Reject(limits, version);
            options.MaxPackageBytes = 512 * 1024 * 1024;
            options.MaxExpandedBytes = 8;
            await Reject(Service(Package()), version);
            options.MaxExpandedBytes = 2L * 1024 * 1024 * 1024;
            using (var wrongKey = RSA.Create(2048))
            {
                options.PublicKeyPem = wrongKey.ExportSubjectPublicKeyInfoPem();
                await Reject(Service(Package()), version);
            }
            options.PublicKeyPem = signingKey.ExportSubjectPublicKeyInfoPem();
            var tampered = new GXAgentUpdateService(options, NullLogger<GXAgentUpdateService>.Instance,
                () => new HttpClient(new BytesHandler(Package(), version, signingKey, tamper: true)));
            await Reject(tampered, version);
            try { await Service(Package()).PrepareAsync(version, ["http://releases.test/agent.zip"], CancellationToken.None); throw new Exception("HTTP accepted."); }
            catch (InvalidDataException) { }
            options.DownloadTimeout = TimeSpan.FromMilliseconds(100);
            var slow = new GXAgentUpdateService(options, NullLogger<GXAgentUpdateService>.Instance,
                () => new HttpClient(new SlowHandler()));
            await Reject(slow, version);
            var slowBody = new GXAgentUpdateService(options, NullLogger<GXAgentUpdateService>.Instance,
                () => new HttpClient(new BytesHandler(Package(), version, signingKey, slowBody: true)));
            await Reject(slowBody, version);
            options.DownloadTimeout = TimeSpan.FromMinutes(10);
            string stale = Path.Combine(options.StagingDirectory, Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(stale);
            File.WriteAllText(Path.Combine(stale, "agent.zip"), "stale");
            Directory.SetLastWriteTimeUtc(stale, DateTime.UtcNow.AddDays(-30));
            service.CleanupStaging();
            if (Directory.Exists(stale) || !File.Exists(service.PendingUpdate!.PackagePath)) {
                throw new Exception("Retention removed pending update or retained expired stage.");
            }
            string active = Path.Combine(options.StagingDirectory, Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(active);
            using (var lease = new FileStream(Path.Combine(active, ".active"), FileMode.Create, FileAccess.ReadWrite, FileShare.None))
            {
                Directory.SetLastWriteTimeUtc(active, DateTime.UtcNow.AddDays(-30));
                service.CleanupStaging();
                if (!Directory.Exists(active)) {
                    throw new Exception("Retention removed another active download.");
                }
            }
            string validTool = options.Executable;
            options.Executable = app;
            if (Service(Package()).UnavailableReason == null) {
                throw new Exception("Updater inside the install must be rejected.");
            }
            options.Executable = validTool;
            string? environment = Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER");
            try
            {
                Environment.SetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER", "true");
                if (Service(Package()).UnavailableReason == null) {
                    throw new Exception("Container agent must request an image update.");
                }
            }
            finally { Environment.SetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER", environment); }
            Console.WriteLine("PASS signed HTTPS packages, tamper rejection, download/body deadlines, size limits, active-stage retention and safe updater launch.");
        }
        finally
        {
            string expected = Path.GetFullPath(root);
            string tempRoot = Path.TrimEndingDirectorySeparator(Path.GetFullPath(Path.GetTempPath())) + Path.DirectorySeparatorChar;
            if (!expected.StartsWith(tempRoot, StringComparison.OrdinalIgnoreCase)) {
                throw new Exception("Invalid temporary cleanup path.");
            }
            Directory.Delete(expected, recursive: true);
        }
    }
    private static async Task Reject(GXAgentUpdateService service, string version)
    {
        try { await service.PrepareAsync(version, ["https://releases.test/agent.zip"], CancellationToken.None); }
        catch (InvalidOperationException) { if (service.PendingUpdate != null) {
            throw new Exception("Rejected package became pending.");
        } return; }
        throw new Exception("An invalid application package was accepted.");
    }
    private static void Write(ZipArchive zip, string name, byte[] data)
    { using var stream = zip.CreateEntry(name).Open(); stream.Write(data); }
    private sealed class BytesHandler(byte[] bytes, string version, RSA key, bool tamper = false, bool slowBody = false) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
        {
            if (request.Headers.Authorization != null) {
                throw new Exception("AMI credentials leaked to a release host.");
            }
            byte[] body = bytes;
            if (request.RequestUri!.AbsolutePath.EndsWith(".manifest.json"))
            {
                string hash = Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
                string signature = Convert.ToBase64String(key.SignData(Encoding.UTF8.GetBytes(version + "\n" + hash + "\n"), HashAlgorithmName.SHA256, RSASignaturePadding.Pss));
                body = JsonSerializer.SerializeToUtf8Bytes(new { version, sha256 = hash, signature });
            }
            else if (slowBody) {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StreamContent(new WaitingStream()) });
            }
            else if (tamper) {
                body = [1, 2, 3];
            }
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(body) });
        }
    }
    private sealed class SlowHandler : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
        { await Task.Delay(Timeout.Infinite, token); throw new Exception("Unreachable"); }
    }
    private sealed class WaitingStream : Stream
    {
        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken token = default)
        { await Task.Delay(Timeout.Infinite, token); return 0; }
        public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        public override void Flush() => throw new NotSupportedException();
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long length) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }
}
