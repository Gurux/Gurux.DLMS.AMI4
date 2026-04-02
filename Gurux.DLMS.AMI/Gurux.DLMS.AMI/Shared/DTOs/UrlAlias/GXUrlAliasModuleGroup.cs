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
// This file is a part of Gurux Module Framework.
//
// Gurux Module Framework is Open Source software; you can redistribute it
// and/or modify it under the terms of the GNU General Public License
// as published by the Free Software Foundation; version 2 of the License.
// Gurux Module Framework is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.
// See the GNU General Public License for more details.
//
// This code is licensed under the GNU General Public License v2.
// Full text may be retrieved at http://www.gnu.org/licenses/gpl-2.0.txt
//---------------------------------------------------------------------------
using Gurux.Service.Orm.Common;
using Gurux.Service.Orm.Common.Enums;
using Gurux.DLMS.AMI.Shared.DTOs.Module;
using System.Runtime.Serialization;

namespace Gurux.DLMS.AMI.Shared.DTOs.UrlAlias
{
    /// <summary>
    /// A data contract class representing url alias to module group binding object.
    /// </summary>
    [IndexCollection(true, nameof(ModuleGroupId), nameof(UrlAliasId), Clustered = true)]
    public class GXUrlAliasModuleGroup
    {
        /// <summary>
        /// Module group ID.
        /// </summary>
        [DataMember]
        [ForeignKey(typeof(GXModuleGroup), OnDelete = ForeignKeyDelete.Cascade)]
        [IsRequired]
        public Guid ModuleGroupId
        {
            get;
            set;
        }

        /// <summary>
        /// Url alias ID.
        /// </summary>
        [DataMember]
        [ForeignKey(typeof(GXUrlAlias), OnDelete = ForeignKeyDelete.Cascade)]
        [IsRequired]
        public Guid UrlAliasId
        {
            get;
            set;
        }
    }
}
