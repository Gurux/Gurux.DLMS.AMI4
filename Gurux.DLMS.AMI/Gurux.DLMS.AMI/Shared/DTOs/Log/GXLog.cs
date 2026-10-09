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
using Gurux.DLMS.AMI.Shared.DTOs.Authentication;
using Gurux.Service.Orm.Common;
using Gurux.Service.Orm.Common.Enums;
using Gurux.DLMS.AMI.Shared.Enums;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace Gurux.DLMS.AMI.Shared.DTOs.Log
{
    /// <summary>
    /// Generic Log.
    /// </summary>
    [DataContract]
    [JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
    [IndexCollection(false, nameof(LogType), nameof(Creator), nameof(CreationTime), nameof(Id), Name = "LogType_Creator_Time_Id")]
    [IndexCollection(false, nameof(LogType), nameof(SourceId), nameof(CreationTime), nameof(Id), Name = "LogType_SourceId_Time_Id")]
    [IndexCollection(false, nameof(LogType), nameof(User), nameof(CreationTime), nameof(Id), Name = "LogType_User_Time_Id")]
    [IndexCollection(false, nameof(LogType), nameof(CreationTime), nameof(Id), Name = "LogType_Time_Id")]
    [IndexCollection(false, nameof(CreationTime), nameof(Id), Name = "Time_Id")]
    public class GXLog : IUnique<long>
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        public GXLog()
        {
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <remarks>
        /// Log levels from 0 to 4 are reserved for Gurux.DLMS.AMI.
        /// </remarks>
        /// <param name="level">Log severity level</param>
        public GXLog(int level)
        {
            Level = level;
            Type = 0;
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="level">Log severity level</param>
        public GXLog(TraceLevel level) : this((int)level)
        {
        }

        /// <summary>
        /// External GUID identifier.
        /// </summary>
        [DataMember(IsRequired = true)]
        [DefaultValue(null)]
        [Filter(FilterType.Exact)]
        [Index(true)]
        [IsRequired]
        public Guid PublicId { get; set; }

        /// <summary>Database-generated insertion order and storage primary key.</summary>
        [DataMember]
        [PrimaryKey]
        [AutoIncrement]
        [Filter(FilterType.GreaterOrEqual)]
        public long Id { get; set; }

        /// <summary>Origin identifier, including string identifiers used by modules.</summary>
        [DataMember]
        [StringLength(128)]
        [Filter(FilterType.Exact)]
        public string? SourceId { get; set; }

        /// <summary>Optional diagnostic stack trace.</summary>
        [DataMember]
        public string? StackTrace { get; set; }

        /// <summary>Presentation group, preserving system log grouping.</summary>
        [DataMember]
        [DefaultValue(0)]
        public int Group { get; set; }
        /// <summary>
        /// Log type.
        /// </summary>
        [DataMember]
        [ForeignKey(OnDelete = ForeignKeyDelete.Restrict)]
        [Filter(FilterType.Exact)]
        public GXLogType? LogType { get; set; }

        /// <summary>
        /// Optional User information.
        /// </summary>
        [Filter(FilterType.Exact)]
        [DataMember]
        [ForeignKey(OnDelete = ForeignKeyDelete.None)]
        public GXUser? User { get; set; }

        /// <summary>
        /// Optional log creator.
        /// </summary>
        /// <remarks>
        /// This is not the user, but the creator of the log, for example, a script or a module. 
        /// It is used to link logs to their source.
        /// </remarks>
        [DataMember]
        [Filter(FilterType.Exact)]
        public Guid? Creator { get; set; }

        /// <summary>
        /// Creation time.
        /// </summary>
        [DataMember]
        [TimeStorageUnit(TimeStorageUnit.Milliseconds)]
        [Filter(FilterType.GreaterOrEqual)]
        [IsRequired]
        public DateTimeOffset? CreationTime { get; set; }

        /// <summary>
        /// Error is active if closed time is not set.
        /// </summary>
        [DataMember]
        [DefaultValue(null)]
        [Filter(FilterType.Null)]
        public DateTimeOffset? Closed { get; set; }

        /// <summary>
        /// Log string.
        /// </summary>
        [DataMember]
        [DefaultValue(null)]
        [Filter(FilterType.Contains)]
        [IsRequired]
        public string? Message { get; set; }

        /// <summary>
        /// Optional log data.
        /// </summary>
        [DataMember]
        [DefaultValue(null)]
        [Filter(FilterType.Contains)]
        public string? Data { get; set; }

        /// <summary>
        /// Log duration in ms.
        /// </summary>
        [DataMember]
        [DefaultValue(0)]
        [Filter(FilterType.GreaterOrEqual)]
        public int? Duration { get; set; }

        /// <summary>
        /// Log severity level.
        /// </summary>
        [DataMember]
        [DefaultValue(1)]
        [IsRequired]
        [Filter(FilterType.Exact)]
        public int? Level { get; set; }

        /// <summary>
        /// Log sub type.
        /// </summary>
        [DataMember]
        [DefaultValue(0)]
        [IsRequired]
        [Filter(FilterType.Exact)]
        public int? Type { get; set; }
        /// <summary>Audit target category.</summary>
        [DataMember, StringLength(32), Filter(FilterType.Exact)]
        public string? TargetType { get; set; }

        /// <summary>Audit operation.</summary>
        [DataMember, DefaultValue(CrudAction.None), Filter(FilterType.Exact)]
        public CrudAction Action { get; set; }

        /// <summary>Audit response status.</summary>
        [DataMember, DefaultValue(0), Filter(FilterType.Exact)]
        public int Status { get; set; }

        /// <summary>Audit response payload.</summary>
        [DataMember]
        public string? Reply { get; set; }

    }
}
