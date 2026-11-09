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

using Gurux.DLMS.AMI.Shared.DTOs.Authentication;
using Gurux.DLMS.AMI.Shared.Enums;
using Microsoft.AspNetCore.Authorization;
namespace Gurux.DLMS.AMI.Server.Internal
{
    /// <summary>
    /// Defines dynamic authorization requirements.
    /// </summary>
    /// <remarks>
    /// Constructor.
    /// </remarks>
    public sealed class GXAuthorizationRequirement(
        PolicyRequirementOperator @operator,
        IReadOnlyCollection<GXPolicyRequirement> requirements)
                : IAuthorizationRequirement
    {

        /// <summary>
        /// Operator used between requirements.
        /// </summary>
        public PolicyRequirementOperator Operator
        {
            get;
        } = @operator;

        /// <summary>
        /// Policy requirements.
        /// </summary>
        public IReadOnlyCollection<GXPolicyRequirement> Requirements
        {
            get;
        } = requirements;
    }
}
