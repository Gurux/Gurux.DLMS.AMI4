// --------------------------------------------------------------------------
// Gurux Ltd
//
// Copyright (c) Gurux Ltd.
//
// This file is part of Gurux Device Framework.
//
// Commit: $Format:%H$
// Date:   $Format:%cI$
//
// This software is available under either of the following licenses:
//
// 1. GNU General Public License, version 2 only (GPL-2.0-only).
//    See the LICENSE file included with this distribution.
//
// 2. A commercial license granted by Gurux Ltd under a separate written
//    agreement. Use under this option requires a valid commercial license
//    and is governed by the terms of that agreement.
//
// If you have not obtained a commercial license from Gurux Ltd,
// use of this software is governed by GPL-2.0-only.
//
// Commercial licensing: https://www.gurux.fi/
//
// SPDX-License-Identifier: GPL-2.0-only OR LicenseRef-Gurux-Commercial
// --------------------------------------------------------------------------

namespace Gurux.DLMS.AMI.Shared
{
    /// <summary>
    /// AMI error is used to send server errors.
    /// </summary>
    [System.ComponentModel.Description("AMI error is used to send server errors.")]
    public class GXAmiException : Exception
    {
        /// <summary>Initializes a new instance of GXAmiException.</summary>
        [System.ComponentModel.Description("Initializes a new instance of GXAmiException.")]
        public GXAmiException()
        {
            Message = "";
        }

        /// <inheritdoc />
        public new string Message { get; set; }

        /// <summary>Initializes a new instance of GXAmiException.</summary>
        /// <param name="message">Error message.</param>
        [System.ComponentModel.Description("Initializes a new instance of GXAmiException.")]
        public GXAmiException(string message) : base(message)
        {
            Message = message;
        }

        /// <summary>Time when the record was created.</summary>
        [System.ComponentModel.Description("Time when the record was created.")]
        public DateTime CreationTime { get; set; }
        /// <summary>Message identifier on the database.</summary>
        [System.ComponentModel.Description("Message identifier on the database.")]
        public Guid Id { get; set; }
    }
}
