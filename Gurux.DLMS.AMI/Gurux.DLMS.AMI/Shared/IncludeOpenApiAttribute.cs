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
// and/or modify it under the terms of the GNU General License
// as published by the Free Software Foundation; version 2 of the License.
// Gurux Device Framework is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.
// See the GNU General License for more details.
//
// This code is licensed under the GNU General License v2.
// Full text may be retrieved at http://www.gnu.org/licenses/gpl-2.0.txt
//---------------------------------------------------------------------------

using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace Gurux.DLMS.AMI.Shared
{
    /// <summary>
    /// Property is included to swagger schema.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property,
      AllowMultiple = true)]
    public sealed class IncludeOpenApiAttribute : Attribute
    {
        /// <summary>
        /// Included type.
        /// </summary>
        public Type Type { get; private set; }

        /// <summary>
        /// Included items.
        /// </summary>
        public string[] Include { get; private set; }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="type">Included type.</param>
        /// <param name="include">Included items.</param>
        public IncludeOpenApiAttribute(Type type, params string[] include)
        {
            Type = type;
            Include = include;
        }
    }
}
