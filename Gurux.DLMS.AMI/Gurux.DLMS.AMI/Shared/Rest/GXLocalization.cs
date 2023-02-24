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
using Gurux.DLMS.AMI.Shared.Enums;

namespace Gurux.DLMS.AMI.Shared.Rest
{

    /// <summary>
    /// Update localization.
    /// </summary>
    [DataContract]
    public class UpdateLocalization : IGXRequest<UpdateLocalizationResponse>
    {
        /// <summary>
        /// Localization to add.
        /// </summary>
        [DataMember]
        public GXLanguage[] Localizations
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Update reader localization.
    /// </summary>
    [DataContract]
    public class UpdateLocalizationResponse
    {
        /// <summary>
        /// New localization identifiers.
        /// </summary>
        [DataMember]
        public Guid[] LocalizationIds
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Get list from localizations.
    /// </summary>
    [DataContract]
    public class ListLanguages : IGXRequest<ListLanguagesResponse>
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
        /// Amount of the localizations to retreave.
        /// </summary>
        public int Count
        {
            get;
            set;
        }

        /// <summary>
        /// Filter can be used to filter localizations.
        /// </summary>
        public GXLanguage? Filter
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
    /// Get localizations response.
    /// </summary>
    [DataContract]
    public class ListLanguagesResponse
    {
        /// <summary>
        /// List of languages.
        /// </summary>
        [DataMember]
        public GXLanguage[] Languages
        {
            get;
            set;
        }
        /// <summary>
        /// Total count of the languages.
        /// </summary>
        [DataMember]
        public int Count
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Remove localizations.
    /// </summary>
    [DataContract]
    public class RemoveLocalization : IGXRequest<RemoveLocalizationResponse>
    {
        /// <summary>
        /// Language identifiers to remove.
        /// </summary>
        [DataMember]
        public string[] Ids
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Remove localizations response.
    /// </summary>
    [DataContract]
    public class RemoveLocalizationResponse
    {
    }

    /// <summary>
    /// Refresh localizated strings.
    /// </summary>
    [DataContract]
    public class RefreshLocalization
    {
        /// <summary>
        /// Refrest localizations for the selected languages.
        /// </summary>
        [DataMember]
        public GXLanguage[]? Languages
        {
            get;
            set;
        }
    }
}
