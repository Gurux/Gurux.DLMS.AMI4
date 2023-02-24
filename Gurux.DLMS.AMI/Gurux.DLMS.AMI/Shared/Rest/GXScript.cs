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
using Gurux.DLMS.AMI.Shared.DTOs.Authentication;
using Gurux.DLMS.AMI.Shared.Enums;
using System.ComponentModel.DataAnnotations;

namespace Gurux.DLMS.AMI.Shared.Rest
{
    /// <summary>
    /// Get script.
    /// </summary>
    public class GetScriptResponse
    {
        /// <summary>
        /// Script information.
        /// </summary>
        [IncludeSwagger(typeof(GXScriptGroup), nameof(GXScriptGroup.Id),
            nameof(GXScriptGroup.Name))]
        [IncludeSwagger(typeof(GXWorkflow), nameof(GXWorkflow.Id),
            nameof(GXWorkflow.Name))]
        [IncludeSwagger(typeof(GXScriptMethod), nameof(GXScriptMethod.Id),
            nameof(GXScriptMethod.Name))]
        [ExcludeSwagger(typeof(GXScript), nameof(GXScript.Logs)
            , nameof(GXScript.Resources), nameof(GXScript.Languages), 
            nameof(GXScript.ByteAssembly), nameof(GXScript.Module))]
        [IncludeSwagger(typeof(GXUser), nameof(GXUser.Id), nameof(GXUser.UserName))]
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public GXScript Item
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Get list from scripts.
    /// </summary>
    [DataContract]
    public class ListScripts : IGXRequest<ListScriptsResponse>
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
        /// Amount of the scripts to retreave.
        /// </summary>
        public int Count
        {
            get;
            set;
        }

        /// <summary>
        /// Filter can be used to filter scripts.
        /// </summary>
        [ExcludeSwagger(typeof(GXScript),
        nameof(GXScript.ScriptGroups),
             nameof(GXScript.Workflows),
             nameof(GXScript.Methods),
             nameof(GXScript.Logs),
             nameof(GXScript.Creator),
             nameof(GXScript.Resources), 
             nameof(GXScript.Languages),
             nameof(GXScript.Module))]
        public GXScript? Filter
        {
            get;
            set;
        }

        /// <summary>
        /// Admin user can access scripts from all users.
        /// </summary>
        /// <remarks>
        /// If true, scripts from all users are retreaved, not just current user. 
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
    /// Script items reply.
    /// </summary>
    [DataContract]
    [Description("Script items reply.")]
    public class ListScriptsResponse
    {
        /// <summary>
        /// List of script items.
        /// </summary>
        [Description("List of script items.")]
        [DataMember]
        [ExcludeSwagger(typeof(GXScript),
        nameof(GXScript.ScriptGroups),
            nameof(GXScript.Workflows),
            nameof(GXScript.Methods),
            nameof(GXScript.Logs), 
            nameof(GXScript.Resources), 
            nameof(GXScript.Languages), 
            nameof(GXScript.Module))]
        [IncludeSwagger(typeof(GXUser), nameof(GXUser.Id), nameof(GXUser.UserName))]
        public GXScript[]? Scripts
        {
            get;
            set;
        }

        /// <summary>
        /// Total count of the scriptrs.
        /// </summary>
        [DataMember]
        [Description("Total count of the scriptrs.")]
        public int Count
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Update scripts.
    /// </summary>
    [DataContract]
    public class UpdateScript : IGXRequest<UpdateScriptResponse>
    {
        /// <summary>
        /// Scripts to update.
        /// </summary>
        [DataMember]
        [Description("Scripts to update.")]
        [IncludeSwagger(typeof(GXScriptGroup), nameof(GXScriptGroup.Id))]
        [IncludeSwagger(typeof(GXScriptMethod), nameof(GXScriptMethod.Id))]
        [ExcludeSwagger(typeof(GXScript),
            nameof(GXScript.Workflows),
            nameof(GXScript.Methods),
            nameof(GXScript.Logs),
            nameof(GXScript.Creator),
            nameof(GXScript.Resources),
            nameof(GXScript.Languages),
            nameof(GXScript.Module))]
        public List<GXScript> Scripts{get; set;} = new List<GXScript>();
    }

    /// <summary>
    /// Update scripts reply.
    /// </summary>
    [Description("Update scripts reply.")]
    [DataContract]
    public class UpdateScriptResponse
    {
        /// <summary>
        /// New script identifiers.
        /// </summary>
        [DataMember]
        [Description("New script identifiers.")]
        public Guid[]? ScriptIds
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Delete scripts.
    /// </summary>
    [DataContract]
    public class RemoveScript : IGXRequest<RemoveScriptResponse>
    {
        /// <summary>
        /// Removed script identifiers.
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
    /// Reply from Delete script.
    /// </summary>
    [DataContract]
    public class RemoveScriptResponse
    {
    }

    /// <summary>
    /// Validate script.
    /// </summary>
    [DataContract]
    public class ValidateScript : IGXRequest<ValidateScriptResponse>
    {
        /// <summary>
        /// Validated string.
        /// </summary>
        public string Script
        {
            get;
            set;
        }

        /// <summary>
        /// Additional name spaces.
        /// </summary>
        public string? NameSpaces
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Reply from refresh scripts.
    /// </summary>
    [DataContract]
    public class ValidateScriptResponse
    {
        /// <summary>
        /// Script errors in JSON, or null if there are no errors.
        /// </summary>
        public string? Errors
        {
            get;
            set;
        }

        /// <summary>
        /// Compile time.
        /// </summary>
        public int CompileTime
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Run the script.
    /// </summary>
    [DataContract]
    public class RunScript : IGXRequest<RunScriptResponse>
    {
        /// <summary>
        /// Script method ID.
        /// </summary>
        public Guid MethodId
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Run script reply.
    /// </summary>
    [DataContract]
    public class RunScriptResponse
    {
        /// <summary>
        /// Result value.
        /// </summary>
        public object? Result
        {
            get;
            set;
        }
    }
}
