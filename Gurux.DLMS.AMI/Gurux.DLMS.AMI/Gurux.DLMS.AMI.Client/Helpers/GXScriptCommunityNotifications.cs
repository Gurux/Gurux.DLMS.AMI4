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

using System.Net.Http.Json;
using Gurux.DLMS.AMI.Client.Rest;
using Gurux.DLMS.AMI.Http;
using Gurux.UI.Components;
using Gurux.UI.Components.Enums;

namespace Gurux.DLMS.AMI.Client.Helpers;

/// <summary>Displays durable community review results using the application's global notifications.</summary>
/// <param name="http">HTTP transport used to access the AMI API.</param>
/// <param name="notifications">Global notification list used to display durable submission outcomes.</param>
public sealed class GXScriptCommunityNotifications(HttpClient http, IGXNotificationService notifications)
{
    /// <summary>Serializes publishing and refresh operations that update community submission notifications.</summary>
    private readonly SemaphoreSlim _gate = new(1, 1);
    /// <summary>Most recently displayed status and notification for each community submission.</summary>
    private readonly Dictionary<Guid, (string Status, GXNotificationItem Item)> _tracked = [];
    /// <summary>Reset generation used to discard responses for a previous authenticated layout.</summary>
    private long _generation;

    /// <summary>Sends only the saved script ID to the authenticated AMI server.</summary>
    /// <param name="id">Identifier of the saved script to submit for community review.</param>
    /// <param name="token">Token that cancels publishing, refresh or acknowledgement work.</param>
    /// <returns>A task that completes after the submission result has been observed.</returns>
    public Task PublishAsync(Guid id, CancellationToken token = default)
        => PublishAsync("api/ScriptCommunity/Publish", new GXPublishCommunityScript { Id = id }, token);

    /// <summary>Submits a saved device template through its authorized server exporter.</summary>
    /// <param name="request">Saved script or device-template publication request sent to the authorized API.</param>
    /// <param name="token">Token that cancels publishing, refresh or acknowledgement work.</param>
    /// <returns>A task that completes after the server&apos;s publication result has been observed.</returns>
    public Task PublishDeviceTemplateAsync(GXPublishCommunityDeviceTemplate request, CancellationToken token = default)
        => PublishAsync("api/DeviceTemplateCommunity/Publish", request, token);

    /// <summary>Sends a community submission and records its notification while the layout generation remains current.</summary>
    /// <param name="endpoint">Authorized server endpoint used to publish the community submission.</param>
    /// <param name="request">Saved script or device-template publication request sent to the authorized API.</param>
    /// <param name="token">Token that cancels publishing, refresh or acknowledgement work.</param>
    /// <returns>A task that completes after the submission result has been observed.</returns>
    private async Task PublishAsync(string endpoint, object request, CancellationToken token)
    {
        var current = _generation;
        var submission = await http.PostAsJson<GXScriptCommunitySubmission>(endpoint, request, token);
        await _gate.WaitAsync(token);
        try
        {
            token.ThrowIfCancellationRequested();
            if (current == _generation)
            {
                Observe(submission);
            }
        }
        finally { _gate.Release(); }
    }

    /// <summary>Restores pending reviews and displays unseen decisions, including after reconnecting.</summary>
    /// <param name="token">Token that cancels publishing, refresh or acknowledgement work.</param>
    /// <returns>A task that completes after current submission states are displayed and completed decisions acknowledged.</returns>
    public async Task RefreshAsync(CancellationToken token = default)
    {
        var current = _generation;
        await _gate.WaitAsync(token);
        try
        {
            var submissions = await http.GetFromJsonAsync<GXScriptCommunitySubmission[]>("api/ScriptCommunity", token) ?? [];
            token.ThrowIfCancellationRequested();
            if (current != _generation) { return; }
            foreach (var submission in submissions)
            {
                token.ThrowIfCancellationRequested();
                if (current != _generation) { return; }
                Observe(submission);
                if (submission.Status is "Approved" or "Rejected" or "Failed")
                {
                    // Acknowledge only after the outcome has been placed in the user's notification list.
                    using var response = await http.PostAsJsonAsync("api/ScriptCommunity/Acknowledge", new GXPublishCommunityScript { Id = submission.Id }, token);
                    await response.EnsureSuccessAsync(token);
                }
            }
        }
        finally { _gate.Release(); }
    }

    /// <summary>Adds or updates the global notification for a recognized submission status without regressing final decisions.</summary>
    /// <param name="submission">Community submission whose latest status should be displayed.</param>
    private void Observe(GXScriptCommunitySubmission submission)
    {
        if (submission.Id == Guid.Empty || submission.Status is not ("Pending" or "Publishing" or "Approved" or "Rejected" or "Failed"))
        {
            return;
        }
        if (_tracked.TryGetValue(submission.Id, out var previous) &&
            (previous.Status == submission.Status || previous.Status is "Approved" or "Rejected"))
        {
            return;
        }
        var (level, description) = submission.Status switch
        {
            "Approved" => (NotificationLevel.Success, string.Format(System.Globalization.CultureInfo.CurrentCulture, Properties.Resources.CommunityScriptApproved, submission.Version ?? "")),
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
        var title = string.Format(System.Globalization.CultureInfo.CurrentCulture, submission.Status switch
        {
            "Approved" => Properties.Resources.CommunityPublicationApprovedForName,
            "Rejected" => Properties.Resources.CommunityPublicationRejectedForName,
            "Failed" => Properties.Resources.CommunitySubmissionFailedForName,
            _ => Properties.Resources.CommunityReviewPendingForName
        }, submission.ScriptName);
        var item = previous.Item == null
            ? new GXNotificationItem(level, title, description,
                "script-community:" + submission.Id.ToString("D"), true, NotificationScope.Global)
            : previous.Item with { Level = level, Title = title, Description = description };
        _tracked[submission.Id] = (submission.Status, item);
        notifications.Add(item);
    }

    /// <summary>Clears user-specific in-memory feedback when the authenticated layout is removed.</summary>
    public void Reset()
    {
        Interlocked.Increment(ref _generation);
        foreach (var value in _tracked.Values)
        {
            notifications.Remove(value.Item.Id);
        }
        _tracked.Clear();
    }
}
