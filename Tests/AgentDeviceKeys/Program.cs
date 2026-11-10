using System.Diagnostics;
using System.Net;
using System.Reflection;
using Gurux.DLMS.AMI.Agent.Worker;
using Gurux.DLMS.AMI.Shared.DTOs.Device;
using Gurux.DLMS.Enums;
using Gurux.DLMS.Objects;
using Gurux.DLMS.Secure;

var assembly = typeof(GXAgentWorker).Assembly;
if (assembly.GetType("Gurux.DLMS.AMI.Client.Shared.KeyManagement.GXManagedKeySession") != null ||
    assembly.GetReferencedAssemblies().Any(a => a.Name == "Gurux.DLMS.AMI.Client"))
    {
    throw new Exception("Agent still depends on the managed acquisition client.");
}
var type = assembly.GetType("Gurux.DLMS.AMI.Agent.Worker.GXDLMSReader")!;
using var http = new HttpClient(new RejectKeys()) { BaseAddress = new("https://ami.test/") };
var settings = new GXDLMSSettings
{
    Password = [1, 2, 3], BlockCipherKey = Enumerable.Repeat((byte)0x11, 16).ToArray(),
    AuthenticationKey = Enumerable.Repeat((byte)0x22, 16).ToArray()
};
var client = new GXDLMSSecureClient(true, 16, 1, Authentication.Low, null, InterfaceType.WRAPPER);
client.Ciphering.Security = Security.AuthenticationEncryption;
var apply = type.GetMethod("ApplyDeviceKeys", BindingFlags.Static | BindingFlags.NonPublic)!;
apply.Invoke(null, [client, settings]);
var reader = Activator.CreateInstance(type, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null,
    [client, null, null, TraceLevel.Off, TraceLevel.Off, 1, 1, new GXDevice { Id = Guid.NewGuid() }, http], null)!;
if (!client.Password.SequenceEqual(settings.Password) || client.AARQRequest().Length == 0 ||
    client.Read(new GXDLMSData("0.0.96.1.0.255"), 2).Length == 0)
    {
    throw new Exception("Device settings did not supply password and protected frame keys.");
}
settings.Password = [4, 5]; settings.BlockCipherKey = Enumerable.Repeat((byte)0x33, 16).ToArray();
apply.Invoke(null, [client, settings]);
if (!client.Password.SequenceEqual(settings.Password) || !client.Ciphering.BlockCipherKey.SequenceEqual(settings.BlockCipherKey))
    {
    throw new Exception("Fresh Device settings did not replace prior keys.");
}
apply.Invoke(null, [client, new GXDLMSSettings()]);
if (client.Password != null || client.Ciphering.BlockCipherKey != null || client.Ciphering.AuthenticationKey != null)
    {
    throw new Exception("Missing settings reused stale/default secrets.");
}
var create = typeof(GXAgentWorker).GetMethod("CreateDeviceClient", BindingFlags.Static | BindingFlags.NonPublic)!;
var operational = new GXDLMSSettings { Authentication = (byte)Authentication.Low, Password = [49, 50, 51, 52] };
var defaultTemplate = new GXDLMSSettings { Authentication = (byte)Authentication.None, ClientAddress = 16, UseLogicalNameReferencing = true };
var association = (GXDLMSSecureClient)create.Invoke(null, [operational, defaultTemplate, 1])!;
if (association.Authentication != Authentication.Low || !association.Password.SequenceEqual(operational.Password) ||
    !association.AARQRequest().Any(frame => frame.AsSpan().IndexOf(operational.Password) >= 0))
{
    throw new Exception("The association must send the device's LLS password even when template authentication differs.");
}
Console.WriteLine("PASS settings-based password/encrypted frames, replacement and missing-key clearing without key-management HTTP.");

sealed class RejectKeys : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
        => throw new Exception("Unexpected HTTP during DLMS frame construction: " + request.RequestUri);
}
