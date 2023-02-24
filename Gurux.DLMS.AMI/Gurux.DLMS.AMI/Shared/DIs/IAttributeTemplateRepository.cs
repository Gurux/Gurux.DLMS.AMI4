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

using System.Linq.Expressions;
using System.Security.Claims;
using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.DLMS.AMI.Shared.Rest;

namespace Gurux.DLMS.AMI.Shared.DIs
{
    /// <summary>
    /// This interface is used to handle attribute templates.
    /// </summary>
    public interface IAttributeTemplateRepository
    {
        /// <summary>
        /// List attribute templates.
        /// </summary>
        /// <returns>Attribute templates.</returns>
        Task<GXAttributeTemplate[]> ListAsync(ClaimsPrincipal user,
            ListAttributeTemplates? request,
            ListAttributeTemplatesResponse? response,
            CancellationToken cancellationToken);

        /// <summary>
        /// Read attribute template information.
        /// </summary>
        /// <param name="user">Current user.</param>
        /// <param name="id">Attribute template id.</param>
        /// <returns>Attribute template information.</returns>
        Task<GXAttributeTemplate> ReadAsync(ClaimsPrincipal user, Guid id);

        /// <summary>
        /// Update attribute template(s).
        /// </summary>
        /// <param name="user">Current user.</param>
        /// <param name="templates">Updated attribute template(s).</param>
        /// <param name="columns">Updated columns(s).</param>
        Task<Guid[]> UpdateAsync(
            ClaimsPrincipal user, 
            IEnumerable<GXAttributeTemplate> templates,
            Expression<Func<GXAttributeTemplate, object?>>? columns = null);

        /// <summary>
        /// Delete attribute template(s).
        /// </summary>
        /// <param name="user">Current user.</param>
        /// <param name="templates">AttributeTemplate(s) to delete.</param>
        /// <param name="delete">If true, attribute templates are deleted, not marked as removed.</param>
        Task DeleteAsync(ClaimsPrincipal user, IEnumerable<Guid> templates, bool delete);

        /// <summary>
        /// Get all users that can access this attribute template.
        /// </summary>
        /// <param name="user">Current user.</param>
        /// <param name="templateId">Attribute template id.</param>
        /// <returns>Collection of User IDs.</returns>
        Task<List<string>> GetUsersAsync(ClaimsPrincipal user, Guid? templateId);

        /// <summary>
        /// Get all users that can access attribute templates.
        /// </summary>
        /// <param name="user">Current user.</param>
        /// <param name="templateIds">Attribute template ids.</param>
        /// <returns>Collection of User IDs.</returns>
        Task<List<string>> GetUsersAsync(ClaimsPrincipal user, IEnumerable<Guid>? templateIds);
    }
}
