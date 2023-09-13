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
    /// <summary>
    /// This interface is used to hanle task notifications for auto connection while 
    /// agent is connected for the gateway.
    /// </summary>
    interface IGXTaskNotification
    {
        /// <summary>
        /// Register the listener.
        /// </summary>
        /// <param name="id"></param>
        void Register(Guid id);

        /// <summary>
        /// Wait new task.
        /// </summary>
        /// <param name="id"></param>
        bool Wait(Guid id);

        /// <summary>
        /// New task has reveived for the agent or gateway.
        /// </summary>
        /// <param name="id"></param>
        void Set(Guid id);

        /// <summary>
        /// Unregister the listener
        /// </summary>
        /// <param name="id"></param>
        void Unregister(Guid id);

        /// <summary>
        /// Closes all tasks on close.
        /// </summary>
        void Close();

    }
}
