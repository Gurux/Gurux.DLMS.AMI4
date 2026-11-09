using System.ComponentModel.DataAnnotations;
using System.Text.Json.Nodes;
using Gurux.DLMS.AMI.Module;
using Microsoft.Extensions.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Gurux.DLMS.AMI.Module.Tests;

[TestClass]
public sealed class TypedSettingsTests
{
    public sealed class Model
    {
        [Required]
        public string? Name { get; set; }
        [Range(1, 10)]
        public int Count { get; set; } = 1;
    }

    [TestMethod]
    public void SequentialMigrationValidatesAndPreservesHostMetadata()
    {
        var schema = new AmiModuleSettingsSchema<Model>(2, new Dictionary<int, Func<JsonObject, JsonObject>>
        {
            [0] = value => { value["Name"] = value["oldName"]!.GetValue<string>(); value.Remove("oldName"); return value; },
            [1] = value => { value["Count"] = 2; return value; }
        });
        var result = JsonNode.Parse(schema.Normalize("{\"oldName\":\"meter\",\"$state\":{\"Cursor\":9},\"$resources\":{\"Id\":1}}"))!;
        Assert.AreEqual("meter", result["Name"]!.GetValue<string>());
        Assert.AreEqual(2, result["Count"]!.GetValue<int>());
        Assert.AreEqual(2, result[AmiModuleSettingsSchema.VersionKey]!.GetValue<int>());
        Assert.AreEqual(9, result["$state"]!["Cursor"]!.GetValue<int>());
    }

    [TestMethod]
    public void RejectsValidationFutureVersionAndMissingMigration()
    {
        var schema = new AmiModuleSettingsSchema<Model>(1);
        Assert.ThrowsExactly<ValidationException>(() => schema.Normalize("{\"$schemaVersion\":1,\"Count\":0}"));
        Assert.ThrowsExactly<InvalidOperationException>(() => schema.Normalize("{\"$schemaVersion\":2,\"Name\":\"meter\"}"));
        Assert.ThrowsExactly<InvalidOperationException>(() => schema.Normalize("{\"Name\":\"meter\"}"));
    }

    [TestMethod]
    public void CustomValidationAndMigrationCannotReplaceHostState()
    {
        var schema = new AmiModuleSettingsSchema<Model>(1, new Dictionary<int, Func<JsonObject, JsonObject>>
        {
            [0] = value => new JsonObject { ["Name"] = "meter", ["$state"] = new JsonObject { ["Cursor"] = 100 } }
        }, value => { if (value.Name == "forbidden") { throw new ValidationException("Name is forbidden."); } });
        Assert.AreEqual(9, JsonNode.Parse(schema.Normalize("{\"$state\":{\"Cursor\":9}}"))!["$state"]!["Cursor"]!.GetValue<int>());
        Assert.ThrowsExactly<ValidationException>(() => schema.Normalize("{\"$schemaVersion\":1,\"Name\":\"forbidden\"}"));
    }
}
