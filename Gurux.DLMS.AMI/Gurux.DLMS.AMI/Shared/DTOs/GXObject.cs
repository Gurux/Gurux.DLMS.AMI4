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
using System.Text.Json.Serialization;

namespace Gurux.DLMS.AMI.Shared.DTOs
{
    /// <summary>
    /// COSEM object.
    /// </summary>
    [IndexCollection(true, nameof(Device), nameof(Template))]
    public class GXObject : GXTableBase, IUnique<Guid>
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        public GXObject()
        {
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        public GXObject(GXObjectTemplate? template)
        {
            Template = template;
            Attributes = new();
            Parameters = new();
            Tasks = new();
            Errors = new List<GXObjectError>();
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        public GXObject(int objectType, string? logicalName)
        {
            Template = new GXObjectTemplate()
            {
                LogicalName = logicalName,
                ObjectType = objectType,
            };
            Attributes = new();
            Parameters = new();
            Tasks = new();
            Errors = new List<GXObjectError>();
        }

        /// <summary>
        /// Object Id.
        /// </summary>
        [DataMember]
        [Filter(FilterType.Exact)]
        public Guid Id
        {
            get;
            set;
        }

        /// <summary>
        /// Device identifier.
        /// </summary>
        [DataMember]
        [ForeignKey(OnDelete = ForeignKeyDelete.Cascade)]
        [Index(false)]
        [Filter(FilterType.Exact)]
        public GXDevice? Device
        {
            get;
            set;
        }


        /// <summary>
        /// Object Template.
        /// </summary>
        [DataMember]
        [ForeignKey(OnDelete = ForeignKeyDelete.None)]
        [Index(false)]
        [Filter(FilterType.Exact)]
        public GXObjectTemplate? Template
        {
            get;
            set;
        }

        /// <summary>
        /// Creation time.
        /// </summary>
        [DataMember]
        [Index(false, Descend = true)]
        [Filter(FilterType.GreaterOrEqual)]
        [IsRequired]
        public DateTime? CreationTime
        {
            get;
            set;
        }

        /// <summary>
        /// When object is last updated.
        /// </summary>
        [DataMember]
        [Description("When object is last updated.")]
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
        [JsonIgnore]
        public bool Modified
        {
            get;
            set;
        }

        /// <summary>
        /// Object is created only when needed.
        /// </summary>
        /// <remarks>
        /// If true, the object is not created for the database.
        /// It's created when needed for the first time.
        /// This makes faster to add new devices for the database.
        /// </remarks>
        [Ignore]
        public bool Latebind
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
        /// Object parameters.
        /// </summary>
        [DataMember]
        [ForeignKey]
        [Filter(FilterType.Contains)]
        public List<GXObjectParameter>? Parameters
        {
            get;
            set;
        }

        /// <summary>
        /// When the object's attribute were last read.
        /// </summary>
        [DataMember]
        [Description("When the object's attribute were last read.")]
        [Filter(FilterType.GreaterOrEqual)]
        public DateTimeOffset? LastRead
        {
            get;
            set;
        }

        /// <summary>
        /// When the object's attribute were last written.
        /// </summary>
        [DataMember]
        [Description("When the object's attribute were last written.")]
        [Filter(FilterType.GreaterOrEqual)]
        public DateTimeOffset? LastWrite
        {
            get;
            set;
        }

        /// <summary>
        /// When the object's actions were last invoked.
        /// </summary>
        [DataMember]
        [Description("When the object's actions were last invoked.")]
        [Filter(FilterType.GreaterOrEqual)]
        public DateTimeOffset? LastAction
        {
            get;
            set;
        }

        /// <summary>
        /// When the last error was occurred.
        /// </summary>
        [DataMember]
        [Description("When the last error was occurred.")]
        [Filter(FilterType.GreaterOrEqual)]
        public DateTimeOffset? LastError
        {
            get;
            set;
        }

        /// <summary>
        /// Last error message.
        /// </summary>
        [DataMember]
        [Description("Last error message.")]
        [IsRequired]
        public string? LastErrorMessage
        {
            get;
            set;
        }

        /// <summary>
        /// Attributes.
        /// </summary>
        [DataMember]
        [ForeignKey(OnDelete = ForeignKeyDelete.None)]
        [Filter(FilterType.Contains)]
        public List<GXAttribute>? Attributes
        {
            get;
            set;
        }

        /// <summary>
        /// Executed tasks.
        /// </summary>
        [DataMember]
        [ForeignKey(typeof(GXTask))]
        [Filter(FilterType.Contains)]
        public List<GXTask>? Tasks
        {
            get;
            set;
        }

        /// <summary>
        /// Object errors.
        /// </summary>
        [DataMember, ForeignKey(typeof(GXObjectError))]
        [Filter(FilterType.Contains)]
        public List<GXObjectError>? Errors
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

        /// <inheritdoc />
        public override string ToString()
        {
            if (Template != null)
            {
                if (Template.ShortName != 0)
                {
                    return Template.ShortName + " " + Template.Name;
                }
                return Template.LogicalName + " " + Template.Name;
            }
            return nameof(GXObject);
        }
    }
}
