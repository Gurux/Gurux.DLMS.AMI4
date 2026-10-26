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
namespace Gurux.DLMS.AMI.Shared.DTOs
{
    /// <summary>A lightweight description of a resource update delivered to connected clients.</summary>
    public class GXTarget
    {
        /// <summary>
        /// Initializes a new instance of the GXTarget class.
        /// </summary>
        public GXTarget()
        {
        }

        /// <summary>
        /// Initializes a new instance of the GXTarget class.
        /// </summary>
        /// <param name="type">The type of the target.</param>
        /// <param name="id">The identifier of the target.</param>
        /// <param name="name">The name of the target.</param>
        /// <param name="description">The description of the target.</param>
        public GXTarget(string type, string? id = default, string? name = default, string? description = default)
        {
            Type = type;
            Id = id;
            Name = name;
            Description = description;
            CreationTime = DateTimeOffset.Now;
        }

        /// <summary>
        /// Initializes a new instance of the GXTarget class.
        /// </summary>
        /// <param name="type">The type of the target.</param>
        /// <param name="id">The identifier of the target.</param>
        /// <param name="name">The name of the target.</param>
        /// <param name="description">The description of the target.</param>
        public GXTarget(string type, Guid id, string? name = default, string? description = default)
        {
            Type = type;
            Id = id.ToString();
            Name = name;
            Description = description;
            CreationTime = DateTimeOffset.Now;
        }

        /// <summary>Resource type and update action, for example <c>system-log.clear</c>.</summary>
        public string Type { get; set; } = default!;

        /// <summary>Originating resource or module identifier, used to filter update notifications.</summary>
        public string? Source { get; set; }

        /// <summary>Identifier of the affected item; may be absent for notifications concerning an entire source.</summary>
        public string? Id { get; set; }

        /// <summary>Optional saved row version; an opaque equality token, not a notification sequence number.</summary>
        public string? ConcurrencyStamp { get; set; }

        /// <summary>Optional display name of the affected item.</summary>
        public string? Name { get; set; }

        /// <summary>Human-readable notification title supplied by the server.</summary>
        public string? Title { get; set; }

        /// <summary>Optional human-readable description of the update.</summary>
        public string? Description { get; set; }

        /// <summary>Time associated with the notification; initialized to the current time by the parameterized constructors.</summary>
        public DateTimeOffset? CreationTime { get; set; }

        /// <summary>Optional severity level associated with the update.</summary>
        public int? Level { get; set; }

        /// <summary>Optional serialized settings or additional data supplied by the notification producer.</summary>
        public string? Settings { get; set; }

        /// <inheritdoc />
        public override string ToString()
        {
            if (string.IsNullOrEmpty(Description))
            {
                if (string.IsNullOrEmpty(Name))
                {
                    if (string.IsNullOrEmpty(Type))
                    {
                        return nameof(GXTarget);
                    }
                    return Type;
                }
                return Name;
            }
            return Name + " " + Description;
        }
    }
}
