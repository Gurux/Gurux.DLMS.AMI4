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
    /// <summary>Indicates that the current user cannot perform an AMI operation.</summary>
    public sealed class GXAmiForbiddenException : Exception
    {
        /// <summary>Initializes a denial without a specific policy requirement.</summary>
        public GXAmiForbiddenException() : this("Insufficient permissions.")
        {
        }

        /// <summary>Initializes a denial with a server-side diagnostic message.</summary>
        /// <param name="message">Diagnostic message.</param>
        public GXAmiForbiddenException(string? message) : base(message ?? "Insufficient permissions.")
        {
            RequiredPolicies = Array.AsReadOnly(Array.Empty<string>());
        }

        /// <summary>Initializes a denial when none of the alternative policies is satisfied.</summary>
        /// <param name="requiredPolicies">Policies of which at least one must be satisfied.</param>
        public GXAmiForbiddenException(IEnumerable<string> requiredPolicies) : this()
        {
            ArgumentNullException.ThrowIfNull(requiredPolicies);
            RequiredPolicies = Array.AsReadOnly(requiredPolicies.ToArray());
        }

        /// <summary>Gets an immutable snapshot of the alternative policies that were required.</summary>
        public IReadOnlyList<string> RequiredPolicies { get; }

        /// <inheritdoc />
        public override string Message => RequiredPolicies.Count == 0 ? base.Message :
            base.Message + " Required policy (any of): " + string.Join(", ", RequiredPolicies) + ".";
    }
}
