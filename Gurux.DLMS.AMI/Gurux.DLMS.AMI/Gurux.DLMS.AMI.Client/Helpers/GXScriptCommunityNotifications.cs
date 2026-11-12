using System.Net.Http.Json;
using Gurux.DLMS.AMI.Client.Rest;
using Gurux.DLMS.AMI.Http;
using Gurux.UI.Components;
using Gurux.UI.Components.Enums;

namespace Gurux.DLMS.AMI.Client.Helpers;

/// <summary>Displays durable community review results using the application's global notifications.</summary>
public sealed class GXScriptCommunityNotifications(HttpClient http, IGXNotificationService notifications)
{
    private readonly SemaphoreSlim gate = new(1, 1);
    private readonly Dictionary<Guid, (string Status, GXNotificationItem Item)> tracked = [];
    private long generation;

    /// <summary>Sends only the saved script ID to the authenticated AMI server.</summary>
    public Task PublishAsync(Guid id, CancellationToken token = default)
        => PublishAsync("api/ScriptCommunity/Publish", new GXPublishCommunityScript { Id = id }, token);

    /// <summary>Submits a saved device template through its authorized server exporter.</summary>
    public Task PublishDeviceTemplateAsync(GXPublishCommunityDeviceTemplate request, CancellationToken token = default)
        => PublishAsync("api/DeviceTemplateCommunity/Publish", request, token);

    private async Task PublishAsync(string endpoint, object request, CancellationToken token)
    {
        var current = generation;
        var submission = await http.PostAsJson<GXScriptCommunitySubmission>(endpoint, request, token);
        await gate.WaitAsync(token);
        try
        {
            token.ThrowIfCancellationRequested();
            if (current == generation)
            {
                Observe(submission);
            }
        }
        finally { gate.Release(); }
    }

    /// <summary>Restores pending reviews and displays unseen decisions, including after reconnecting.</summary>
    public async Task RefreshAsync(CancellationToken token = default)
    {
        var current = generation;
        await gate.WaitAsync(token);
        try
        {
            var submissions = await http.GetFromJsonAsync<GXScriptCommunitySubmission[]>("api/ScriptCommunity", token) ?? [];
            token.ThrowIfCancellationRequested();
            if (current != generation) { return; }
            foreach (var submission in submissions)
            {
                token.ThrowIfCancellationRequested();
                if (current != generation) { return; }
                Observe(submission);
                if (submission.Status is "Approved" or "Rejected" or "Failed")
                {
                    // Acknowledge only after the outcome has been placed in the user's notification list.
                    using var response = await http.PostAsJsonAsync("api/ScriptCommunity/Acknowledge", new GXPublishCommunityScript { Id = submission.Id }, token);
                    await response.EnsureSuccessAsync(token);
                }
            }
        }
        finally { gate.Release(); }
    }

    private void Observe(GXScriptCommunitySubmission submission)
    {
        if (submission.Id == Guid.Empty || submission.Status is not ("Pending" or "Publishing" or "Approved" or "Rejected" or "Failed"))
        {
            return;
        }
        if (tracked.TryGetValue(submission.Id, out var previous) &&
            (previous.Status == submission.Status || previous.Status is "Approved" or "Rejected"))
        {
            return;
        }
        var (level, description) = submission.Status switch
        {
            "Approved" => (NotificationLevel.Success, string.Format(Properties.Resources.CommunityScriptApproved, submission.Version ?? "")),
            "Rejected" => (NotificationLevel.Warning, Properties.Resources.CommunityScriptRejected),
            "Failed" => (NotificationLevel.Error, submission.Message ?? Properties.Resources.CommunityPublishFailed),
            _ => (NotificationLevel.Information, Properties.Resources.CommunityReviewPending)
        };
        if (submission.ContentType == "DeviceTemplate")
        {
            description = submission.Status switch
            {
                "Approved" => Properties.Resources.CommunityDeviceTemplateApproved,
                "Rejected" => Properties.Resources.CommunityDeviceTemplateRejected,
                "Failed" => submission.Message ?? Properties.Resources.CommunityPublishFailed,
                _ => Properties.Resources.CommunityDeviceTemplatePending
            };
        }
        var title = submission.ScriptName + ": " + (submission.Status switch
        {
            "Approved" => Properties.Resources.CommunityApprovedTitle,
            "Rejected" => Properties.Resources.CommunityRejectedTitle,
            "Failed" => Properties.Resources.CommunityFailedTitle,
            _ => Properties.Resources.CommunityPendingTitle
        });
        var item = previous.Item == null
            ? new GXNotificationItem(level, title, description,
                "script-community:" + submission.Id.ToString("D"), true, NotificationScope.Global)
            : previous.Item with { Level = level, Title = title, Description = description };
        tracked[submission.Id] = (submission.Status, item);
        notifications.Add(item);
    }

    /// <summary>Clears user-specific in-memory feedback when the authenticated layout is removed.</summary>
    public void Reset()
    {
        Interlocked.Increment(ref generation);
        foreach (var value in tracked.Values)
        {
            notifications.Remove(value.Item.Id);
        }
        tracked.Clear();
    }
}
