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
using Gurux.DLMS.AMI.Data;
using Gurux.DLMS.AMI.Shared.DTOs.Invite;
using Microsoft.AspNetCore.Mvc;

namespace Gurux.DLMS.AMI.Server.Controllers
{
    /// <summary>
    /// This controller is used to ask user to join selected user group.
    /// </summary>
    /// <param name="store"></param>
    /// <param name="cfg"></param>
    [ApiController]
    [Route("api/invites")]
    public class InvitesController(IInviteStore store, IConfiguration cfg) : ControllerBase
    {
        // TODO:
        private bool IsAdmin()
        {
            var key = cfg["AdminApiKey"];
            if (string.IsNullOrWhiteSpace(key)) return true;
            return Request.Headers.TryGetValue("x-api-key", out var given) && given.ToString() == key;
        }

        [HttpPost]
        public ActionResult<GXGroupInviteResponse> Create(GXGroupInviteRequest req)
        {
            if (!IsAdmin()) return Unauthorized();
            var ttl = TimeSpan.FromHours(Math.Min(req.ExpiresInHours ?? 24, 24));
            var inv = store.Create(req.GroupId, req.AllowedUserId, ttl);
            return Ok(new GXGroupInviteResponse(inv.Token, inv.GroupId, inv.ExpiresAtUtc, inv.AllowedUser, inv.Used));
        }

        [HttpGet("{token}")]
        public ActionResult<GXGroupInviteStatus> GetStatus(string token)
        {
            var inv = store.Get(token);
            if (inv is null) return NotFound();
            return Ok(new GXGroupInviteStatus(inv.GroupId, inv.ExpiresAtUtc, inv.AllowedUser, inv.Used));
        }

        [HttpPost("{token}/redeem")]
        public ActionResult<GXRedeemResult> Redeem(string token)
        {
            var currentUser = User.Identity?.Name;
            if (string.IsNullOrEmpty(currentUser))
                return Unauthorized(new GXRedeemResult(false, "Not authenticated."));

            var ok = store.TryRedeem(token, currentUser!, out var inv, out var msg);
            return ok ? Ok(new GXRedeemResult(true, msg)) : BadRequest(new GXRedeemResult(false, msg));
        }

        [HttpGet("groups/{groupId}/members")]
        public ActionResult<IEnumerable<string>> Members(string groupId)
            => Ok(store.Members(groupId));
    }
}
