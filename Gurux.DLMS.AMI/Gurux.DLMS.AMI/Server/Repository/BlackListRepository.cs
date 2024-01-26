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

using Gurux.DLMS.AMI.Server.Internal;
using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.DLMS.AMI.Shared.DIs;
using Gurux.Service.Orm;

namespace Gurux.DLMS.AMI.Server.Repository
{
    /// <inheritdoc/>
    public class BlackListRepository : IBlackListRepository
    {
        private readonly List<UInt64> blockedList;

        /// <summary>        
        /// Update blocked items in constructor.
        /// </summary>
        public BlackListRepository(IGXHost host)
        {
            try
            {
                GXSelectArgs args = GXSelectArgs.Select<GXIpAddress>(s => s.IPAddress, where => where.Blocked == true);
                List<GXIpAddress> address = host.Connection.Select<GXIpAddress>(args);
                blockedList = address.Select(s => s.IPAddress).ToList();
            }
            catch (Exception)
            {
                blockedList = new List<UInt64>();
            }
        }

        /// <inheritdoc/>
        public void Add(ulong address)
        {
            lock (blockedList)
            {
                blockedList.Add(address);
            }
        }

        /// <inheritdoc/>
        public void AddRange(IEnumerable<ulong> list)
        {
            lock (blockedList)
            {
                blockedList.AddRange(list);
            }
        }

        /// <inheritdoc/>
        public bool IsBlocked(ulong address)
        {
            lock (blockedList)
            {
                return blockedList.Contains(address);
            }
        }

        /// <inheritdoc/>
        public void Delete(ulong address)
        {
            lock (blockedList)
            {
                blockedList.Remove(address);
            }
        }

        /// <inheritdoc/>
        public void RemoveRange(IEnumerable<ulong> list)
        {
            lock (blockedList)
            {
                foreach (ulong address in list)
                {
                    blockedList.Remove(address);
                }
            }
        }
    }
}