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

using System.Data;
namespace Gurux.DLMS.AMI.Services.Database;

/// <summary>Optional module-owned membership cleanup in the host's deletion transaction.</summary>
public interface IGXAmiMembershipLifecycle
{
    /// <summary>Removes module-owned membership references before the host deletes user groups.</summary>
    /// <remarks>The host owns the database and transaction; the provider must not dispose or complete them.</remarks>
    /// <param name="database">Host database instance used by the deletion operation.</param>
    /// <param name="transaction">Host transaction in which membership cleanup must run.</param>
    /// <param name="groupIds">Identifiers of the user groups being deleted.</param>
    /// <param name="cancellationToken">Token used to cancel cleanup.</param>
    Task UserGroupsDeletingAsync(IGXDatabase database, IDbTransaction transaction, IReadOnlyList<Guid> groupIds, CancellationToken cancellationToken);
}
