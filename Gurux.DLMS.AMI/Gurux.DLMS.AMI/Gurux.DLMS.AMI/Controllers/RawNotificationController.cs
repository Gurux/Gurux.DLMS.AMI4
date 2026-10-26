//
// --------------------------------------------------------------------------
//  Gurux Ltd
//
//
//
// Filename:        $HeadURL$
//
// Version:         $Revision$,
//                  $Date$
//                  $Author$
//
// Copyright (c) Gurux Ltd
//
//---------------------------------------------------------------------------
//
//  DESCRIPTION
//
// This file is a part of Gurux Device Framework.
//
// Gurux Device Framework is Open Source software; you can redistribute it
// and/or modify it under the terms of the GNU General Public License
// as published by the Free Software Foundation; version 2 of the License.
// Gurux Device Framework is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.
// See the GNU General Public License for more details.
//
// This code is licensed under the GNU General Public License v2.
// Full text may be retrieved at http://www.gnu.org/licenses/gpl-2.0.txt
//---------------------------------------------------------------------------
using Gurux.DLMS.AMI.Client.Shared;
using Gurux.DLMS.AMI.Shared.DIs;
using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.DLMS.AMI.Shared.Rest;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Gurux.DLMS.AMI.Server.Repository;

/// <summary>Stores raw notification chunks and coordinates deferred parsing through connection leases.</summary>
[Route("api/[controller]")]
[ApiController]
[Authorize]
public sealed class RawNotificationController : ControllerBase
{
    private readonly IRawNotificationRepository repository;

    /// <summary>Creates a controller for the authenticated raw notification repository.</summary>
    public RawNotificationController(IRawNotificationRepository rawNotificationRepository) => repository = rawNotificationRepository;

    private async Task<ActionResult<T>> ExecuteAsync<T>(Func<Task<T>> action)
    {
        try { return await action(); }
        catch (ArgumentException ex) { return BadRequest(ex.Message); }
        catch (UnauthorizedAccessException) { return Forbid(); }
        catch (KeyNotFoundException) { return NotFound(); }
        catch (InvalidOperationException ex) { return Conflict(ex.Message); }
    }

    /// <summary>Reads an accessible notification without reserving it.</summary>
    [HttpGet("{id:guid}")]
    [Authorize(Policy = GXRawNotificationPolicies.View)]
    public Task<ActionResult<GXRawNotification>> Get(Guid id, CancellationToken cancellationToken = default) =>
        ExecuteAsync(() => repository.ReadAsync(id, cancellationToken: cancellationToken));

    /// <summary>Lists accessible notifications with filtering and paging.</summary>
    [HttpPost("List")]
    [Authorize(Policy = GXRawNotificationPolicies.View)]
    public Task<ActionResult<ListRawNotificationsResponse>> Post(ListRawNotifications request, CancellationToken cancellationToken = default) =>
        ExecuteAsync(async () => {
            var response = new ListRawNotificationsResponse();
            await repository.ListAsync(request, response, cancellationToken);
            return response;
        });

    /// <summary>Stores received chunks, preserving their identifiers across delivery retries.</summary>
    [HttpPost("Add")]
    [Authorize(Policy = GXRawNotificationPolicies.Add)]
    public Task<ActionResult<AddRawNotificationResponse>> Post(AddRawNotification request, CancellationToken cancellationToken = default) =>
        ExecuteAsync(async () => {
            if (request.RawNotifications == null) throw new ArgumentException("Notifications are required.");
            return new AddRawNotificationResponse { Ids = await repository.AddAsync(request.RawNotifications, cancellationToken) };
        });

    /// <summary>Reserves the oldest eligible connection for a processing agent.</summary>
    [HttpPost("Next")]
    [Authorize(Policy = GXRawNotificationPolicies.Edit)]
    public Task<ActionResult<GetNextRawNotificationResponse>> Post(GetNextRawNotification request, CancellationToken cancellationToken = default) =>
        ExecuteAsync(async () => new GetNextRawNotificationResponse {
            RawNotifications = await repository.GetNextAsync(request.ProcessorId, request.LeaseDuration, request.ReceivingAgentId, cancellationToken)
        });

    /// <summary>Retrieves newly arrived and previously returned chunks under the current lease.</summary>
    [HttpPost("Leased")]
    [Authorize(Policy = GXRawNotificationPolicies.Edit)]
    public Task<ActionResult<GetLeasedRawNotificationsResponse>> Post(GetLeasedRawNotifications request, CancellationToken cancellationToken = default) =>
        ExecuteAsync(async () => new GetLeasedRawNotificationsResponse {
            RawNotifications = await repository.GetLeasedAsync(request.ProcessorId, request.LeaseToken, cancellationToken)
        });

    /// <summary>Extends an unexpired connection lease.</summary>
    [HttpPost("RenewLease")]
    [Authorize(Policy = GXRawNotificationPolicies.Edit)]
    public Task<ActionResult<RenewRawNotificationLeaseResponse>> Post(RenewRawNotificationLease request, CancellationToken cancellationToken = default) =>
        ExecuteAsync(async () => new RenewRawNotificationLeaseResponse {
            LeaseExpires = await repository.RenewLeaseAsync(request.ProcessorId, request.LeaseToken, request.LeaseDuration, cancellationToken)
        });

    /// <summary>Completes a closed connection after its device updates have been persisted.</summary>
    [HttpPost("Done")]
    [Authorize(Policy = GXRawNotificationPolicies.Edit)]
    public Task<ActionResult<RawNotificationDoneResponse>> Post(RawNotificationDone request, CancellationToken cancellationToken = default) =>
        ExecuteAsync(async () => {
            await repository.DoneAsync(request.ProcessorId, request.LeaseToken, cancellationToken);
            return new RawNotificationDoneResponse();
        });

    /// <summary>Records a parsing failure and optionally schedules a retry.</summary>
    [HttpPost("Fail")]
    [Authorize(Policy = GXRawNotificationPolicies.Edit)]
    public Task<ActionResult<RawNotificationFailedResponse>> Post(RawNotificationFailed request, CancellationToken cancellationToken = default) =>
        ExecuteAsync(async () => {
            await repository.FailAsync(request.ProcessorId, request.LeaseToken, request.Error, request.NextAttempt, cancellationToken);
            return new RawNotificationFailedResponse();
        });

    /// <summary>Deletes selected connections unless they have an active lease.</summary>
    [HttpPost("Delete")]
    [Authorize(Policy = GXRawNotificationPolicies.Delete)]
    public Task<ActionResult<DeleteRawNotificationResponse>> Post(DeleteRawNotification request, CancellationToken cancellationToken = default) =>
        ExecuteAsync(async () => {
            if (request.Ids == null) throw new ArgumentException("Notification identifiers are required.");
            await repository.DeleteAsync(request.Ids, cancellationToken);
            return new DeleteRawNotificationResponse();
        });

    /// <summary>Deletes completed connections older than the requested retention cutoff.</summary>
    [HttpPost("Clear")]
    [Authorize(Policy = GXRawNotificationPolicies.Clear)]
    public Task<ActionResult<ClearRawNotificationResponse>> Post(ClearRawNotification request, CancellationToken cancellationToken = default) =>
        ExecuteAsync(async () => {
            await repository.ClearAsync(request.ProcessedBefore, cancellationToken);
            return new ClearRawNotificationResponse();
        });
}