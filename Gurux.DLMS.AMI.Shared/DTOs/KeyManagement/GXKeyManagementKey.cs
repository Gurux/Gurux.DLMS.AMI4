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
using Gurux.DLMS.AMI.Shared.DTOs.Enums;
using Gurux.DLMS.AMI.Shared.Enums;
using Gurux.Service.Orm.Common;
using Gurux.Service.Orm.Common.Enums;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace Gurux.DLMS.AMI.Shared.DTOs.KeyManagement
{
    /// <summary>A managed key or password with its security purpose, provider reference, and lifecycle metadata.</summary>
    [Description("A managed key or password with its security purpose, provider reference, and lifecycle metadata.")]
    [DataContract(Name = "GXKeyManagementKey"), Serializable]
    public class GXKeyManagementKey : GXTableBase, IUnique<Guid>
    {
        /// <summary>Unique identifier of the key management key.</summary>
        [Description("Unique identifier of the key management key.")]
        [Key]
        [DataMember(Name = "ID")]
        [DefaultValue(null)]
        [Filter(FilterType.Exact)]
        public Guid Id { get; set; }

        /// <summary>
        /// The parent key management.
        /// </summary>
        [Description("The parent key management.")]
        [DataMember]
        [ForeignKey(OnDelete = ForeignKeyDelete.None)]
        [DefaultValue(null)]
        [Index(false)]
        [IsRequired]
        public GXKeyManagement? KeyManagement { get; set; }

        /// <summary>
        /// Key type.
        /// </summary>
        [Description("Key type.")]
        [DataMember]
        [IsRequired]
        public KeyManagementType? KeyType { get; set; }

        /// <summary>
        /// Key data.
        /// </summary>
        [Description("Key data.")]
        [DataMember]
        [DefaultValue(null)]
        //[IsRequired]
        public byte[]? Key { get; set; }

        /// <summary>Time when the record was created.</summary>
        [Description("Time when the record was created.")]
        [Index(false, Descend = true)]
        [DataMember]
        [DefaultValue(null)]
        [Filter(FilterType.GreaterOrEqual)]
        [IsRequired]
        public DateTimeOffset? CreationTime { get; set; }

        /// <summary>Time when the key management key was last updated.</summary>
        [Description("Time when the key management key was last updated.")]
        [DataMember]
        [DefaultValue(null)]
        [Filter(FilterType.GreaterOrEqual)]
        public DateTimeOffset? Updated { get; set; }

        /// <summary>Returns a detached metadata object without bytes or nested secret lists.</summary>
        [Description("Returns a detached metadata object without bytes or nested secret lists.")]
        public GXKeyManagementKey ToMetadata()
        {
            var copy = (GXKeyManagementKey)MemberwiseClone();
            copy.Key = null;
            if (KeyManagement != null)
            {
                copy.KeyManagement = new GXKeyManagement
                {
                    Id = KeyManagement.Id,
                    Name = KeyManagement.Name,
                    ProviderId = KeyManagement.ProviderId,
                    ProviderConnectionId = KeyManagement.ProviderConnectionId
                };
            }
            return copy;
        }
        /// <summary>Lifecycle state. Null is an unmigrated key and denies use.</summary>
        [Description("Lifecycle state. Null is an unmigrated key and denies use.")]
        [DataMember, DefaultValue(null)]
        public KeyState? State { get; set; }

        /// <summary>Managed object type, separate from the DLMS key role.</summary>
        [Description("Managed object type, separate from the DLMS key role.")]
        [DataMember, DefaultValue(null)]
        public KeyObjectType? ObjectType { get; set; }

        /// <summary>Cryptographic algorithm.</summary>
        [Description("Cryptographic algorithm.")]
        [DataMember, DefaultValue(null)]
        public KeyAlgorithm? Algorithm { get; set; }

        /// <summary>Cryptographic key length in bits.</summary>
        [Description("Cryptographic key length in bits.")]
        [DataMember, DefaultValue(null)]
        public int? CryptographicLength { get; set; }

        /// <summary>Encoding of the key material.</summary>
        [Description("Encoding of the key material.")]
        [DataMember, DefaultValue(null)]
        public KeyFormat? Format { get; set; }

        /// <summary>Permitted cryptographic purposes.</summary>
        [Description("Permitted cryptographic purposes.")]
        [DataMember, DefaultValue(null)]
        public KeyUsage? UsageMask { get; set; }

        /// <summary>Whether material may leave the provider.</summary>
        [Description("Whether material may leave the provider.")]
        [DataMember, DefaultValue(null)]
        public bool? Extractable { get; set; }

        /// <summary>Provider override; null inherits the group provider.</summary>
        [Description("Provider override; null inherits the group provider.")]
        [DataMember, DefaultValue(null)]
        [StringLength(512)]
        public string? ProviderId { get; set; }

        /// <summary>Named provider connection override.</summary>
        [Description("Named provider connection override.")]
        [DataMember, DefaultValue(null)]
        [StringLength(512)]
        public string? ProviderConnectionId { get; set; }

        /// <summary>Opaque provider object identifier.</summary>
        [Description("Opaque provider object identifier.")]
        [DataMember, DefaultValue(null)]
        [StringLength(512)]
        public string? ExternalId { get; set; }

        /// <summary>Related public or private key identity.</summary>
        [Description("Related public or private key identity.")]
        [DataMember, DefaultValue(null)]
        public Guid? RelatedKeyId { get; set; }
        /// <summary>Opaque related-object identifier at an external provider.</summary>
        [Description("Opaque related-object identifier at an external provider.")]
        [DataMember, DefaultValue(null), StringLength(512)]
        public string? RelatedExternalId { get; set; }

        /// <summary>Time when the key becomes active.</summary>
        [Description("Time when the key becomes active.")]
        [DataMember, DefaultValue(null)]
        public DateTimeOffset? ActivationDate { get; set; }

        /// <summary>Time when the key is deactivated.</summary>
        [Description("Time when the key is deactivated.")]
        [DataMember, DefaultValue(null)]
        public DateTimeOffset? DeactivationDate { get; set; }

        /// <summary>Time associated with the key's compromise.</summary>
        [Description("Time associated with the key's compromise.")]
        [DataMember, DefaultValue(null)]
        public DateTimeOffset? CompromiseDate { get; set; }

        /// <summary>Time when the key material is destroyed.</summary>
        [Description("Time when the key material is destroyed.")]
        [DataMember, DefaultValue(null)]
        public DateTimeOffset? DestroyDate { get; set; }

        /// <summary>Earliest time when the key may be used for cryptographic processing.</summary>
        [Description("Earliest time when the key may be used for cryptographic processing.")]
        [DataMember, DefaultValue(null)]
        public DateTimeOffset? ProcessStartDate { get; set; }

        /// <summary>Time after which the key may no longer protect new data.</summary>
        [Description("Time after which the key may no longer protect new data.")]
        [DataMember, DefaultValue(null)]
        public DateTimeOffset? ProtectStopDate { get; set; }


        /// <summary>Version token used by the database to detect conflicting updates.</summary>
        /// <remarks>
        /// A stale token causes a conflicting update to be rejected instead of overwriting newer changes.
        /// </remarks>
        [Description("Version token used by the database to detect conflicting updates.")]
        [DataMember]
        [StringLength(36)]
        [ConcurrencyCheck]
        public string? ConcurrencyStamp { get; set; }

        /// <summary>Time when the record was marked as removed.</summary>
        [Description("Time when the record was marked as removed.")]
        [DataMember]
        [Index(false, Descend = true)]
        [DefaultValue(null)]
        [Filter(FilterType.Null)]
        public DateTimeOffset? Removed { get; set; }

        /// <summary>
        /// Initializes the creation timestamp before insertion if it has not been set.
        /// </summary>
        [Description("Initializes the creation timestamp before insertion if it has not been set.")]
        public override void BeforeAdd()
        {
            if (CreationTime == null)
            {
                CreationTime = DateTime.Now;
            }
        }

        /// <summary>
        /// Updates the last-modified timestamp before persisting changes.
        /// </summary>
        [Description("Updates the last-modified timestamp before persisting changes.")]
        public override void BeforeUpdate()
        {
            Updated = DateTime.Now;
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            if (KeyType != null)
            {
                return KeyType.ToString()!;
            }
            return nameof(GXKeyManagementKey);
        }
    }
}
