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

using Gurux.DLMS.AMI.Client.Shared;
using Gurux.DLMS.AMI.Server.Internal;
using Gurux.DLMS.AMI.Server.Midlewares;
using Gurux.DLMS.AMI.Shared.DIs;
using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.DLMS.AMI.Shared.Enums;
using Gurux.DLMS.AMI.Shared.Rest;
using Gurux.Service.Orm;
using System.Security.Claims;

namespace Gurux.DLMS.AMI.Server.Repository
{
    /// <inheritdoc />
    class EnumTypeRepository : IEnumTypeRepository
    {
        private readonly ClaimsPrincipal? User;
        private readonly IGXHost _host;
        private readonly GXLanguageService _languageService;
        private readonly ILocalizationRepository _localizationRepository;
        /// <summary>
        /// Constructor.
        /// </summary>
        public EnumTypeRepository(IGXHost host,
            IGXAmiContextAccessor contextAccessor,
            IGXEventsNotifier eventsNotifier,
            GXLanguageService languageService,
            ILocalizationRepository localizationRepository,
            GXPerformanceSettings performanceSettings)
        {
            User = contextAccessor?.User;
            _host = host;
            _languageService = languageService;
            _localizationRepository = localizationRepository;
        }

        public async Task<int> GetLogTypeAsync(string type, string name)
        {
            if (User == null)
            {
                throw new UnauthorizedAccessException();
            }
            if (string.IsNullOrEmpty(type))
            {
                throw new ArgumentNullException(nameof(type));
            }
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentNullException(nameof(name));
            }
            GXSelectArgs arg = GXSelectArgs.SelectAll<GXEnumType>(w => w.Type == type && w.Name == name);
            arg.Distinct = true;
            GXEnumType lt = await _host.Connection.SingleOrDefaultAsync<GXEnumType>(arg);
            if (lt == null)
            {
                lt = new GXEnumType() { Type = type, Name = name };
                await _host.Connection.InsertAsync(GXInsertArgs.Insert(lt));
            }
            return lt.Id;
        }

        public async Task<IEnumerable<GXEnumType>> ListAsync(ListEnumTypes? request,
           ListEnumTypesResponse? response = null,
           CancellationToken cancellationToken = default)
        {
            GXSelectArgs arg = GXSelectArgs.SelectAll<GXEnumType>();
            if (request != null)
            {
                arg.Where.FilterBy(request.Filter);
                if (request.Exclude != null && request.Exclude.Any())
                {
                    arg.Where.And<GXEnumType>(w => !request.Exclude.Contains(w.Id));
                }
                if (request?.Included != null && request.Included.Any())
                {
                    arg.Where.And<GXEnumType>(w => request.Included.Contains(w.Id));
                }
            }
            if (request != null && !string.IsNullOrEmpty(request.OrderBy))
            {
                arg.Descending = request.Descending;
                arg.OrderBy.Add<GXEnumType>(request.OrderBy);
            }
            else
            {
                arg.OrderBy.Add<GXEnumType>(q => q.Name);
                arg.Descending = false;
            }
            arg.Distinct = true;
            if (request != null && request.Count != 0)
            {
                //Return total row count. This can be used for paging.
                GXSelectArgs total = GXSelectArgs.Select<GXEnumType>(q => GXSql.DistinctCount(q.Id));
                total.Where.Append(arg.Where);
                if (response != null)
                {
                    response.Count = _host.Connection.SingleOrDefault<int>(total);
                }
                arg.Index = (UInt32)request.Index;
                arg.Count = (UInt32)request.Count;
            }
            GXEnumType[] types = (await _host.Connection.SelectAsync<GXEnumType>(arg)).ToArray();
            if (response != null)
            {
                response.Types = types;
            }
            return types;
        }

        public async Task DeleteAsync(string type)
        {
            if (User == null ||
                !User.IsInRole(GXRoles.Admin) && !User.IsInRole(GXRoles.User))
            {
                throw new UnauthorizedAccessException();
            }
            GXDeleteArgs args = GXDeleteArgs.Delete<GXEnumType>(w => w.Type == type);
            await _host.Connection.DeleteAsync(args);
        }

        public async Task<int> GetLogTypeAsync(string type, Enum value)
        {
            if (User == null)
            {
                throw new UnauthorizedAccessException();
            }
            if (string.IsNullOrEmpty(type))
            {
                throw new ArgumentNullException(nameof(type));
            }
            var name = value.ToString();
            Enum.GetValues(value.GetType());
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentNullException(nameof(name));
            }
            GXSelectArgs arg = GXSelectArgs.SelectAll<GXEnumType>(w => w.Type == type && w.Name == name);
            arg.Distinct = true;
            GXEnumType lt = await _host.Connection.SingleOrDefaultAsync<GXEnumType>(arg);
            if (lt == null)
            {
                lt = new GXEnumType() { Type = type, Name = name, Value = Convert.ToInt32(value) };
                await _host.Connection.InsertAsync(GXInsertArgs.Insert(lt));
            }
            return lt.Id;
        }
    }
}
