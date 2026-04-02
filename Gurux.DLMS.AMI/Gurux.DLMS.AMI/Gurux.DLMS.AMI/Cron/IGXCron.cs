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

namespace Gurux.DLMS.AMI.Server.Cron
{
    /// <summary>
    /// This interface is used to run Cron service tasks.
    /// </summary>
    public interface IGXCronService
    {
        /// <summary>
        /// Run cron tasks.
        /// </summary>
        Task RunAsync();
    }

    /// <summary>
    /// Cron service uses this interface to find executed cron tasks.
    /// </summary>
    public interface IGXCronTask
    {
        /// <summary>
        /// Check new agent versions.
        /// </summary>
        Task CheckAgentsAsync();

        /// <summary>
        /// Check new modules.
        /// </summary>
        Task CheckModulesAsync();

        /// <summary>
        /// Check manufacturer settings.
        /// </summary>
        Task CheckManufacturersAsync();

        /// <summary>
        /// Cron calls to execute the task.
        /// </summary>
        /// <returns></returns>
        Task RunAsync();
    }

}
