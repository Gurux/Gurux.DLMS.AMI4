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
using Gurux.Common.Db;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

namespace Gurux.DLMS.AMI.Shared.DTOs
{
    /// <summary>
    /// Device template settings.
    /// </summary>
    public class GXDeviceTemplate : GXTableBase, IUnique<Guid>
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        public GXDeviceTemplate()
        {
            Objects = new List<GXObjectTemplate>();
            DeviceTemplateGroups = new List<GXDeviceTemplateGroup>();
            Type = "";
            WaitTime = 5;
            ResendCount = 3;
            MediaType = "";
            MediaSettings = "";
        }

        /// <summary>
        /// Device template Id.
        /// </summary>
        public Guid Id
        {
            get;
            set;
        }

        /// <summary>
        /// Template type.
        /// </summary>
        public string Type
        {
            get;
            set;
        }

        /// <summary>
        /// This is default device group where new devices are added automatically when user creates them.
        /// </summary>
        [DataMember]
        [DefaultValue(false)]
        [Filter(FilterType.Exact)]
        [IsRequired]
        public bool? Default
        {
            get;
            set;
        }

        /// <summary>
        /// Name of the meter template.
        /// </summary>
        [StringLength(64)]
        [DataMember]
        [DefaultValue(null)]
        [Filter(FilterType.Contains)]
        public string? Name
        {
            get;
            set;
        }        

        /// <summary>
        /// Define how long reply is waited in seconds.
        /// </summary>
        [DefaultValue(5)]
        public int WaitTime
        {
            get;
            set;
        }

        /// <summary>
        /// Define re-send count.
        /// </summary>
        [DefaultValue(3)]
        public int ResendCount
        {
            get;
            set;
        }              

        /// <summary>
        /// Device settings.
        /// </summary>
        [DataMember]
        [DefaultValue(null)]
        public string? Settings
        {
            get;
            set;
        }

        /// <summary>
        /// Media type.
        /// </summary>
        [StringLength(64)]
        public virtual string MediaType
        {
            get;
            set;
        }

        /// <summary>
        /// Media settings as a string.
        /// </summary>
        public string MediaSettings
        {
            get;
            set;
        }
        
        /// <summary>
        /// Creation time.
        /// </summary>
        [Index(false, Descend = true)]
        [Filter(FilterType.GreaterOrEqual)]
        [IsRequired]
        public DateTime CreationTime
        {
            get;
            set;
        }

        /// <summary>
        /// When device template is updated.
        /// </summary>
        [Filter(FilterType.GreaterOrEqual)]
        public DateTimeOffset? Updated
        {
            get;
            set;
        }

        /// <summary>
        /// User has modified the item.
        /// </summary>
        [IgnoreDataMember]
        [Ignore]
        public bool Modified
        {
            get;
            set;
        }


        /// <summary>
        /// Concurrency stamp.
        /// </summary>
        /// <remarks>
        /// Concurrency stamp is used to verify that several user's can't 
        /// modify the target at the same time.
        /// </remarks>
        [DataMember]
        [StringLength(36)]
        public string? ConcurrencyStamp
        {
            get;
            set;
        }

        /// <summary>
        /// Remove time.
        /// </summary>
        [DataMember]
        [Index(false, Descend = true)]
        [DefaultValue(null)]
        [Filter(FilterType.Null)]
        public DateTimeOffset? Removed
        {
            get;
            set;
        }

        /// <summary>
        /// Objects.
        /// </summary>
        [Description("Object templates.")]
        [ForeignKey(typeof(GXObjectTemplate))]
        public List<GXObjectTemplate> Objects
        {
            get;
            set;
        }

        /// <summary>
        /// List of device template groups where this device template belongs.
        /// </summary>
        [DataMember]
        [ForeignKey(typeof(GXDeviceTemplateGroup), typeof(GXDeviceTemplateGroupDeviceTemplate))]
        public List<GXDeviceTemplateGroup> DeviceTemplateGroups
        {
            get;
            set;
        }

        /// <summary>
        /// Update creation time before update.
        /// </summary>
        public override void BeforeAdd()
        {
            if (CreationTime == DateTime.MinValue)
            {
                CreationTime = DateTime.Now;
            }
        }

        /// <summary>
        /// Update concurrency stamp.
        /// </summary>
        public override void BeforeUpdate()
        {
            Updated = DateTime.Now;
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            if (!string.IsNullOrEmpty(Name))
            {
                return Name;
            }
            return nameof(GXDeviceTemplate);
        }
    }
}
