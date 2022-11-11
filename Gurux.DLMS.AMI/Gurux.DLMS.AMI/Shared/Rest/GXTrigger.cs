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
using System.ComponentModel;
using Gurux.DLMS.AMI.Shared.DTOs;

namespace Gurux.DLMS.AMI.Shared.Rest
{
    /// <summary>
    /// Get list from triggers.
    /// </summary>
    [DataContract]
    public class ListTriggers : IGXRequest<ListTriggersResponse>
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
        /// Amount of the triggers to retreave.
        /// </summary>
        public int Count
        {
            get;
            set;
        }

        /// <summary>
        /// Filter can be used to filter triggers.
        /// </summary>
        public GXTrigger? Filter
        {
            get;
            set;
        }
        /// <summary>
        /// Admin user can access triggers from all users.
        /// </summary>
        /// <remarks>
        /// If true, triggers for all users are retreaved, not just current user. 
        /// </remarks>
        public bool AllUsers
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Trigger items reply.
    /// </summary>
    [DataContract]
    [Description("Trigger items reply.")]
    public class ListTriggersResponse
    {
        /// <summary>
        /// List of trigger items.
        /// </summary>
        [Description("List of trigger items.")]
        [DataMember]
        public GXTrigger[] Triggers
        {
            get;
            set;
        }

        /// <summary>
        /// Total count of the triggerrs.
        /// </summary>
        [DataMember]
        [Description("Total count of the triggerrs.")]
        public int Count
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Update triggers.
    /// </summary>
    [DataContract]
    public class UpdateTrigger : IGXRequest<UpdateTriggerResponse>
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        public UpdateTrigger()
        {
            Triggers = new List<GXTrigger>();
        }

        /// <summary>
        /// Triggers to update.
        /// </summary>
        [DataMember]
        [Description("Triggers to update.")]
        public List<GXTrigger> Triggers
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Update triggers reply.
    /// </summary>
    [Description("Update triggers reply.")]
    [DataContract]
    public class UpdateTriggerResponse
    {
        /// <summary>
        /// New trigger identifiers.
        /// </summary>
        [DataMember]
        [Description("New trigger identifiers.")]
        public Guid[] TriggerIds
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Delete triggers.
    /// </summary>
    [DataContract]
    public class DeleteTrigger : IGXRequest<DeleteTriggerResponse>
    {
        /// <summary>
        /// Removed trigger identifiers.
        /// </summary>
        [DataMember]
        [Description("Removed trigger identifiers.")]
        public Guid[] TriggerIds
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Reply from Delete trigger.
    /// </summary>
    [DataContract]
    [Description("Reply from Delete trigger.")]
    public class DeleteTriggerResponse
    {
    }

    /// <summary>
    /// Refresh triggers.
    /// </summary>
    [DataContract]
    public class RefreshTrigger : IGXRequest<RefreshTriggerResponse>
    {
    }

    /// <summary>
    /// Reply from refresh triggers.
    /// </summary>
    [DataContract]
    public class RefreshTriggerResponse
    {
        /// <summary>
        /// True, if there are new triggers available.
        /// </summary>
        public bool NewItems
        {
            get;
            set;
        }
    }
}
