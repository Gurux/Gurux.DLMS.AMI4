using System.Security.Claims;
using System.Text.Json.Nodes;
using Gurux.DLMS.AMI.Client.Shared.KeyManagement;
using Gurux.DLMS.AMI.Server.Repository;
using Gurux.DLMS.AMI.Server.Services.KeyManagement;
using Gurux.DLMS.AMI.Shared.DTOs.Device;
using Gurux.DLMS.AMI.Shared.DTOs.Enums;
using Gurux.DLMS.AMI.Shared.DTOs.KeyManagement;
using Gurux.DLMS.AMI.Shared.Enums;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ModuleRuntime.Tests;

[TestClass]
public sealed class PasswordRotationTests
{
    [TestMethod]
    public async Task PendingAndArchivedPasswordsDoNotDisplaceTheCurrentPassword()
    {
        var current = new GXKeyManagementKey { Id = Guid.NewGuid(), KeyType = KeyManagementType.LLSPassword, Key = [1, 2, 3] };
        GXKeyPolicy.InitializeLegacy(current);
        var next = current.ToMetadata();
        next.Id = Guid.NewGuid();
        next.State = KeyState.PreActive;
        var previous = current.ToMetadata();
        previous.Id = Guid.NewGuid();
        previous.State = KeyState.Deactivated;
        var actor = new ClaimsPrincipal(new ClaimsIdentity([new Claim("Permission", GXKeyPermissions.Use)], "test"));
        var device = new GXDevice { Settings = System.Text.Json.JsonSerializer.Serialize(new GXDLMSSettings { Authentication = 1 }) };
        var acquired = new List<Guid>();
        await GXDeviceSettingsKeys.PopulateAsync(actor, device,
            [new GXKeyManagement { Keys = [previous, current.ToMetadata(), next] }], false,
            (key, _, _) => { acquired.Add(key.Id); return Task.FromResult(new byte[] { 1, 2, 3 }); });
        CollectionAssert.AreEqual(new[] { current.Id }, acquired);
    }

    [TestMethod]
    public void KeyManagementHistoryRetainsKeyIdentitiesAndStatesButNeverMaterial()
    {
        var id = Guid.NewGuid();
        var management = new GXKeyManagement
        {
            Id = Guid.NewGuid(),
            Keys = [new GXKeyManagementKey { Id = id, KeyType = KeyManagementType.LLSPassword, State = KeyState.Active, Key = [83, 69, 67, 82, 69, 84] }]
        };
        var json = GXHistorySnapshot.Serialize(management);
        var snapshot = JsonNode.Parse(json)!;
        Assert.AreEqual(id.ToString(), snapshot["Keys"]![0]!["Id"]!.GetValue<string>());
        Assert.IsNull(snapshot["Keys"]![0]!["Key"]);
        Assert.IsFalse(json.Contains("U0VDUkVU", StringComparison.Ordinal));
        StringAssert.Contains(json, "State");
    }
}