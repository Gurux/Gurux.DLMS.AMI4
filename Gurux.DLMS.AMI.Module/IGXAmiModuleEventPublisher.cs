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

using Gurux.DLMS.AMI.Shared;
using System.Data;
using System.Security.Claims;
using System.Text.Json;
namespace Gurux.DLMS.AMI.Module;

/// <summary>Host-owned notification transport. Providers select authorized recipients.</summary>
public interface IGXAmiModuleEventPublisher
{
    /// <summary>Publishes a module target event to the authorized recipients.</summary>
    /// <param name="recipients">User identifiers selected and authorized by the calling provider.</param>
    /// <param name="moduleId">Identifier of the module that owns the target.</param>
    /// <param name="target">Module-defined target category.</param>
    /// <param name="targetId">Identifier of the affected target.</param>
    /// <param name="action">Module-defined action describing the change.</param>
    /// <param name="cancellationToken">Token used to cancel publication.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    Task PublishAsync(IReadOnlyList<string> recipients, string moduleId, string target, string targetId, string action, CancellationToken cancellationToken);
}
