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

using Gurux.DLMS.AMI.Server;
using Gurux.DLMS.AMI.Server.Internal;
using System.Security.Claims;

/// <summary>
/// This interface is used to get calling user.
/// </summary>
public interface IGXAmiContextAccessor
{
    /// <summary>
    /// Current user.
    /// </summary>
    ClaimsPrincipal? User
    {
        get;
    }
}

/// <inheritdoc/>
public class GXAmiContextAccessor : IGXAmiContextAccessor
{
    /// <summary>
    /// Constructor.
    /// </summary>
    public GXAmiContextAccessor(IGXHost host,
        IHttpContextAccessor? httpContextAccessor)
    {
        var user = httpContextAccessor?.HttpContext?.User;
        if (user != null)
        {
            User = user;
        }
        else
        {
            User = ServerSettings.GetDefaultAdminUser(host);
        }
    }

    /// <inheritdoc/>
    public ClaimsPrincipal? User
    {
        get;
        private set;
    }
}