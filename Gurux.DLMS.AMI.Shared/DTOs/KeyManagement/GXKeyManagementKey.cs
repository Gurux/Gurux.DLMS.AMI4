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
    [DataContract(Name = "GXKeyManagementKey"), Serializable]
    public class GXKeyManagementKey : GXTableBase, IUnique<Guid>
    {
        /// <summary>
        /// Identifier.
        /// </summary>
        [Key]
        [DataMember(Name = "ID")]
        [DefaultValue(null)]
        [Filter(FilterType.Exact)]
        public Guid Id { get; set; }

        /// <summary>
        /// The parent key management.
        /// </summary>
        [DataMember]
        [ForeignKey(OnDelete = ForeignKeyDelete.None)]
        [DefaultValue(null)]
        [Index(false)]
        [IsRequired]
        public GXKeyManagement? KeyManagement { get; set; }

        /// <summary>
        /// Key type.
        /// </summary>
        [DataMember]
        [IsRequired]
        public KeyManagementType? KeyType { get; set; }

        /// <summary>
        /// Key data.
        /// </summary>
        [DataMember]
        [DefaultValue(null)]
        //[IsRequired]
        public byte[]? Key { get; set; }

        /// <summary>
        /// Creation time.
        /// </summary>
        [Index(false, Descend = true)]
        [DataMember]
        [DefaultValue(null)]
        [Filter(FilterType.GreaterOrEqual)]
        [IsRequired]
        public DateTimeOffset? CreationTime { get; set; }

        /// <summary>
        /// When was the key management key last updated.
        /// </summary>
        [Description("When was the key management key last updated.")]
        [DataMember]
        [DefaultValue(null)]
        [Filter(FilterType.GreaterOrEqual)]
        public DateTimeOffset? Updated { get; set; }

        /// <summary>Returns a detached metadata object without bytes or nested secret lists.</summary>
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
        [DataMember, DefaultValue(null)]
        public KeyState? State { get; set; }

        /// <summary>Managed object type, separate from the DLMS key role.</summary>
        [DataMember, DefaultValue(null)]
        public KeyObjectType? ObjectType { get; set; }

        /// <summary>Cryptographic algorithm.</summary>
        [DataMember, DefaultValue(null)]
        public KeyAlgorithm? Algorithm { get; set; }

        /// <summary>Cryptographic key length in bits.</summary>
        [DataMember, DefaultValue(null)]
        public int? CryptographicLength { get; set; }

        /// <summary>Encoding of the key material.</summary>
        [DataMember, DefaultValue(null)]
        public KeyFormat? Format { get; set; }

        /// <summary>Permitted cryptographic purposes.</summary>
        [DataMember, DefaultValue(null)]
        public KeyUsage? UsageMask { get; set; }

        /// <summary>Whether material may leave the provider.</summary>
        [DataMember, DefaultValue(null)]
        public bool? Extractable { get; set; }

        /// <summary>Provider override; null inherits the group provider.</summary>
        [DataMember, DefaultValue(null)]
        [StringLength(512)]
        public string? ProviderId { get; set; }

        /// <summary>Named provider connection override.</summary>
        [DataMember, DefaultValue(null)]
        [StringLength(512)]
        public string? ProviderConnectionId { get; set; }

        /// <summary>Opaque provider object identifier.</summary>
        [DataMember, DefaultValue(null)]
        [StringLength(512)]
        public string? ExternalId { get; set; }

        /// <summary>Related public or private key identity.</summary>
        [DataMember, DefaultValue(null)]
        public Guid? RelatedKeyId { get; set; }
        /// <summary>Opaque related-object identifier at an external provider.</summary>
        [DataMember, DefaultValue(null), StringLength(512)]
        public string? RelatedExternalId { get; set; }

        /// <summary>Time when the key becomes active.</summary>
        [DataMember, DefaultValue(null)]
        public DateTimeOffset? ActivationDate { get; set; }

        /// <summary>Time when the key is deactivated.</summary>
        [DataMember, DefaultValue(null)]
        public DateTimeOffset? DeactivationDate { get; set; }

        /// <summary>Time associated with the key's compromise.</summary>
        [DataMember, DefaultValue(null)]
        public DateTimeOffset? CompromiseDate { get; set; }

        /// <summary>Time when the key material is destroyed.</summary>
        [DataMember, DefaultValue(null)]
        public DateTimeOffset? DestroyDate { get; set; }

        /// <summary>Earliest time when the key may be used for cryptographic processing.</summary>
        [DataMember, DefaultValue(null)]
        public DateTimeOffset? ProcessStartDate { get; set; }

        /// <summary>Time after which the key may no longer protect new data.</summary>
        [DataMember, DefaultValue(null)]
        public DateTimeOffset? ProtectStopDate { get; set; }


        /// <summary>Version token used by the database to detect conflicting updates.</summary>
        /// <remarks>
        /// A stale token causes a conflicting update to be rejected instead of overwriting newer changes.
        /// </remarks>
        [DataMember]
        [StringLength(36)]
        [ConcurrencyCheck]
        public string? ConcurrencyStamp { get; set; }

        /// <summary>
        /// Remove time.
        /// </summary>
        [DataMember]
        [Index(false, Descend = true)]
        [DefaultValue(null)]
        [Filter(FilterType.Null)]
        public DateTimeOffset? Removed { get; set; }

        /// <summary>
        /// Initializes the creation timestamp before insertion if it has not been set.
        /// </summary>
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
