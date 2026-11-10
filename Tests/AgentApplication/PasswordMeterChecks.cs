using Task = System.Threading.Tasks.Task;
#pragma warning disable CS0672 // Implement the test server callbacks required by the pinned DLMS package.
using System.Diagnostics;
using System.Text;
using Gurux.Common;
using Gurux.DLMS;
using Gurux.DLMS.AMI.Agent.Worker;
using Gurux.DLMS.Enums;
using Gurux.DLMS.Objects;
using Gurux.DLMS.Secure;

internal static class PasswordMeterChecks
{
    internal static async Task RunAsync()
    {
        foreach (var scenario in new[] { "normal", "lost-intent-reply", "lost-write-reply", "lost-confirm-reply" })
        {
            byte[] oldPassword = Encoding.ASCII.GetBytes("old-LLS-secret");
            byte[] newPassword = Encoding.ASCII.GetBytes("new-LLS-secret");
            var server = new PasswordMeter(oldPassword);
            using var media = new PasswordMeterMedia(server) { DropNextWriteReply = scenario == "lost-write-reply" };
            using var http = new HttpClient();
            var client = new GXDLMSSecureClient(true, 16, 1, Authentication.Low, null, InterfaceType.WRAPPER) { Password = oldPassword };
            var reader = new GXDLMSReader(client, media, null, TraceLevel.Off, TraceLevel.Off, 1, 1, null, http);
            media.Open();
            try
            {
                await reader.InitializeConnection(false, false, null);
                var confirmed = false;
                var intent = false;
                try
                {
                    await GXPasswordChangeExecution.RunAsync(false,
                        () =>
                        {
                            intent = true;
                            if (scenario == "lost-intent-reply")
                            {
                                throw new IOException("Write-intent response lost before meter write.");
                            }
                            return Task.CompletedTask;
                        },
                        () =>
                        {
                            reader.Write(new GXDLMSAssociationLogicalName("0.0.40.0.0.255") { Secret = newPassword }, 7);
                            return Task.CompletedTask;
                        },
                        () => reader.ReconnectWithPasswordAsync(newPassword),
                        () =>
                        {
                            if (scenario == "lost-confirm-reply")
                            {
                                throw new IOException("Confirmation response lost.");
                            }
                            confirmed = true;
                            return Task.CompletedTask;
                        });
                    if (scenario != "normal")
                    {
                        throw new Exception("The lost response was not reported.");
                    }
                }
                catch (IOException) when (scenario == "lost-intent-reply" && intent && server.Writes == 0)
                {
                    try
                    {
                        await reader.ReconnectWithPasswordAsync(newPassword);
                        throw new Exception("The meter accepted an unwritten password.");
                    }
                    catch (GXDLMSException error) when (error.Diagnostic == (byte)SourceDiagnostic.AuthenticationFailure)
                    {
                        await reader.ReconnectWithPasswordAsync(oldPassword);
                    }
                    await GXPasswordChangeExecution.RunAsync(false, () => Task.CompletedTask,
                        () => { reader.Write(new GXDLMSAssociationLogicalName("0.0.40.0.0.255") { Secret = newPassword }, 7); return Task.CompletedTask; },
                        () => reader.ReconnectWithPasswordAsync(newPassword),
                        () => { confirmed = true; return Task.CompletedTask; });
                }
                catch (Exception) when (scenario != "normal" && intent && server.Writes == 1 && !confirmed)
                {
                    // Simulate process recovery: the new credential must be probed before
                    // another write. It succeeds even if the original write reply was lost.
                    await reader.ReconnectWithPasswordAsync(newPassword);
                    await GXPasswordChangeExecution.RunAsync(true,
                        () => throw new Exception("Repeated write intent."),
                        () => throw new Exception("Repeated meter write."),
                        () => throw new Exception("The verified association was discarded."),
                        () => { confirmed = true; return Task.CompletedTask; });
                }
                if (!confirmed || server.Writes != 1 || !server.Association.Secret.SequenceEqual(newPassword))
                {
                    throw new Exception("A real LLS association did not commit exactly one password write.");
                }
                // Recovery uses the currently working new credential to write the retained old one.
                await GXPasswordChangeExecution.RunAsync(false, () => Task.CompletedTask,
                    () => { reader.Write(new GXDLMSAssociationLogicalName("0.0.40.0.0.255") { Secret = oldPassword }, 7); return Task.CompletedTask; },
                    () => reader.ReconnectWithPasswordAsync(oldPassword), () => Task.CompletedTask);
                if (server.Writes != 2 || !server.Association.Secret.SequenceEqual(oldPassword))
                {
                    throw new Exception("Restoring the old LLS password did not reach the meter.");
                }
            }
            finally
            {
                reader.Close();
            }
            Console.WriteLine("PASS real DLMS LLS write, fresh authentication and password recovery: " + scenario);
        }
    }
}

internal sealed class PasswordMeter : GXDLMSSecureServer
{
    internal GXDLMSAssociationLogicalName Association { get; }
    internal int Writes;
    internal PasswordMeter(byte[] password) : base(true, InterfaceType.WRAPPER)
    {
        Association = new GXDLMSAssociationLogicalName("0.0.40.0.0.255") { Secret = password.ToArray(), ClientSAP = 16 };
        Association.AuthenticationMechanismName.MechanismId = Authentication.Low;
        Association.XDLMSContextInfo.Conformance = Conformance.Get | Conformance.Set;
        MaxReceivePDUSize = 1024;
        Association.XDLMSContextInfo.MaxReceivePduSize = 1024;
        Association.XDLMSContextInfo.MaxSendPduSize = 1024;
        Items.Add(Association);
        Conformance = Conformance.Get | Conformance.Set;
        Initialize();
    }
    protected override GXDLMSObject FindObject(ObjectType type, int sn, string ln) => Items.FindByLN(type, ln);
    protected override bool IsTarget(int serverAddress, int clientAddress)
    {
        AssignedAssociation = Association;
        return serverAddress == 1 && clientAddress == 16;
    }
    protected override AccessMode GetAttributeAccess(ValueEventArgs arg) => AccessMode.ReadWrite;
    protected override AccessMode3 GetAttributeAccess3(ValueEventArgs arg) => AccessMode3.Read | AccessMode3.Write;
    protected override MethodAccessMode GetMethodAccess(ValueEventArgs arg) => MethodAccessMode.Access;
    protected override MethodAccessMode3 GetMethodAccess3(ValueEventArgs arg) => MethodAccessMode3.Access;
    protected override SourceDiagnostic ValidateAuthentication(Authentication authentication, byte[] password)
        => authentication == Authentication.Low && password != null && password.SequenceEqual(Association.Secret)
            ? SourceDiagnostic.None : SourceDiagnostic.AuthenticationFailure;
    protected override void Connected(GXDLMSConnectionEventArgs info) { }
    protected override void InvalidConnection(GXDLMSConnectionEventArgs info) { }
    protected override void Disconnected(GXDLMSConnectionEventArgs info) { }
    public override void PreGet(ValueEventArgs[] args) { }
    public override void PostGet(ValueEventArgs[] args) { }
    protected override void PreRead(ValueEventArgs[] args) { }
    protected override void PreWrite(ValueEventArgs[] args) { }
    protected override void PreAction(ValueEventArgs[] args) { }
    protected override void PostRead(ValueEventArgs[] args) { }
    protected override void PostWrite(ValueEventArgs[] args)
    {
        foreach (var arg in args)
        {
            if (arg.Target == Association && arg.Index == 7 && arg.Error == ErrorCode.Ok)
            {
                Writes++;
            }
        }
    }
    protected override void PostAction(ValueEventArgs[] args) { }
    protected override void Execute(List<KeyValuePair<GXDLMSObject, int>> actions) { }
}

#pragma warning disable CS0067
internal sealed class PasswordMeterMedia(PasswordMeter server) : IGXMedia, IDisposable
{
    private byte[]? reply;
    internal bool DropNextWriteReply;
    public string Name => "password-test";
    public TraceLevel Trace { get; set; }
    public bool IsOpen { get; private set; }
    public string MediaType => GetType().FullName!;
    public bool Enabled => true;
    public string Settings { get; set; } = "";
    public object Synchronous { get; } = new();
    public bool IsSynchronous => false;
    public ulong BytesSent => 0;
    public ulong BytesReceived => 0;
    public object Eop { get; set; } = null!;
    public int ConfigurableSettings { get; set; }
    public object Tag { get; set; } = null!;
    public IGXMediaContainer MediaContainer { get; set; } = null!;
    public object SyncRoot => Synchronous;
    public event ReceivedEventHandler? OnReceived;
    public event Gurux.Common.ErrorEventHandler? OnError;
    public event MediaStateChangeEventHandler? OnMediaStateChange;
    public event ClientConnectedEventHandler? OnClientConnected;
    public event ClientDisconnectedEventHandler? OnClientDisconnected;
    public event TraceEventHandler? OnTrace;
    public void Copy(object target) { }
    public void Open() { server.Reset(); IsOpen = true; }
    public void Close() { IsOpen = false; reply = null; }
    public void Send(object data, string receiver)
    {
        var request = (byte[])data;
        var exchange = new GXServerReply(request);
        server.HandleRequest(exchange);
        reply = exchange.Reply;
        if (DropNextWriteReply && request.Length > 8 && request[8] == 0xC1)
        {
            DropNextWriteReply = false;
            reply = null;
        }
    }
    public bool Receive<T>(ReceiveParameters<T> args)
    {
        if (reply == null)
        {
            return false;
        }
        args.Reply = (T)(object)reply;
        reply = null;
        return true;
    }
    public void ResetSynchronousBuffer() { }
    public void ResetByteCounters() { }
    public void Validate() { }
    public void Dispose() { Close(); }
}
#pragma warning restore CS0067