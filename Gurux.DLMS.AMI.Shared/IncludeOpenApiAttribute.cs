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
    /// Limits a nested type in the OpenAPI schema for this property to the named properties.
    /// </summary>
    /// <remarks>
    /// Constructor.
    /// </remarks>
    /// <param name="type">Nested type whose schema is restricted.</param>
    /// <param name="include">Names of properties to include.</param>
    [System.ComponentModel.Description("Limits a nested type in the OpenAPI schema for this property to the named properties.")]
    [AttributeUsage(AttributeTargets.Property,
      AllowMultiple = true)]
    public sealed class IncludeOpenApiAttribute(Type type, params string[] include) : Attribute
    {
        /// <summary>
        /// Nested type whose schema is restricted.
        /// </summary>
        [System.ComponentModel.Description("Nested type whose schema is restricted.")]
        public Type Type { get; private set; } = type;

        /// <summary>
        /// Property names to include in the nested type schema.
        /// </summary>
        [System.ComponentModel.Description("Property names to include in the nested type schema.")]
        public string[] Include { get; private set; } = include;
    }
}
