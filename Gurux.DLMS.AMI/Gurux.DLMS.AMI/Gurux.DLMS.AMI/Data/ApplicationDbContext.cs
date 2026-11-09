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

using Gurux.DLMS.AMI.Shared.DTOs.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using System;

namespace Gurux.DLMS.AMI.Data
{
    /// <summary>Provides application db context.</summary>
    /// <param name="options">The options.</param>
    public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) :
    IdentityDbContext<ApplicationUser, ApplicationRole, string>(options)
    {
        /// <inheritdoc/>
        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            if (Database.ProviderName?.Contains("Oracle", StringComparison.OrdinalIgnoreCase) == true)
            {
                // Oracle EF uses quoted identifiers; keep names uppercase to match unquoted table creation.
                var user = builder.Entity<ApplicationUser>();
                user.ToTable("GXUSER");

                var role = builder.Entity<ApplicationRole>();
                role.ToTable("GXROLE");
                role.HasKey(x => new { x.Id });

                var roleClaim = builder.Entity<IdentityRoleClaim<string>>();
                roleClaim.ToTable("GXROLECLAIM");

                var userRole = builder.Entity<IdentityUserRole<string>>();
                userRole.ToTable("GXUSERROLE");

                var userClaim = builder.Entity<IdentityUserClaim<string>>();
                userClaim.ToTable("GXUSERCLAIM");

                var userLogin = builder.Entity<IdentityUserLogin<string>>();
                userLogin.ToTable("GXUSERLOGIN");
                userLogin.HasKey(x => new { x.LoginProvider, x.ProviderKey });

                var userToken = builder.Entity<IdentityUserToken<string>>();
                userToken.ToTable("GXUSERTOKEN");
                userToken.HasKey(x => new { x.UserId, x.LoginProvider, x.Name });
                UppercaseColumnNames(user);
                UppercaseColumnNames(role);
                UppercaseColumnNames(roleClaim);
                UppercaseColumnNames(userRole);
                UppercaseColumnNames(userClaim);
                UppercaseColumnNames(userLogin);
                UppercaseColumnNames(userToken);
                // Ensure Oracle provider binds Identity booleans as numeric values.
                user.Property(x => x.EmailConfirmed).HasConversion<int>().HasColumnType("NUMBER(1)");
                user.Property(x => x.PhoneNumberConfirmed).HasConversion<int>().HasColumnType("NUMBER(1)");
                user.Property(x => x.TwoFactorEnabled).HasConversion<int>().HasColumnType("NUMBER(1)");
                user.Property(x => x.LockoutEnabled).HasConversion<int>().HasColumnType("NUMBER(1)");
                user.Property(x => x.IsApproved).HasConversion<int>().HasColumnType("NUMBER(1)");
            }
            else
            {
                builder.Entity<ApplicationUser>().ToTable(nameof(GXUser)).Property(x => x.Id).HasColumnName("ID");
                builder.Entity<ApplicationRole>().ToTable(nameof(GXRole)).HasKey(x => new { x.Id });
                builder.Entity<ApplicationRole>().Property(x => x.Id).HasColumnName("ID");
                builder.Entity<IdentityRoleClaim<string>>().ToTable(nameof(GXRoleClaim)).Property(x => x.Id).HasColumnName("ID");
                builder.Entity<IdentityUserRole<string>>().ToTable(nameof(GXUserRole));
                builder.Entity<IdentityUserClaim<string>>().ToTable(nameof(GXUserClaim)).Property(x => x.Id).HasColumnName("ID");
                builder.Entity<IdentityUserLogin<string>>().ToTable(nameof(GXUserLogin)).HasKey(x => new { x.LoginProvider, x.ProviderKey });
                builder.Entity<IdentityUserToken<string>>().ToTable(nameof(GXUserToken)).HasKey(x => new { x.UserId, x.LoginProvider, x.Name });
                if (Database.ProviderName == "Npgsql.EntityFrameworkCore.PostgreSQL")
                {
                    // The ORM stores CreationTime as timestamp without time zone.
                    // Treat that stored clock value as UTC and convert local input
                    // to UTC before removing its Kind for Npgsql's timestamp writer.
                    builder.Entity<ApplicationUser>().Property(x => x.CreationTime)
                        .HasColumnType("timestamp without time zone")
                        .HasConversion(new ValueConverter<DateTime, DateTime>(
                            value => DateTime.SpecifyKind(value.Kind == DateTimeKind.Local ? value.ToUniversalTime() : value, DateTimeKind.Unspecified),
                            value => DateTime.SpecifyKind(value, DateTimeKind.Utc)));
                    // PostgreSQL timestamptz stores instants, and Npgsql requires
                    // DateTimeOffset values to have offset zero when writing.
                    var utcOffset = new ValueConverter<DateTimeOffset, DateTimeOffset>(
                        value => value.ToUniversalTime(), value => value.ToUniversalTime());
                    foreach (var property in builder.Entity<ApplicationUser>().Metadata.GetProperties())
                    {
                        if ((Nullable.GetUnderlyingType(property.ClrType) ?? property.ClrType) == typeof(DateTimeOffset))
                        {
                            property.SetValueConverter(utcOffset);
                        }
                    }
                    // The ORM creates unquoted PostgreSQL identifiers, which PostgreSQL
                    // folds to lowercase. EF quotes identifiers, so use those stored names.
                    foreach (var entity in builder.Model.GetEntityTypes())
                    {
                        if (entity.GetTableName() is string tableName)
                        {
                            entity.SetTableName(tableName.ToLowerInvariant());
                        }
                        foreach (var property in entity.GetProperties())
                        {
                            property.SetColumnName(property.GetColumnName().ToLowerInvariant());
                        }
                    }
                }
            }
        }

        private static void UppercaseColumnNames<TEntity>(EntityTypeBuilder<TEntity> entity) where TEntity : class
        {
            foreach (var property in entity.Metadata.GetProperties())
            {
                property.SetColumnName(property.GetColumnName().ToUpperInvariant());
            }
        }
    }
}
