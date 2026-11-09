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

using Microsoft.AspNetCore.Identity;

namespace Gurux.DLMS.AMI.Server.Midlewares
{
    /// <summary>
    /// Provides custom error message descriptions for identity-related operations, such as user name and email
    /// duplication, by overriding the default messages in the Identity framework.
    /// </summary>
    /// <remarks>Override members of this class to customize the error messages returned by identity
    /// operations, such as registration and password validation. This is useful for localizing messages or providing
    /// more user-friendly feedback in authentication workflows. Typically used in ASP.NET Core Identity to tailor
    /// validation errors to application requirements.</remarks>
    public class CustomIdentityErrorDescriber : IdentityErrorDescriber
    {
        /// <summary>
        /// Returns an IdentityError indicating that the specified user name is already in use.
        /// </summary>
        /// <param name="userName">The user name that caused the duplication error. Cannot be null.</param>
        /// <returns>An IdentityError with a code and description indicating that the user name is already taken.</returns>
        public override IdentityError DuplicateUserName(string userName)
        {
            return new IdentityError
            {
                Code = nameof(DuplicateUserName),
                Description = string.Format(Properties.Resources.UsernameIsAlreadyTaken, userName)
            };
        }

        /// <summary>
        /// Returns an IdentityError indicating that the specified email address is already in use.
        /// </summary>
        /// <param name="email">The email address that caused the duplication error. Cannot be null.</param>
        /// <returns>An IdentityError with a code and description indicating that the email address is already taken.</returns>
        public override IdentityError DuplicateEmail(string email)
        {
            return new IdentityError
            {
                Code = nameof(DuplicateEmail),
                Description = string.Format(Properties.Resources.EmailIsAlreadyTaken, email)
            };
        }
        /// <inheritdoc />
        public override IdentityError InvalidEmail(string? email)
        {
            return base.InvalidEmail(email);
        }
        /// <inheritdoc />
        public override IdentityError PasswordRequiresDigit()
        {
            return base.PasswordRequiresDigit();
        }

        /// <inheritdoc />
        public override IdentityError PasswordMismatch()
        {
            return base.PasswordMismatch();
        }

        /// <inheritdoc />
        public override IdentityError PasswordRequiresLower()
        {
            return base.PasswordRequiresLower();
        }

        /// <inheritdoc />
        public override IdentityError PasswordRequiresUpper()
        {
            return base.PasswordRequiresUpper();
        }
        /// <inheritdoc />
        public override IdentityError PasswordTooShort(int length)
        {
            return base.PasswordTooShort(length);
        }
    }
}
