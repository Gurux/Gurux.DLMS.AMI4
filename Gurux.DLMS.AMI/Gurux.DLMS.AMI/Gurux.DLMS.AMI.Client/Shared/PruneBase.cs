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

namespace Gurux.DLMS.AMI.Client.Shared
{
    /// <summary>
    /// Prune base settings.
    /// </summary>
    public class PruneBase
    {
        /// <summary>
        /// Prune interval in minutes.
        /// </summary>
        public int Interval
        {
            get;
            set;
        } = 0;

        /// <summary>
        /// Are items removed permamently.
        /// </summary>
        /// <remarks>
        /// If false, items are only disabled and not removed from the database. 
        /// </remarks>
        public bool Delete
        {
            get;
            set;
        }

        /// <summary>
        /// The maximum number of items to delete during a run.
        /// </summary>
        /// <remarks>
        /// If the value is zero, there are no restrictions.
        /// </remarks>
        public int Maximum
        {
            get;
            set;
        }

        /// <summary>
        /// last execution time.
        /// </summary>
        public DateTimeOffset? Run
        {
            get;
            set;
        }
    }
}