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
using System.Security.Claims;
using Gurux.DLMS.AMI.Server.Internal;
using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.DLMS.AMI.Shared.DTOs.Authentication;
using Gurux.DLMS.AMI.Shared.Enums;
using Gurux.DLMS.AMI.Shared.Rest;
using Gurux.Service.Orm;
using Gurux.DLMS.AMI.Shared.DIs;
using Gurux.DLMS.AMI.Client.Shared;
using System.Linq.Expressions;
using Gurux.DLMS.AMI.Shared.DTOs.ContentType;
using Gurux.DLMS.AMI.Shared.DTOs.Content;
using Gurux.DLMS.AMI.Server.Midlewares;
using Gurux.DLMS.AMI.Shared.DTOs.Enums;
using Gurux.DLMS.AMI.Client.Shared.Enums;

namespace Gurux.DLMS.AMI.Server.Repository
{
    /// <inheritdoc />
    public class ContentTypeRepository : IContentTypeRepository
    {
        private readonly ClaimsPrincipal User;
        private readonly IGXHost _host;
        private readonly IGXEventsNotifier _eventsNotifier;
        private readonly IUserRepository _userRepository;
        private readonly IServiceProvider _serviceProvider;
        private readonly IContentTypeGroupRepository _contentTypeGroupRepository;
        private readonly ILocalizationRepository _localizationRepository;
        private readonly GXLanguageService _languageService;
        private readonly GXPerformanceSettings _performanceSettings;

        /// <summary>
        /// Constructor.
        /// </summary>
        public ContentTypeRepository(IGXHost host,
            IGXAmiContextAccessor contextAccessor,
            IUserRepository userRepository,
            GXLanguageService languageService,
            IServiceProvider serviceProvider,
            IContentTypeGroupRepository contentTypeGroupRepository,
            IGXEventsNotifier eventsNotifier,
            ILocalizationRepository localizationRepository,
            GXPerformanceSettings performanceSettings)
        {
            var user = contextAccessor?.User;
            if (user == null)
            {
                throw new UnauthorizedAccessException();
            }
            User = user;
            _host = host;
            _eventsNotifier = eventsNotifier;
            _userRepository = userRepository;
            _serviceProvider = serviceProvider;
            _contentTypeGroupRepository = contentTypeGroupRepository;
            _localizationRepository = localizationRepository;
            _languageService = languageService;
            _performanceSettings = performanceSettings;
        }

        /// <inheritdoc />
        public async Task<List<string>> GetUsersAsync(Guid? contentTypeId)
        {
            GXSelectArgs args = GXQuery.GetUsersByContentType(s => s.Id,
                ServerHelpers.GetUserId(User), contentTypeId);
            List<GXUser> users = await _host.Connection.SelectAsync<GXUser>(args);
            List<string> ret = users.Select(s => s.Id).ToList();
            if (User.IsInRole(GXRoles.Admin))
            {
                ret.AddDistinct(await _userRepository.GetUserIdsInRoleAsync([GXRoles.Admin]));
            }
            return ret;
        }

        /// <inheritdoc />
        public async Task<List<string>> GetUsersAsync(IEnumerable<Guid>? contentTypeIds)
        {
            GXSelectArgs args = GXQuery.GetUsersByContentTypes(s => s.Id,
                ServerHelpers.GetUserId(User), contentTypeIds);
            List<GXUser> users = await _host.Connection.SelectAsync<GXUser>(args);
            List<string> ret = users.Select(s => s.Id).ToList();
            if (User.IsInRole(GXRoles.Admin))
            {
                ret.AddDistinct(await _userRepository.GetUserIdsInRoleAsync([GXRoles.Admin]));
            }
            return ret;
        }

        /// <inheritdoc />
        public async Task DeleteAsync(
            IEnumerable<Guid> contentTypes,
            bool delete)
        {
            if (!User.IsInRole(GXRoles.Admin) &&
                !User.IsInRole(GXRoles.ContentType) &&
                !User.HasScope(GXContentTypePolicies.Delete))
            {
                throw new UnauthorizedAccessException();
            }
            GXSelectArgs arg = GXSelectArgs.Select<GXContentType>(a => a.Id, q => contentTypes.Contains(q.Id));
            List<GXContentType> list = _host.Connection.Select<GXContentType>(arg);
            DateTime now = DateTime.Now;
            Dictionary<GXContentType, List<string>> updates = new();
            foreach (GXContentType it in list)
            {
                it.Removed = now;
                List<string> users = await GetUsersAsync(it.Id);
                if (delete)
                {
                    await _host.Connection.DeleteAsync(GXDeleteArgs.DeleteById<GXContentType>(it.Id));
                }
                else
                {
                    _host.Connection.Update(GXUpdateArgs.Update(it, q => q.Removed));
                }
                updates[it] = users;
            }
            foreach (var it in updates)
            {
                var users = NotificationRepository.GetNotifiedUsers(_host.Connection, _performanceSettings,
                    it.Value, TargetType.ContentType, NotificationAction.Remove);
                if (users == null)
                {
                    break;
                }
                GXContentType tmp = new GXContentType() { Id = it.Key.Id };
                await _eventsNotifier.ContentTypeDelete(it.Value, new GXContentType[] { tmp });
            }
        }

        /// <inheritdoc />
        public async Task<GXContentType[]> ListAsync(
            ListContentTypes? request,
            ListContentTypesResponse? response,
            CancellationToken cancellationToken)
        {
            if (!User.IsInRole(GXRoles.Admin) &&
                !User.IsInRole(GXRoles.ContentType) &&
                !User.HasScope(GXContentTypePolicies.View))
            {
                throw new UnauthorizedAccessException();
            }
            GXSelectArgs arg;
            if (request != null && request.AllUsers && User.IsInRole(GXRoles.Admin))
            {
                //Admin can see all the content types.
                arg = GXSelectArgs.SelectAll<GXContentType>();
                if (request?.Exclude != null && request.Exclude.Any())
                {
                    arg.Where.And<GXContentType>(w => !request.Exclude.Contains(w.Id));
                }
                if (request?.Included != null && request.Included.Any())
                {
                    arg.Where.And<GXContentType>(w => request.Included.Contains(w.Id));
                }
            }
            else
            {
                string? userId = ServerHelpers.GetUserId(User);
                arg = GXQuery.GetContentTypesByUser(s => "*", userId, null,
                    request?.Exclude, request?.Included);
            }
            arg.Distinct = true;
            if (request?.Filter != null)
            {
                //User is already filtered. It can be removed.
                GXUser? orig = request.Filter.Creator;
                try
                {
                    request.Filter.Creator = null;
                    arg.Where.FilterBy(request.Filter);
                }
                finally
                {
                    request.Filter.Creator = orig;
                }
            }
            if (request != null && !string.IsNullOrEmpty(request.OrderBy))
            {
                arg.Descending = request.Descending;
                arg.OrderBy.Add<GXContentType>(request.OrderBy);
            }
            else
            {
                arg.OrderBy.Add<GXContentType>(q => q.Name);
            }
            arg.Distinct = true;
            if (request != null && request.Count != 0)
            {
                //Return total row count. This can be used for paging.
                GXSelectArgs total = GXSelectArgs.Select<GXContentType>(q => GXSql.DistinctCount(q.Id));
                total.Joins.Append(arg.Joins);
                total.Where.Append(arg.Where);
                if (response != null)
                {
                    response.Count = _host.Connection.SingleOrDefault<int>(total);
                }
                arg.Index = (UInt32)request.Index;
                arg.Count = (UInt32)request.Count;
            }
            GXContentType[] contentTypes = (await _host.Connection.SelectAsync<GXContentType>(arg)).ToArray();
            if (request?.Filter?.Active.GetValueOrDefault() == true)
            {
                //TODO: Fix active issue.
                contentTypes = contentTypes.Where(w => w.Active == true).ToArray();
            }
            if (request?.Select != null && request.Select.Contains(TargetType.ContentType))
            {
                //If client wants to know content type fields.
                foreach (var it in contentTypes)
                {
                    arg = GXSelectArgs.SelectAll<GXContentTypeField>(w => w.Parent == it);
                    arg.Columns.Exclude<GXContentType>(e => e.Fields);
                    arg.Columns.Add<GXContentType>();
                    arg.Joins.AddLeftJoin<GXContentTypeField, GXContentType>(j => j.Source, j => j.Id);
                    arg.OrderBy.Add<GXContentTypeField>(o => o.Order);
                    it.Fields = (await _host.Connection.SelectAsync<GXContentTypeField>(arg)).ToList();
                }
            }           
            if (response != null)
            {
                response.ContentTypes = contentTypes;
                if (response.Count == 0)
                {
                    response.Count = contentTypes.Length;
                }
            }
            return contentTypes;
        }

        /// <inheritdoc />
        public async Task<GXContentType> ReadAsync(Guid id)
        {
            if (!User.IsInRole(GXRoles.Admin) &&
                !User.IsInRole(GXRoles.ContentType) &&
                !User.HasScope(GXContentTypePolicies.View))
            {
                throw new UnauthorizedAccessException();
            }
            GXSelectArgs arg;
            if (User.IsInRole(GXRoles.Admin))
            {
                //Admin can see all the content types.
                arg = GXSelectArgs.SelectAll<GXContentType>(w => w.Id == id);
                arg.Joins.AddLeftJoin<GXContentType, GXContentTypeGroupContentType>(x => x.Id, y => y.ContentTypeId);
                arg.Joins.AddLeftJoin<GXContentTypeGroupContentType, GXContentTypeGroup>(j => j.ContentTypeGroupId, j => j.Id);
            }
            else
            {
                string userId = ServerHelpers.GetUserId(User);
                arg = GXQuery.GetContentTypesByUser(s => "*", userId, id);
                arg.Joins.AddInnerJoin<GXContentTypeGroupContentType, GXContentTypeGroup>(j => j.ContentTypeGroupId, j => j.Id);
            }
            arg.Columns.Add<GXContentTypeGroup>(s => new { s.Id, s.Name });
            arg.Columns.Exclude<GXContentTypeGroup>(e => new { e.ContentTypes, e.Creator });
            arg.Distinct = true;
            GXContentType contentType = await _host.Connection.SingleOrDefaultAsync<GXContentType>(arg);
            if (contentType == null)
            {
                throw new ArgumentException(Properties.Resources.UnknownTarget);
            }
            //Get fields.
            arg = GXSelectArgs.SelectAll<GXContentTypeField>(w => w.Parent == contentType);
            arg.Columns.Add<GXContentType>(s => new { s.Id, s.Name });
            arg.Joins.AddLeftJoin<GXContentTypeField, GXContentType>(y => y.Source, x => x.Id);
            arg.OrderBy.Add<GXContentTypeField>(o => o.Order);
            arg.Columns.Exclude<GXContentTypeField>(e => e.Parent);
            arg.Columns.Exclude<GXContentType>(e => e.Fields);
            contentType.Fields = (await _host.Connection.SelectAsync<GXContentTypeField>(arg)).ToList();
            //Get creator with own query. It's faster for some DBs.
            arg = GXSelectArgs.Select<GXUser>(s => new { s.Id, s.UserName }, q => q.Removed == null);
            arg.Distinct = true;
            arg.Joins.AddInnerJoin<GXContentType, GXUser>(s => s.Creator, o => o.Id);
            arg.Where.And<GXContentType>(w => w.Id == id);
            contentType.Creator = await _host.Connection.SingleOrDefaultAsync<GXUser>(arg);
            return contentType;
        }

        /// <inheritdoc />
        public async Task<Guid[]> UpdateAsync(
            IEnumerable<GXContentType> contentTypes,
            Expression<Func<GXContentType, object?>>? columns)
        {
            if (!User.IsInRole(GXRoles.Admin) &&
                !User.IsInRole(GXRoles.ContentType) &&
                !User.HasScope(GXContentTypePolicies.Edit))
            {
                throw new UnauthorizedAccessException();
            }
            DateTime now = DateTime.Now;
            GXUser creator = new GXUser() { Id = ServerHelpers.GetUserId(User) };
            List<Guid> list = new();
            Dictionary<GXContentType, List<string>> updates = new();
            foreach (GXContentType contentType in contentTypes)
            {
                if (string.IsNullOrEmpty(contentType.Name))
                {
                    throw new ArgumentException(Properties.Resources.InvalidName);
                }
                if (contentType.ContentTypeGroups == null || !contentType.ContentTypeGroups.Any())
                {
                    ListContentTypeGroups request = new ListContentTypeGroups()
                    {
                        Filter = new GXContentTypeGroup() { Default = true }
                    };
                    contentType.ContentTypeGroups = new List<GXContentTypeGroup>();
                    contentType.ContentTypeGroups.AddRange(await _contentTypeGroupRepository.ListAsync(request, null, CancellationToken.None));
                    if (!contentType.ContentTypeGroups.Any())
                    {
                        throw new ArgumentException(Properties.Resources.TargetMustBelongToOneGroup);
                    }
                }
                if (contentType.Id == Guid.Empty)
                {
                    contentType.Creator = creator;
                    contentType.CreationTime = now;
                    GXInsertArgs args = GXInsertArgs.Insert(contentType);
                    args.Exclude<GXContentType>(q => new
                    {
                        q.ContentTypeGroups,
                        q.Fields,
                    });
                    _host.Connection.Insert(args);
                    list.Add(contentType.Id);
                    AddContentTypeToContentTypeGroups(contentType.Id, contentType.ContentTypeGroups);
                    AddFieldsToContentType(contentType, contentType.Fields);
                    updates[contentType] = await GetUsersAsync(contentType.Id);
                }
                else
                {
                    GXSelectArgs m = GXSelectArgs.Select<GXContentType>(q => q.ConcurrencyStamp, where => where.Id == contentType.Id);
                    string updated = _host.Connection.SingleOrDefault<string>(m);
                    if (!string.IsNullOrEmpty(updated) && updated != contentType.ConcurrencyStamp)
                    {
                        throw new ArgumentException(Properties.Resources.ContentEdited);
                    }
                    contentType.Updated = now;
                    contentType.ConcurrencyStamp = Guid.NewGuid().ToString();
                    GXUpdateArgs args = GXUpdateArgs.Update(contentType, columns);
                    args.Exclude<GXContentType>(q => new
                    {
                        q.CreationTime,
                        q.ContentTypeGroups,
                        q.Fields
                    });
                    if (!User.IsInRole(GXRoles.Admin) ||
                        contentType.Creator == null ||
                        string.IsNullOrEmpty(contentType.Creator.Id))
                    {
                        //Only admin can update the creator.
                        args.Exclude<GXContentType>(q => q.Creator);
                    }
                    _host.Connection.Update(args);
                    //Map content type groups to contentType.
                    List<GXContentTypeGroup> contentTypeGroups;
                    using (IServiceScope scope = _serviceProvider.CreateScope())
                    {
                        IContentTypeGroupRepository contentTypeGroupRepository = scope.ServiceProvider.GetRequiredService<IContentTypeGroupRepository>();
                        contentTypeGroups = await contentTypeGroupRepository.GetJoinedContentTypeGroups(contentType.Id);
                    }
                    var comparer = new UniqueComparer<GXContentTypeGroup, Guid>();
                    List<GXContentTypeGroup> removedContentTypeGroups = contentTypeGroups.Except(contentType.ContentTypeGroups, comparer).ToList();
                    List<GXContentTypeGroup> addedContentTypeGroups = contentType.ContentTypeGroups.Except(contentTypeGroups, comparer).ToList();
                    if (removedContentTypeGroups.Any())
                    {
                        RemoveContentTypesFromContentTypeGroup(contentType.Id, removedContentTypeGroups);
                    }
                    if (addedContentTypeGroups.Any())
                    {
                        AddContentTypeToContentTypeGroups(contentType.Id, addedContentTypeGroups);
                    }
                    updates[contentType] = await GetUsersAsync(contentType.Id);
                    //Update fields.
                    GXContentType target = await ReadAsync(contentType.Id);
                    if (contentType.Fields != null)
                    {
                        var comparer4 = new UniqueComparer<GXContentTypeField, Guid>();
                        List<GXContentTypeField>? addedFields;
                        if (target.Fields == null)
                        {
                            //If new field is added.
                            AddFieldsToContentType(contentType, contentType.Fields);
                        }
                        else
                        {
                            List<GXContentTypeField> updatedFields = contentType.Fields.Union(target.Fields, comparer4).ToList();
                            List<GXContentTypeField> removedFields = target.Fields.Except(contentType.Fields, comparer4).ToList();
                            addedFields = contentType.Fields.Except(target.Fields, comparer4).ToList();
                            if (removedFields != null && removedFields.Any())
                            {
                                RemoveFieldsFromContentType(removedFields);
                                updatedFields.RemoveAll(w => removedFields.Contains(w));
                            }
                            //Update order.
                            if (contentType.Fields != null)
                            {
                                int index = 0;
                                foreach (var it in contentType.Fields)
                                {
                                    it.Order = index;
                                    ++index;
                                }
                            }

                            if (addedFields.Any())
                            {
                                AddFieldsToContentType(contentType, addedFields);
                                foreach (var field in addedFields)
                                {
                                    await AddNewField(field);
                                }
                                updatedFields.RemoveAll(w => addedFields.Contains(w));
                            }
                            if (updatedFields.Any())
                            {
                                foreach (var it in updatedFields)
                                {
                                    GXUpdateArgs u = GXUpdateArgs.Update(it);
                                    u.Exclude<GXContentTypeField>(e => new
                                    {
                                        e.CreationTime,
                                        e.Parent
                                    });
                                    await _host.Connection.UpdateAsync(u);
                                }
                            }
                        }
                    }
                }
            }
            foreach (var it in updates)
            {
                await _eventsNotifier.ContentTypeUpdate(it.Value, new GXContentType[] { it.Key });
            }
            return list.ToArray();
        }

        /// <summary>
        /// Add new content type field to all existing contents.
        /// </summary>
        private async Task AddNewField(GXContentTypeField value)
        {
            DateTime now = DateTime.Now;
            List<GXContentField> list = new();
            GXSelectArgs arg = GXSelectArgs.SelectAll<GXContent>(w => w.Removed == null);
            arg.Columns.Add<GXContentField>();
            arg.Columns.Add<GXContentType>();
            arg.Joins.AddLeftJoin<GXContent, GXContentField>(x => x.Id, y => y.Parent);
            arg.Joins.AddLeftJoin<GXContentField, GXContentType>(x => x.Type, y => y.Id);
            var contents = (await _host.Connection.SelectAsync<GXContent>(arg)).ToList();
            foreach (var content in contents)
            {
                if (content.Fields == null)
                {
                    content.Fields = new List<GXContentField>();
                }
                if (!content.Fields.Where(w => w.Type?.Id == value.Id).Any())
                {
                    var item = new GXContentField()
                    {
                        Parent = content,
                        Type = value,
                    };
                    list.Add(item);
                }
            }
            _host.Connection.Insert(GXInsertArgs.InsertRange(list));
            /*
            //Get content type fields and update the new ones.
            arg = GXSelectArgs.SelectAll<GXContentType>(w => w.Removed == null);
            arg.Columns.Add<GXContentType>();
            arg.Columns.Add<GXGXContentTypeValue>();
            arg.Joins.AddLeftJoin<GXContentType, GXContentType>(x => x.Parent, y => y.Id);
            arg.Joins.AddLeftJoin<GXContentType, GXGXContentTypeValue>(x => x.Id, y => y.Parent);
            arg.OrderBy.Add<GXContentType>(o => o.Id);
            arg.Where.And<GXContentType>(w => w.Id == value.Id);
            var fields = (await _host.Connection.SelectAsync<GXContentType>(arg)).ToList();
            foreach (var field in fields)
            {
                if (!content.Fields.Where(w => w.Id == field.Id).Any())
                {

                }
            }
            */
        }

        /// <summary>
        /// Map content type group to user groups.
        /// </summary>
        /// <param name="contentTypeId">ContentType ID.</param>
        /// <param name="groups">Group IDs of the content type groups where the content type is added.</param>
        public void AddContentTypeToContentTypeGroups(Guid contentTypeId, IEnumerable<GXContentTypeGroup> groups)
        {
            DateTime now = DateTime.Now;
            List<GXContentTypeGroupContentType> list = new();
            foreach (GXContentTypeGroup it in groups)
            {
                list.Add(new GXContentTypeGroupContentType()
                {
                    ContentTypeId = contentTypeId,
                    ContentTypeGroupId = it.Id,
                    CreationTime = now
                });
            }
            _host.Connection.Insert(GXInsertArgs.InsertRange(list));
        }

        /// <summary>
        /// Remove map between content type group and contentType.
        /// </summary>
        /// <param name="contentTypeId">Content type ID.</param>
        /// <param name="groups">Group IDs of the content type groups where the content type is removed.</param>
        public void RemoveContentTypesFromContentTypeGroup(Guid contentTypeId, IEnumerable<GXContentTypeGroup> groups)
        {
            foreach (GXContentTypeGroup it in groups)
            {
                _host.Connection.Delete(GXDeleteArgs.Delete<GXContentTypeGroupContentType>(w => w.ContentTypeId == contentTypeId && w.ContentTypeGroupId == it.Id));
            }
        }

        /// <summary>
        /// Add field to content type.
        /// </summary>
        /// <param name="contentType">ContentType where fields are joined.</param>
        /// <param name="fields">Content type fields.</param>
        public void AddFieldsToContentType(GXContentType contentType, IEnumerable<GXContentTypeField>? fields)
        {
            if (fields != null)
            {
                int order = 0;
                DateTime now = DateTime.Now;
                foreach (GXContentTypeField it in fields)
                {
                    if (string.IsNullOrEmpty(it.Name))
                    {
                        throw new ArgumentException(Properties.Resources.InvalidName);
                    }
                    it.CreationTime = now;
                    it.Parent = contentType;
                    it.Order = order;
                    ++order;
                }
                _host.Connection.Insert(GXInsertArgs.InsertRange(fields));
            }
        }

        /// <summary>
        /// Remove map between field and content type.
        /// </summary>
        /// <param name="fields">Content type fields.</param>
        public void RemoveFieldsFromContentType(IEnumerable<GXContentTypeField> fields)
        {
            foreach (GXContentTypeField it in fields)
            {
                //Remove field from content fields as well.
                var del = GXDeleteArgs.Delete<GXContentField>(w => w.Type == it);
                _host.Connection.Delete(del);
                del = GXDeleteArgs.Delete<GXContentTypeField>(w => w.Id == it.Id);
                _host.Connection.Delete(del);
            }
        }
    }
}
