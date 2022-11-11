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

using Gurux.Common.Db;

namespace Gurux.DLMS.AMI.Script
{
    /// <summary>
    /// This class contains information from the script log.
    /// </summary>
    public class GXScriptException
    {
        /// <summary>
        /// Error identifier.
        /// </summary>
        [Filter(FilterType.Exact)]
        public string Id
        {
            get;
            set;
        }

        /// <summary>
        /// Line number where error has occurred.
        /// </summary>
        public int Line
        {
            get;
            set;
        }

        /// <summary>
        /// Occurred error.
        /// </summary>
        [Filter(FilterType.Contains)]
        public string Message
        {
            get;
            set;
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        public GXScriptException()
        {
            Id = "";
            Message = "";
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return string.Format("ID: {0} Line: {1}, {2}", Id, Line, Message);
        }
    }
}
