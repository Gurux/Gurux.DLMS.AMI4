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

using Duende.IdentityServer.EntityFramework.Entities;
using Duende.IdentityServer.EntityFramework.Options;
using Gurux.DLMS.AMI.Server.Models;
using Gurux.DLMS.AMI.Shared.DTOs.Authentication;
using Microsoft.AspNetCore.ApiAuthorization.IdentityServer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Gurux.DLMS.AMI.Server.Data
{
    /// <inheritdoc/>
    public class ApplicationDbContext : ApiAuthorizationDbContext<ApplicationUser>
    {
        /// <inheritdoc/>
        public ApplicationDbContext(
            DbContextOptions options,
            IOptions<OperationalStoreOptions> operationalStoreOptions) : base(options, operationalStoreOptions)
        {
        }

        /// <inheritdoc/>
        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            builder.Entity<DeviceFlowCodes>().ToTable(nameof(GXDeviceCodes));
            builder.Entity<Duende.IdentityServer.EntityFramework.Entities.PersistedGrant>().ToTable(nameof(GXPersistedGrants));
            builder.Entity<ApplicationUser>().ToTable(nameof(GXUser));
            builder.Entity<IdentityRole>().ToTable(nameof(GXRole));
            builder.Entity<IdentityRoleClaim<string>>().ToTable(nameof(GXRoleClaim));
            builder.Entity<IdentityUserRole<string>>().ToTable(nameof(GXUserRole));
            builder.Entity<IdentityUserClaim<string>>().ToTable(nameof(GXUserClaim));
            builder.Entity<IdentityUserLogin<string>>().ToTable(nameof(GXUserLogin));
            builder.Entity<IdentityUserToken<string>>().ToTable(nameof(GXUserToken));
            builder.Entity<Key>().ToTable(nameof(GXKey));
        }
    }
}