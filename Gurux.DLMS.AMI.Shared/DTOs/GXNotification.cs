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
    [System.ComponentModel.Description("A lightweight description of a resource update delivered to connected clients.")]
    public class GXTarget
    {
        /// <summary>
        /// Initializes a new instance of the GXTarget class.
        /// </summary>
        [System.ComponentModel.Description("Initializes a new instance of the GXTarget class.")]
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
        [System.ComponentModel.Description("Initializes a new instance of the GXTarget class.")]
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
        [System.ComponentModel.Description("Initializes a new instance of the GXTarget class.")]
        public GXTarget(string type, Guid id, string? name = default, string? description = default)
        {
            Type = type;
            Id = id.ToString();
            Name = name;
            Description = description;
            CreationTime = DateTimeOffset.Now;
        }

        /// <summary>Resource type and update action, for example <c>system-log.clear</c>.</summary>
        [System.ComponentModel.Description("Resource type and update action, for example system-log.clear.")]
        public string Type { get; set; } = default!;

        /// <summary>Originating resource or module identifier, used to filter update notifications.</summary>
        [System.ComponentModel.Description("Originating resource or module identifier, used to filter update notifications.")]
        public string? Source { get; set; }

        /// <summary>Identifier of the affected item; may be absent for notifications concerning an entire source.</summary>
        [System.ComponentModel.Description("Identifier of the affected item; may be absent for notifications concerning an entire source.")]
        public string? Id { get; set; }

        /// <summary>Optional saved row version; an opaque equality token, not a notification sequence number.</summary>
        [System.ComponentModel.Description("Optional saved row version; an opaque equality token, not a notification sequence number.")]
        public string? ConcurrencyStamp { get; set; }

        /// <summary>Optional display name of the affected item.</summary>
        [System.ComponentModel.Description("Optional display name of the affected item.")]
        public string? Name { get; set; }

        /// <summary>Human-readable notification title supplied by the server.</summary>
        [System.ComponentModel.Description("Human-readable notification title supplied by the server.")]
        public string? Title { get; set; }

        /// <summary>Optional human-readable description of the update.</summary>
        [System.ComponentModel.Description("Optional human-readable description of the update.")]
        public string? Description { get; set; }

        /// <summary>Time associated with the notification; initialized to the current time by the parameterized constructors.</summary>
        [System.ComponentModel.Description("Time associated with the notification; initialized to the current time by the parameterized constructors.")]
        public DateTimeOffset? CreationTime { get; set; }

        /// <summary>Optional severity level associated with the update.</summary>
        [System.ComponentModel.Description("Optional severity level associated with the update.")]
        public int? Level { get; set; }

        /// <summary>Optional serialized settings or additional data supplied by the notification producer.</summary>
        [System.ComponentModel.Description("Optional serialized settings or additional data supplied by the notification producer.")]
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
