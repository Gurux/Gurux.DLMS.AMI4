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
using Gurux.Service.Orm.Common;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

namespace Gurux.DLMS.AMI.Shared.DTOs.History
{
    /// <summary>Defines an entity type with a stable GUID and a name and controls whether its changes are recorded in change history.</summary>
    /// <remarks>The GUID is the persistent identity of the type. Renaming the type must not change its GUID.</remarks>
    [Description("Defines an entity type with a stable GUID and a name and controls whether its changes are recorded in change history.")]
    public sealed class GXChangeHistoryType : IUnique<Guid>
    {
        /// <summary>Gets or sets the stable GUID referenced by change history items to identify the entity type.</summary>
        [Description("Gets or sets the stable GUID referenced by change history items to identify the entity type.")]
        [Key]
        public Guid Id { get; set; }

        /// <summary>Gets or sets whether changes to entities of this type create change history items.</summary>
        /// <remarks>
        /// Defaults to false. Disabling recording does not prevent entity changes from being saved
        /// or the current entity revision from advancing. Existing history items are preserved.
        /// Enabling recording applies to subsequent changes; it does not record earlier changes retroactively.
        /// </remarks>
        [DefaultValue(false)]
        [Description("Whether changes to entities of this type create change history items. Defaults to false; entity updates and revision tracking remain active.")]
        public bool RecordHistory { get; set; }

        /// <summary>Gets or sets the name of the entity type, such as Device or User.</summary>
        [Description("Gets or sets the name of the entity type, such as Device or User.")]
        [IsRequired]
        [StringLength(128), Index(false)]
        public string? Name { get; set; }

        /// <summary>Gets or sets the change history items that reference this entity type.</summary>
        [Description("Gets or sets the change history items that reference this entity type.")]
        [DataMember, ForeignKey(typeof(GXChangeHistoryItem))]
        public List<GXChangeHistoryItem>? Items { get; set; }
    }
}
