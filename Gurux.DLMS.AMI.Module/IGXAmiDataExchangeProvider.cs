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

/// <summary>Exports and imports module target data for a user.</summary>
public interface IGXAmiDataExchangeProvider
{
    /// <summary>Exports the requested target records for the specified user.</summary>
    /// <param name="user">User whose access determines which records can be exported.</param>
    /// <param name="target">Module-defined category of records to export.</param>
    /// <param name="ids">Optional identifiers restricting the export to selected records.</param>
    /// <param name="cancellationToken">Token used to cancel the export.</param>
    /// <returns>The exported records as JSON values.</returns>
    Task<IReadOnlyList<JsonElement>> ExportAsync(ClaimsPrincipal user, string target, IReadOnlyList<Guid>? ids, CancellationToken cancellationToken);
    /// <summary>Imports target data using the specified rule and returns the number of imported records.</summary>
    /// <param name="user">User whose permissions govern the import.</param>
    /// <param name="target">Module-defined category of records to import.</param>
    /// <param name="data">JSON payload to import.</param>
    /// <param name="rule">Import rule interpreted by the provider.</param>
    /// <param name="cancellationToken">Token used to cancel the import.</param>
    /// <returns>The number of records imported by the provider.</returns>
    Task<int> ImportAsync(ClaimsPrincipal user, string target, JsonElement data, string rule, CancellationToken cancellationToken);
}
