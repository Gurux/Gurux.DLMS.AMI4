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

using System.Diagnostics.CodeAnalysis;
using Gurux.DLMS.AMI.Shared.DTOs;

namespace Gurux.DLMS.AMI.Server.Repository
{
    /// <summary>
    /// LocalizationComparer is used to compare localization strings.
    /// </summary>
    internal class LocalizationHashComparer : IEqualityComparer<GXLocalizedResource>
    {
        /// <summary>
        /// Check are values equal.
        /// </summary>
        public bool Equals(GXLocalizedResource? x, GXLocalizedResource? y)
        {
            if (x == null || y == null)
            {
                return x == y;
            }
            return x.Hash == y.Hash;
        }

        /// <summary>
        /// Get hash code for the object.
        /// </summary>
        public int GetHashCode([DisallowNull] GXLocalizedResource obj)
        {
            return obj.Hash;
        }
    }
}