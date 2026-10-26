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

namespace Gurux.DLMS.AMI.Shared.DIs.Enums
{
    /// <summary>Determines how an import handles data that already exists.</summary>
    public enum DataExchangeRule
    {
        /// <summary>Keep the existing data and ignore the imported replacement.</summary>
        Keep,
        /// <summary>Replace the existing data with the imported data.</summary>
        Replace,
        /// <summary>Create a separate copy when the imported data already exists.</summary>
        Copy,
        /// <summary>Reject the import when the data already exists.</summary>
        Error
    };
}
