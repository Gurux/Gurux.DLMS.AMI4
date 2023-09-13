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

namespace Gurux.DLMS.AMI.Agent.Worker.AutoConnect
{
    class GXTaskNotification : IGXTaskNotification
    {
        Dictionary<Guid, AutoResetEvent> list = new Dictionary<Guid, AutoResetEvent>();

        public void Close()
        {
            lock (list)
            {
                foreach(var it in list)
                {
                    it.Value.Set();
                }
            }
        }

        public void Register(Guid id)
        {
            lock (list)
            {
                if (list.ContainsKey(id))
                {
                    //If new connection for the new agent.
                    list[id].Set();
                    list.Remove(id);
                }
                list.Add(id, new AutoResetEvent(false));
            }
        }

        public void Set(Guid id)
        {
            lock (list)
            {
                if (list.ContainsKey(id))
                {
                    list[id].Set();
                }
            }
        }

        public void Unregister(Guid id)
        {
            lock (list)
            {
                if (list.ContainsKey(id))
                {
                    list[id].Set();
                    list.Remove(id);
                }
            }
        }

        public bool Wait(Guid id)
        {
            AutoResetEvent? value = null;
            lock (list)
            {
                if (list.ContainsKey(id))
                {
                    value = list[id];
                }
            }
            if (value != null)
            {
                return value.WaitOne();
            }
            return false;
        }
    }
}
