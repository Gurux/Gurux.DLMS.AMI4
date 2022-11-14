using Duende.IdentityServer.EntityFramework.Entities;
using Duende.IdentityServer.EntityFramework.Options;
using Duende.IdentityServer.Models;
using Gurux.DLMS.AMI.Client.Pages.Admin;
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