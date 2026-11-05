// --------------------------------------------------------------------------
// Gurux Ltd
//
// Copyright (c) Gurux Ltd.
//
// This file is part of Gurux Device Framework.
//
// Commit: $Format:%H$
// Date:   $Format:%cI$
//
// This software is available under either of the following licenses:
//
// 1. GNU General Public License, version 2 only (GPL-2.0-only).
//    See the LICENSE file included with this distribution.
//
// 2. A commercial license granted by Gurux Ltd under a separate written
//    agreement. Use under this option requires a valid commercial license
//    and is governed by the terms of that agreement.
//
// If you have not obtained a commercial license from Gurux Ltd,
// use of this software is governed by GPL-2.0-only.
//
// Commercial licensing: https://www.gurux.fi/
//
// SPDX-License-Identifier: GPL-2.0-only OR LicenseRef-Gurux-Commercial
// --------------------------------------------------------------------------

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
