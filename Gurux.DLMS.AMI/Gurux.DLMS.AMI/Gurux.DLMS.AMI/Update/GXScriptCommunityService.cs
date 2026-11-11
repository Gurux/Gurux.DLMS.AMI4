using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Gurux.DLMS.AMI.Shared.DIs;
using Gurux.DLMS.AMI.Shared.DTOs.Enums;
using Gurux.DLMS.AMI.Client.Rest;
using Microsoft.AspNetCore.DataProtection;

namespace Gurux.DLMS.AMI.Update;

/// <summary>Anonymous community publishing with durable owner-only receipts.</summary>
public sealed class GXScriptCommunityService(IGXScriptCommunityStore store, IServiceProvider services,
    IHttpClientFactory clients, IDataProtectionProvider protection, ILogger<GXScriptCommunityService> logger)
{
    /// <summary>Manages durable community submissions.</summary>
    public const string RemoteBase = "https://ami.gurux.fi/api/modules/Gurux.AMI.ScriptManagement/";
    private static readonly SemaphoreSlim Gate = new(1, 1);
    private readonly IDataProtector protector = protection.CreateProtector("Gurux.AMI.ScriptCommunity.v1");
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    private sealed record Receipt(Guid Id, string? Kind, string? Message, string? TrackingToken);
    private sealed record RemoteStatus(Guid Id, string Status, string? Version);

    /// <summary>Returns only the requesting owner’s unacknowledged receipts.</summary>
    public async Task<List<GXScriptCommunitySubmission>> ListAsync(string owner, CancellationToken token)
        => (await store.ReadAsync(token)).Where(row => row.OwnerId == owner && !row.Acknowledged).OrderByDescending(row => row.Created).Select(row => row.ToResponse()).ToList();

    /// <summary>Reads authorized persisted source and submits it anonymously.</summary>
    public async Task<GXScriptCommunitySubmission> PublishAsync(string owner, Guid id, CancellationToken token)
    {
        // Repository construction requires an authenticated request; receipt polling runs without HttpContext.
        var repository = services.GetRequiredService<IScriptRepository>();
        var script = await repository.ReadAsync(id, null, token);
        if (script.Removed != null || script.Language != ScriptLanguage.CSharp || string.IsNullOrWhiteSpace(script.SourceCode))
        {
            throw new ArgumentException("Select an available C# script with source code.");
        }
        var bytes = Encoding.UTF8.GetBytes(script.SourceCode);
        if (bytes.Length > 64 * 1024)
        {
            throw new ArgumentException("Community scripts must contain at most 64 KiB of UTF-8 source.");
        }
        if (!string.IsNullOrWhiteSpace(script.Namespaces))
        {
            var image = repository.Compile(script.Id.ToString("N"), script.SourceCode, null, [], out var errors, out _, token);
            if (image == null || image.Length == 0 || !string.IsNullOrWhiteSpace(errors))
            {
                throw new ArgumentException("Community templates do not carry additional namespace settings. Use fully qualified type names before publishing.");
            }
        }
        var hash = Convert.ToHexString(SHA256.HashData(bytes));
        await Gate.WaitAsync(token);
        try
        {
            var existing = (await store.ReadAsync(token)).FirstOrDefault(row => row.OwnerId == owner && row.ScriptId == id && row.SourceHash == hash);
            if (existing != null && existing.Status != "Failed")
            {
                return existing.ToResponse();
            }
            var identifier = script.CatalogId;
            if (string.IsNullOrWhiteSpace(identifier))
            {
                identifier = Regex.Replace(script.Name ?? "", "[^A-Za-z0-9_.-]", "_").Trim('_', '.', '-');
                if (identifier.Length == 0 || identifier.Length > 150)
                {
                    identifier = "Script-" + id.ToString("N");
                }
            }
            var stem = Regex.Replace(script.Name ?? "", @"\.csx?$", "", RegexOptions.IgnoreCase);
            stem = Regex.Replace(stem, "[^A-Za-z0-9_.-]", "_").Trim('_', '.', '-');
            if (stem.Length == 0 || stem.Length > 180)
            {
                stem = "Script";
            }
            if (!Regex.IsMatch(stem, "^[A-Za-z_]") || Regex.IsMatch(stem, @"^(CON|PRN|AUX|NUL|COM[1-9]|LPT[1-9])($|\.)", RegexOptions.IgnoreCase))
            {
                stem = "Script-" + stem;
            }
            var fileName = stem + ".cs";
            var request = JsonSerializer.Serialize(new
            {
                scriptId = identifier, fileName, source = script.SourceCode,
                description = string.IsNullOrWhiteSpace(script.Description) ? script.Name : script.Description,
                author = "Anonymous", license = "GPL-2.0-only", amiVersionRange = "[4.1,5.0)", baseVersion = script.CatalogVersion
            }, Json);
            var row = existing ?? new GXScriptCommunityRecord
            {
                Id = Guid.NewGuid(), OwnerId = owner, ScriptId = id, SourceHash = hash,
                ScriptName = (script.Name ?? identifier)[..Math.Min((script.Name ?? identifier).Length, 200)],
                Created = DateTimeOffset.UtcNow
            };
            row.RemoteId = Guid.Empty;
            row.ProtectedToken = null;
            row.Version = null;
            row.Status = "Publishing";
            row.Acknowledged = false;
            row.FailureMessage = null;
            await store.SaveAsync(row, existing == null, token);
            await RefreshOneAsync(row, token, request);
            return row.ToResponse();
        }
        finally
        {
            Gate.Release();
        }
    }

    /// <summary>Acknowledges a terminal outcome belonging to the requesting owner.</summary>
    public async Task AcknowledgeAsync(string owner, Guid id, CancellationToken token)
    {
        await Gate.WaitAsync(token);
        try
        {
            var row = (await store.ReadAsync(token)).FirstOrDefault(row => row.OwnerId == owner && row.Id == id);
            if (row != null && row.Status is "Approved" or "Rejected" or "Failed")
            {
                row.Acknowledged = true;
                await store.SaveAsync(row, false, token);
            }
        }
        finally
        {
            Gate.Release();
        }
    }

    /// <summary>Refreshes known receipts without resending ambiguous publications.</summary>
    public async Task RefreshAsync(CancellationToken token)
    {
        await Gate.WaitAsync(token);
        try
        {
            foreach (var row in (await store.ReadAsync(token)).Where(row => row.Status is "Publishing" or "Pending"))
            {
                token.ThrowIfCancellationRequested();
                if (row.RemoteId == Guid.Empty)
                {
                    // A process restart may follow remote acceptance. Never resubmit an ambiguous send automatically.
                    row.Status = "Failed";
                    await store.SaveAsync(row, false, token);
                }
                else
                {
                    await RefreshOneAsync(row, token);
                }
            }
        }
        finally
        {
            Gate.Release();
        }
    }

    private async Task RefreshOneAsync(GXScriptCommunityRecord row, CancellationToken token, string? sourceRequest = null)
    {
        try
        {
            using var client = clients.CreateClient("ScriptCommunity");
            if (row.RemoteId == Guid.Empty)
            {
                using var request = new HttpRequestMessage(HttpMethod.Post, RemoteBase + "submissions")
                {
                    Content = new StringContent(sourceRequest ?? throw new InvalidOperationException("Explicit publication is required."), Encoding.UTF8, "application/json")
                };
                var receipt = await SendAsync<Receipt>(client, request, token);
                if (receipt.Id == Guid.Empty || string.IsNullOrWhiteSpace(receipt.TrackingToken) || receipt.TrackingToken.Length > 4096)
                {
                    throw new InvalidDataException("Invalid community submission receipt.");
                }
                row.RemoteId = receipt.Id;
                row.ProtectedToken = protector.Protect(receipt.TrackingToken);
                row.Status = "Pending";
            }
            else
            {
                using var request = new HttpRequestMessage(HttpMethod.Post, RemoteBase + "submission-status")
                {
                    Content = JsonContent.Create(new { id = row.RemoteId, trackingToken = protector.Unprotect(row.ProtectedToken!) })
                };
                var status = await SendAsync<RemoteStatus>(client, request, token);
                if (status.Id != row.RemoteId || status.Status is not ("Pending" or "Publishing" or "Approved" or "Rejected") || status.Version?.Length > 64)
                {
                    throw new InvalidDataException("Invalid community submission status.");
                }
                row.Status = status.Status;
                row.Version = status.Version;
                if (row.Status is "Approved" or "Rejected")
                {
                    row.ProtectedToken = null;
                }
            }
            row.Checked = DateTimeOffset.UtcNow;
            await store.SaveAsync(row, false, sourceRequest != null ? CancellationToken.None : token);
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception error)
        {
            // Remote bodies and bearer receipts may contain secrets; never log them.
            logger.LogWarning("Community submission {Id} refresh failed ({ErrorType}).", row.Id, error.GetType().Name);
            if (row.RemoteId == Guid.Empty || error is CryptographicException ||
                error is HttpRequestException { StatusCode: System.Net.HttpStatusCode.NotFound })
            {
                row.Status = "Failed";
                if (row.RemoteId != Guid.Empty)
                {
                    row.FailureMessage = error is CryptographicException
                        ? "The saved community receipt cannot be decrypted. Restore the server data protection keys or contact the community administrator before submitting again."
                        : "The community review receipt is unavailable or has expired. Check with the community administrator before submitting again.";
                }
                row.ProtectedToken = null;
                await store.SaveAsync(row, false, token);
            }
        }
    }

    private static async Task<T> SendAsync<T>(HttpClient client, HttpRequestMessage request, CancellationToken token)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(token);
        timeout.CancelAfter(TimeSpan.FromSeconds(20));
        token = timeout.Token;
        using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, token);
        response.EnsureSuccessStatusCode();
        await response.Content.LoadIntoBufferAsync(16 * 1024, token);
        return await response.Content.ReadFromJsonAsync<T>(Json, token) ?? throw new InvalidDataException("Empty community response.");
    }
}
