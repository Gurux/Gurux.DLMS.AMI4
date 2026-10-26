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

namespace Gurux.DLMS.AMI.Module;

/// <summary>Optional module observer for successfully dispatched SignalR messages.</summary>
public interface IGXAmiSignalRRecorder
{
    /// <summary>Records a successfully dispatched SignalR message and its addressed users.</summary>
    /// <remarks>Dispatch does not imply that clients acknowledged receipt.</remarks>
    /// <param name="method">SignalR client method invoked by the host.</param>
    /// <param name="arguments">Arguments sent to the client method.</param>
    /// <param name="recipients">User identifiers addressed by the notification.</param>
    /// <param name="cancellationToken">Token used to cancel recording.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    Task RecordSentAsync(string method, object?[] arguments, IReadOnlyList<string> recipients,
        CancellationToken cancellationToken);
}
