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
