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
using Gurux.Common;
using System.Runtime.Serialization;
using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.DLMS.AMI.Shared.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel;

namespace Gurux.DLMS.AMI.Shared.Rest
{
    /// <summary>
    /// Get script group.
    /// </summary>
    public class GetScriptGroupResponse
    {
        /// <summary>
        /// Script group information.
        /// </summary>
        [IncludeSwagger(typeof(GXScript), nameof(GXScript.Id), nameof(GXScript.Name))]
        [IncludeSwagger(typeof(GXUserGroup), nameof(GXUserGroup.Id),
                nameof(GXUserGroup.Name))]
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public GXScriptGroup Item
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Get script group list.
    /// </summary>
    [DataContract]
    public class ListScriptGroups : IGXRequest<ListScriptGroupsResponse>
    {
        /// <summary>
        /// Start index.
        /// </summary>
        public int Index
        {
            get;
            set;

        }

        /// <summary>
        /// Amount of the script groups to retreave.
        /// </summary>
        public int Count
        {
            get;
            set;
        }

        /// <summary>
        /// Filter can be used to filter script groups.
        /// </summary>
        [ExcludeSwagger(typeof(GXScriptGroup),
           nameof(GXScriptGroup.Scripts),
           nameof(GXScriptGroup.UserGroups))]
        public GXScriptGroup? Filter
        {
            get;
            set;
        }


        /// <summary>
        /// Admin user can access groups from all users.
        /// </summary>
        /// <remarks>
        /// If true, groups from all users are retreaved, not just current user. 
        /// </remarks>
        public bool AllUsers
        {
            get;
            set;
        }

        /// <summary>
        /// Selected extra information.
        /// </summary>
        /// <remarks>
        /// This is reserved for later use.
        /// </remarks>
        public TargetType Select
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Get script groups response.
    /// </summary>
    [DataContract]
    public class ListScriptGroupsResponse
    {
        /// <summary>
        /// List of script groups.
        /// </summary>
        [DataMember]
        [ExcludeSwagger(typeof(GXScriptGroup),
           nameof(GXScriptGroup.Scripts),
           nameof(GXScriptGroup.UserGroups))]
        public GXScriptGroup[] ScriptGroups
        {
            get;
            set;
        }

        /// <summary>
        /// Total count of the script groups.
        /// </summary>
        [DataMember]
        public int Count
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Add new script group.
    /// </summary>
    [DataContract]
    public class AddScriptGroup : IGXRequest<AddScriptGroupResponse>
    {
        /// <summary>
        /// New script group(s).
        /// </summary>
        [DataMember]
        [IncludeSwagger(typeof(GXScript), nameof(GXScript.Id))]
        [IncludeSwagger(typeof(GXUserGroup), nameof(GXUserGroup.Id))]
        public GXScriptGroup[] ScriptGroups
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Add new script group response.
    /// </summary>
    [DataContract]
    public class AddScriptGroupResponse
    {
        /// <summary>
        /// New script group IDs.
        /// </summary>
        public Guid[]? Ids
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Remove script group.
    /// </summary>
    [DataContract]
    public class RemoveScriptGroup : IGXRequest<RemoveScriptGroupResponse>
    {
        /// <summary>
        /// Script group Ids to remove.
        /// </summary>
        [DataMember]
        public Guid[] Ids
        {
            get;
            set;
        }

        /// <summary>
        /// Items are removed from the database.
        /// </summary>
        /// <remarks>
        /// If false, the Removed date is set for the items, but items are kept on the database.
        /// </remarks>
        [DataMember]
        [Required]
        public bool Delete
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Remove script group response.
    /// </summary>
    [DataContract]
    public class RemoveScriptGroupResponse
    {
    }
}
