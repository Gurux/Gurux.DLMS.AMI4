using System.ComponentModel.DataAnnotations;
using Gurux.DLMS.AMI.Services.Database;
using Gurux.DLMS.AMI.Client.Rest;
using Gurux.Service.Orm;
using Gurux.Service.Orm.Common;
using Gurux.Service.Orm.Model;

namespace Gurux.DLMS.AMI.Update;

/// <summary>Private, durable submission state. Secrets never form part of API responses.</summary>
[IndexCollection(true, nameof(OwnerId), nameof(ScriptId), nameof(SourceHash))]
public sealed class GXScriptCommunityRecord : IUnique<Guid>
{
    /// <summary>Local durable submission identifier.</summary>
    public Guid Id { get; set; }
    /// <summary>Authenticated submitting user; never sent to the community service.</summary>
    [StringLength(450)] public string OwnerId { get; set; } = "";
    /// <summary>Persisted local script identifier.</summary>
    public Guid ScriptId { get; set; }
    /// <summary>SHA-256 used to suppress duplicate submissions.</summary>
    [StringLength(64)] public string SourceHash { get; set; } = "";
    /// <summary>Display name retained for notifications.</summary>
    [StringLength(200)] public string ScriptName { get; set; } = "";
    /// <summary>Durable publishing or review status.</summary>
    [StringLength(32)] public string Status { get; set; } = "Publishing";
    /// <summary>Approved catalog version, when available.</summary>
    [StringLength(64)] public string? Version { get; set; }
    /// <summary>Remote submission identifier.</summary>
    public Guid RemoteId { get; set; }
    /// <summary>Data-protected opaque receipt credential; never returned to clients.</summary>
    public string? ProtectedToken { get; set; }
    /// <summary>Sanitized actionable explanation for a failed receipt.</summary>
    [StringLength(500)] public string? FailureMessage { get; set; }
    /// <summary>Whether the owner has received the terminal notification.</summary>
    public bool Acknowledged { get; set; }
    /// <summary>Creation time of the local submission.</summary>
    public DateTimeOffset Created { get; set; }
    /// <summary>Time the receipt was last refreshed.</summary>
    public DateTimeOffset? Checked { get; set; }
    /// <summary>Projects only public submission metadata.</summary>
    public GXScriptCommunitySubmission ToResponse() => new()
    {
        Id = Id, ScriptName = ScriptName, Status = Status, Version = Version,
        Message = Status == "Failed" ? FailureMessage ?? "The submission may have arrived, but its receipt could not be confirmed. Check with the community administrator before submitting again." : null
    };
}

/// <summary>Durable storage boundary for private submission records.</summary>
public interface IGXScriptCommunityStore
{
    /// <summary>Reads durable submission receipts.</summary>
    Task<List<GXScriptCommunityRecord>> ReadAsync(CancellationToken token);
    /// <summary>Persists a new or updated private receipt.</summary>
    Task SaveAsync(GXScriptCommunityRecord record, bool insert, CancellationToken token);
}

/// <summary>Stores receipts and send intents in the normal AMI database.</summary>
public sealed class GXScriptCommunityStore(IGXDatabase database) : IGXScriptCommunityStore
{
    private static readonly SemaphoreSlim Gate = new(1, 1);
    /// <summary>Reads durable submission receipts.</summary>
    public async Task<List<GXScriptCommunityRecord>> ReadAsync(CancellationToken token)
    {
        await Gate.WaitAsync(token);
        try
        {
            await using var db = new GXDbConnection(await database.CreateOpenConnectionAsync(), null);
            EnsureTable(db);
            return await db.SelectAsync<GXScriptCommunityRecord>(GXSelectArgs.SelectAll<GXScriptCommunityRecord>(), token);
        }
        finally
        {
            Gate.Release();
        }
    }
    /// <summary>Persists a new or updated private receipt.</summary>
    public async Task SaveAsync(GXScriptCommunityRecord record, bool insert, CancellationToken token)
    {
        await Gate.WaitAsync(token);
        try
        {
            await using var db = new GXDbConnection(await database.CreateOpenConnectionAsync(), null);
            EnsureTable(db);
            if (insert)
            {
                await db.InsertAsync(GXInsertArgs.Insert(record), token);
            }
            else
            {
                await db.UpdateAsync(GXUpdateArgs.Update(record), token);
            }
        }
        finally
        {
            Gate.Release();
        }
    }
    private static void EnsureTable(GXDbConnection db)
    {
        var schema = new GXSchemaManager(db);
        if (!schema.TableExist<GXScriptCommunityRecord>())
        {
            schema.CreateTable<GXScriptCommunityRecord>(false, false);
        }
    }
}
