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

namespace Gurux.DLMS.AMI.Shared
{
    /// <summary>
    /// Forbidden exception is used to tell that resource access fails.
    /// </summary>
    public class GXAMIForbiddenException : Exception
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        public GXAMIForbiddenException()
        {
            Message = "";
        }

        /// <inheritdoc />
        public new string Message
        {
            get;
            set;
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="message">Error message.</param>
        public GXAMIForbiddenException(string message) : base(message)
        {
            Message = message;
        }

        /// <summary>
        /// Creation time.
        /// </summary>
        public DateTime CreationTime { get; set; }
        /// <summary>
        /// Message ID on the database.
        /// </summary>
        public Guid Id { get; set; }
    }
}
