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
using System.Text.Json.Serialization;

namespace Gurux.DLMS.AMI.Shared.DTOs
{
    /// <summary>
    /// Localized resources.
    /// </summary>
    public class GXLocalizedResource : IUnique<Guid>
    {
        /// <summary>
        /// Localized string identifier.
        /// </summary>
        public Guid Id
        {
            get;
            set;
        }

        /// <summary>
        /// Localized string.
        /// </summary>
        public string Value { get; set; } = "";

        /// <summary>
        /// Hash value for Localized string key.
        /// </summary>
        [Index(false)]
        public int Hash
        {
            get;
            set;
        }

        /// <summary>
        /// Parent language.
        /// </summary>
        [ForeignKey(OnDelete = ForeignKeyDelete.Cascade)]
        [Index(false)]
        [JsonIgnore]
        public GXLanguage? Language
        {
            get;
            set;
        }


        /// <summary>
        /// Configuration that owns this language resouce.
        /// </summary>
        /// <remarks>
        /// Language resource is removed when the configuration is removed.
        /// </remarks>
        [ForeignKey(OnDelete = ForeignKeyDelete.Cascade)]
        [Index(false)]
        public GXConfiguration? Configuration
        {
            get;
            set;
        }

        /// <summary>
        /// Module that owns this language resouce.
        /// </summary>
        /// <remarks>
        /// Language resource is removed when the module is removed.
        /// </remarks>
        [ForeignKey(OnDelete = ForeignKeyDelete.None)]
        [Index(false)]
        public GXModule? Module
        {
            get;
            set;
        }

        /// <summary>
        /// Block that owns this language resouce.
        /// </summary>
        /// <remarks>
        /// Language resource is removed when the block is removed.
        /// </remarks>
        [ForeignKey(OnDelete = ForeignKeyDelete.Cascade)]
        [Index(false)]
        public GXBlock? Block
        {
            get;
            set;
        }

        /// <summary>
        /// Script that owns this language resouce.
        /// </summary>
        /// <remarks>
        /// Language resource is removed when the script is removed.
        /// </remarks>
        [ForeignKey(OnDelete = ForeignKeyDelete.None)]
        [Index(false)]
        public GXScript? Script
        {
            get;
            set;
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return Id + ": " + Value;
        }
    }
}
