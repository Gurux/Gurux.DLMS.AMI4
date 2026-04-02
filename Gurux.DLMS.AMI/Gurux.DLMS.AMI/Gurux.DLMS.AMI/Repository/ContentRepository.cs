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
using Gurux.DLMS.AMI.Shared.DTOs.Content;
using Gurux.DLMS.AMI.Shared.DTOs.User;
using Gurux.DLMS.AMI.Shared.DTOs.ContentType;
using Gurux.DLMS.AMI.Server.Midlewares;
using Gurux.DLMS.AMI.Shared.DTOs.Enums;
using Gurux.DLMS.AMI.Client.Shared.Enums;

namespace Gurux.DLMS.AMI.Server.Repository
{
    /// <inheritdoc />
    public class ContentRepository : IContentRepository
    {
        private readonly ClaimsPrincipal User;
        private readonly IGXHost _host;
        private readonly IGXEventsNotifier _eventsNotifier;
        private readonly IUserRepository _userRepository;
        private readonly IServiceProvider _serviceProvider;
        private readonly IContentGroupRepository _contentGroupRepository;
        private readonly ILocalizationRepository _localizationRepository;
        private readonly GXLanguageService _languageService;
        private readonly GXPerformanceSettings _performanceSettings;

        /// <summary>
        /// Constructor.
        /// </summary>
        public ContentRepository(IGXHost host,
            IGXAmiContextAccessor contextAccessor,
            IUserRepository userRepository,
            IServiceProvider serviceProvider,
            IContentGroupRepository contentGroupRepository,
            IGXEventsNotifier eventsNotifier,
            GXLanguageService languageService,
            ILocalizationRepository localizationRepository,
            GXPerformanceSettings performanceSettings)
        {
            var user = contextAccessor?.User;
            if (user != null)
            {
                if ((!user.IsInRole(GXRoles.Admin) &&
                    !user.IsInRole(GXRoles.ContentManager)))
                {
                    throw new UnauthorizedAccessException();
                }
                User = user;
            }
            _host = host;
            _eventsNotifier = eventsNotifier;
            _userRepository = userRepository;
            _serviceProvider = serviceProvider;
            _contentGroupRepository = contentGroupRepository;
            _languageService = languageService;
            _localizationRepository = localizationRepository;
            _performanceSettings = performanceSettings;
        }

        /// <inheritdoc />
        public async Task<List<string>> GetUsersAsync(Guid? contentId)
        {
            GXSelectArgs args = GXQuery.GetUsersByContent(s => s.Id, ServerHelpers.GetUserId(User), contentId);
            List<GXUser> users = await _host.Connection.SelectAsync<GXUser>(args);
            List<string> ret = users.Select(s => s.Id).ToList();
            if (User.IsInRole(GXRoles.Admin))
            {
                ret.AddDistinct(await _userRepository.GetUserIdsInRoleAsync([GXRoles.Admin]));
            }
            return ret;
        }

        /// <inheritdoc />
        public async Task<List<string>> GetUsersAsync(IEnumerable<Guid>? contentIds)
        {
            GXSelectArgs args = GXQuery.GetUsersByContents(s => s.Id, ServerHelpers.GetUserId(User), contentIds);
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
            IEnumerable<Guid> contents,
            bool delete)
        {
            if (!User.IsInRole(GXRoles.Admin) &&
                !User.IsInRole(GXRoles.ContentManager))
            {
                throw new UnauthorizedAccessException();
            }
            GXSelectArgs arg = GXSelectArgs.Select<GXContent>(a => new { a.Id }, q => contents.Contains(q.Id));
            arg.Columns.Add<GXContentType>(s => new { s.Id, s.Name });
            arg.Joins.AddInnerJoin<GXContent, GXContentType>(j => j.Type, j => j.Id);
            List<GXContent> list = _host.Connection.Select<GXContent>(arg);
            DateTime now = DateTime.Now;
            Dictionary<GXContent, List<string>> updates = new();
            foreach (GXContent it in list)
            {
                it.Removed = now;
                List<string> users = await GetUsersAsync(it.Id);
                if (delete)
                {
                    await _host.Connection.DeleteAsync(GXDeleteArgs.DeleteById<GXContent>(it.Id));
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
                    it.Value, TargetType.Content, NotificationAction.Remove);
                if (users == null)
                {
                    break;
                }
                GXContent tmp = new GXContent() { Id = it.Key.Id, Name = it.Key.Type?.Name };
                await _eventsNotifier.ContentDelete(users, [tmp]);
            }
        }

        /// <inheritdoc />
        public async Task<GXContent[]> ListAsync(

            ListContents? request,
            ListContentsResponse? response,
            CancellationToken cancellationToken)
        {
            GXSelectArgs arg;
            if (request != null && request.AllUsers && User.IsInRole(GXRoles.Admin))
            {
                //Admin can see all the contents.
                arg = GXSelectArgs.SelectAll<GXContent>();
                if (request?.Exclude != null && request.Exclude.Any())
                {
                    arg.Where.And<GXContent>(w => !request.Exclude.Contains(w.Id));
                }
                if (request?.Included != null && request.Included.Any())
                {
                    arg.Where.And<GXContent>(w => request.Included.Contains(w.Id));
                }
            }
            else
            {
                string? userId = ServerHelpers.GetUserId(User);
                arg = GXQuery.GetContentsByUser(s => "*", userId,
                    null, request?.Exclude, request?.Included);
            }
            arg.Columns.Exclude<GXContent>(e => e.User);
            arg.Columns.Add<GXUser>(s => new { s.Id, s.UserName });
            arg.Joins.AddInnerJoin<GXContent, GXUser>(j => j.Creator, j => j.Id);
            arg.Columns.Exclude<GXUser>(s => s.Contents);
            arg.Columns.Add<GXContentType>(s => new { s.Id, s.Name, s.Path });
            arg.Joins.AddInnerJoin<GXContent, GXContentType>(j => j.Type, j => j.Id);
            if (request?.Filter != null)
            {
                //User is already filtered. It can be removed.
                GXUser? orig = request.Filter.User;
                GXContentType? origType = request.Filter.Type;
                try
                {
                    request.Filter.User = null;
                    //   request.Filter.Type = null;
                    arg.Where.FilterBy(request.Filter);
                }
                finally
                {
                    request.Filter.User = orig;
                    request.Filter.Type = origType;
                }
            }
            if (request != null && !string.IsNullOrEmpty(request.OrderBy))
            {
                arg.Descending = request.Descending;
                arg.OrderBy.Add<GXContent>(request.OrderBy);
            }
            else
            {
                arg.OrderBy.Add<GXContent>(q => q.CreationTime);
                arg.Descending = true;
            }
            arg.Distinct = true;
            if (request != null && request.Count != 0)
            {
                //Return total row count. This can be used for paging.
                GXSelectArgs total = GXSelectArgs.Select<GXContent>(q => GXSql.DistinctCount(q.Id));
                total.Joins.Append(arg.Joins);
                total.Where.Append(arg.Where);
                if (response != null)
                {
                    response.Count = _host.Connection.SingleOrDefault<int>(total);
                }
                arg.Index = (UInt32)request.Index;
                arg.Count = (UInt32)request.Count;
            }
            GXContent[] contents = (await _host.Connection.SelectAsync<GXContent>(arg)).ToArray();
            if (request?.Filter?.Active.GetValueOrDefault() == true)
            {
                //TODO: Fix active issue.
                contents = contents.Where(w => w.Active == true).ToArray();
            }           
            if (response != null)
            {
                response.Contents = contents;
                if (response.Count == 0)
                {
                    response.Count = contents.Length;
                }
            }
            return contents;
        }

        /// <inheritdoc />
        public async Task<GXContent> ReadAsync(Guid id)
        {
            GXSelectArgs arg;
            if (User.IsInRole(GXRoles.Admin))
            {
                //Admin can see all the contents.
                arg = GXSelectArgs.SelectAll<GXContent>(w => w.Id == id);
                arg.Joins.AddLeftJoin<GXContent, GXContentGroupContent>(x => x.Id, y => y.ContentId);
                arg.Joins.AddLeftJoin<GXContentGroupContent, GXContentGroup>(j => j.ContentGroupId, j => j.Id);
            }
            else
            {
                string userId = ServerHelpers.GetUserId(User);
                arg = GXQuery.GetContentsByUser(s => "*", userId, id);
                arg.Joins.AddInnerJoin<GXContentGroupContent, GXContentGroup>(j => j.ContentGroupId, j => j.Id);
            }
            arg.Columns.Add<GXContentType>();
            arg.Columns.Add<GXContentTypeField>();
            arg.Columns.Exclude<GXContentTypeField>(e => e.Parent);
            arg.Joins.AddInnerJoin<GXContent, GXContentType>(x => x.Type, y => y.Id);
            arg.Joins.AddLeftJoin<GXContentType, GXContentTypeField>(x => x.Id, y => y.Parent);

            arg.Columns.Add<GXContentGroup>();
            arg.Columns.Exclude<GXContentGroup>(e => e.Contents);
            arg.Distinct = true;
            GXContent content = await _host.Connection.SingleOrDefaultAsync<GXContent>(arg);
            if (content == null)
            {
                throw new ArgumentException(Properties.Resources.UnknownTarget);
            }
            //Get fields.
            arg = GXSelectArgs.SelectAll<GXContentField>(w => w.Parent == content);
            arg.OrderBy.Add<GXContentTypeField>(o => o.Order);
            arg.Columns.Add<GXContentTypeField>();
            arg.Columns.Add<GXContentFieldValue>();
            arg.Columns.Exclude<GXContentField>(e => e.Parent);
            arg.Columns.Exclude<GXContentTypeField>(e => e.Parent);
            arg.Joins.AddLeftJoin<GXContentField, GXContentFieldValue>(x => x.Id, y => y.Parent);
            arg.Joins.AddLeftJoin<GXContentField, GXContentTypeField>(x => x.Type, y => y.Id);
            content.Fields = (await _host.Connection.SelectAsync<GXContentField>(arg)).ToList();

            //Get creator with own query. It's faster for some DBs.
            arg = GXSelectArgs.Select<GXUser>(s => new { s.Id, s.UserName }, q => q.Removed == null);
            arg.Distinct = true;
            arg.Joins.AddInnerJoin<GXContent, GXUser>(s => s.Creator, o => o.Id);
            arg.Where.And<GXContent>(w => w.Id == id);
            content.Creator = await _host.Connection.SingleOrDefaultAsync<GXUser>(arg);
            return content;
        }

        /// <inheritdoc />
        public async Task<Guid[]> UpdateAsync(
            IEnumerable<GXContent> contents,
            Expression<Func<GXContent, object?>>? columns)
        {
            DateTime now = DateTime.Now;
            GXUser creator = new GXUser() { Id = ServerHelpers.GetUserId(User) };
            List<Guid> list = new();
            Dictionary<GXContent, List<string>> updates = new();
            foreach (GXContent content in contents)
            {
                if (content.ContentGroups == null || !content.ContentGroups.Any())
                {
                    ListContentGroups request = new ListContentGroups()
                    {
                        Filter = new GXContentGroup() { Default = true }
                    };
                    content.ContentGroups = [.. await _contentGroupRepository.ListAsync(request, null, CancellationToken.None)];
                    if (!content.ContentGroups.Any())
                    {
                        throw new ArgumentException(Properties.Resources.TargetMustBelongToOneGroup);
                    }
                }
                //Check that unique content fields don't exists.
                if (content.Fields != null)
                {
                    foreach (var it in content.Fields)
                    {
                        if (it.Type == null)
                        {
                            throw new ArgumentException("Invalid content type.");
                        }
                        if (!string.IsNullOrEmpty(it.Type.Maximum))
                        {
                            if (double.TryParse(it.Type.Maximum, out double max))
                            {
                                if (it.Value != null && it.Value.Length > max)
                                {
                                    throw new ArgumentException(string.Format("The {0} value is too large. The maximum allowed value is {1}.", it.Type.Name, it.Type.Maximum));
                                }

                            }
                        }
                        if (!string.IsNullOrEmpty(it.Type.Minimum))
                        {
                            if (double.TryParse(it.Type.Minimum, out double min))
                            {
                                if (it.Value == null || it.Value.Length < min)
                                {
                                    throw new ArgumentException(string.Format("The {0} value is too small. The minimum allowed value is {1}.", it.Type.Name, it.Type.Minimum));
                                }
                            }
                        }
                        if (it.Type.Unique == true)
                        {
                            GXSelectArgs args = GXSelectArgs.SelectAll<GXContent>();
                            args.Columns.Add<GXContentField>();
                            args.Where.And<GXContentField>(w => w.Type == it.Type);
                            args.Joins.AddInnerJoin<GXContent, GXContentField>(j => j.Id, j => j.Parent);
                            args.Joins.AddInnerJoin<GXContentField, GXContentTypeField>(j => j.Type, j => j.Id);
                            var items = _host.Connection.Select<GXContent>(args);
                            foreach (var it2 in items)
                            {
                                if (it2.Fields != null)
                                {
                                    foreach (var it3 in it2.Fields)
                                    {
                                        if (it3.Id != it.Id && string.Compare(it3.Value, it.Value, true) == 0)
                                        {
                                            throw new ArgumentException("The content already exists.");
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                //The first field value is used as a content name.
                content.Name = content.Fields?.FirstOrDefault()?.Value;
                //The max length of the content name is 128 chars.
                if (content.Name != null && content.Name.Length > 128)
                {
                    content.Name = content.Name.Substring(0, 128);
                }
                if (content.Id == Guid.Empty)
                {
                    content.Creator = creator;
                    content.CreationTime = now;
                    if (content.User != null)
                    {
                        content.User = new GXUser() { Id = content.User.Id };
                    }
                    if (content.Type?.Id == null)
                    {
                        throw new ArgumentException("Content type is invalid.");
                    }
                    else
                    {
                        //Check that content type exists.
                        GXSelectArgs arg = GXSelectArgs.Select<GXContentType>(s => s.Id, w => w.Id == content.Type.Id);
                        if ((await _host.Connection.SelectByIdAsync<GXContentType>(content.Type.Id, default)) == null)
                        {
                            throw new ArgumentException("Content type is invalid.");
                        }
                    }
                    content.Type = new GXContentType() { Id = content.Type.Id };
                    if (content.Fields != null)
                    {
                        foreach (GXContentField it in content.Fields)
                        {
                            if (it.Type.FieldType == FieldType.AutoIncrement)
                            {
                                //Find last value and increse it.
                                GXSelectArgs args2 = GXSelectArgs.Select<GXContentField>(s => new { s.Value, s.CreationTime });
                                args2.Where.And<GXContentTypeField>(w => w.Id == it.Type.Id);
                                args2.Joins.AddInnerJoin<GXContentField, GXContentTypeField>(j => j.Type, j => j.Id);
                                args2.Descending = true;
                                args2.OrderBy.Add<GXContentField>(o => o.CreationTime);
                                args2.Count = 1;
                                var item = _host.Connection.SingleOrDefault<GXContentField>(args2);
                                if (!string.IsNullOrEmpty(item?.Value))
                                {
                                    it.Value = (1 + long.Parse(item.Value)).ToString();
                                }
                                else
                                {
                                    it.Value = it.Type.Default;
                                }
                            }
                        }
                        //The first field value is used as a content name.
                        content.Name = content.Fields.FirstOrDefault()?.Value;
                    }
                    GXInsertArgs args = GXInsertArgs.Insert(content);
                    args.Exclude<GXContent>(q => new
                    {
                        q.ContentGroups,
                        q.Updated,
                        q.Fields,
                    });
                    _host.Connection.Insert(args);
                    list.Add(content.Id);
                    AddContentToContentGroups(content.Id, content.ContentGroups);
                    AddContentFields(content.Id, content.Fields);
                    updates[content] = await GetUsersAsync(content.Id);
                }
                else
                {
                    GXSelectArgs m = GXSelectArgs.Select<GXContent>(q => q.ConcurrencyStamp, where => where.Id == content.Id);
                    string updated = _host.Connection.SingleOrDefault<string>(m);
                    if (!string.IsNullOrEmpty(updated) && updated != content.ConcurrencyStamp)
                    {
                        throw new ArgumentException(Properties.Resources.ContentEdited);
                    }
                    content.Updated = now;
                    content.ConcurrencyStamp = Guid.NewGuid().ToString();
                    GXUpdateArgs args = GXUpdateArgs.Update(content, columns);
                    args.Exclude<GXContent>(q => new
                    {
                        q.CreationTime,
                        q.Type,
                        q.ContentGroups,
                    });
                    if (!User.IsInRole(GXRoles.Admin) ||
                    content.Creator == null ||
                    string.IsNullOrEmpty(content.Creator.Id))
                    {
                        //Only admin can update the creator.
                        args.Exclude<GXContent>(q => q.Creator);
                    }
                    _host.Connection.Update(args);
                    //Map content groups to content.
                    List<GXContentGroup> contentGroups;
                    using (IServiceScope scope = _serviceProvider.CreateScope())
                    {
                        IContentGroupRepository contentGroupRepository = scope.ServiceProvider.GetRequiredService<IContentGroupRepository>();
                        contentGroups = await contentGroupRepository.GetJoinedContentGroups(content.Id);
                    }
                    var comparer = new UniqueComparer<GXContentGroup, Guid>();
                    List<GXContentGroup> removedContentGroups = contentGroups.Except(content.ContentGroups, comparer).ToList();
                    List<GXContentGroup> addedContentGroups = content.ContentGroups.Except(contentGroups, comparer).ToList();
                    if (removedContentGroups.Any())
                    {
                        RemoveContentsFromContentGroup(content.Id, removedContentGroups);
                    }
                    if (addedContentGroups.Any())
                    {
                        AddContentToContentGroups(content.Id, addedContentGroups);
                    }
                    updates[content] = await GetUsersAsync(content.Id);
                }
            }
            foreach (var it in updates)
            {
                await _eventsNotifier.ContentUpdate(it.Value, [it.Key]);
            }
            return list.ToArray();
        }


        /// <summary>
        /// Map content group to user groups.
        /// </summary>
        /// <param name="contentId">Content ID.</param>
        /// <param name="fields">Content fields.</param>
        public void AddContentFields(Guid contentId, IEnumerable<GXContentField>? fields)
        {
            if (fields != null)
            {
                DateTime now = DateTime.Now;
                List<GXContentGroupContent> list = new();
                foreach (GXContentField it in fields)
                {
                    it.Parent = new GXContent() { Id = contentId };
                    if (it.Type?.Id == null)
                    {
                        throw new ArgumentException("Content field type is invalid.");
                    }
                    else
                    {
                        //Check that content field type exists.
                        GXSelectArgs arg = GXSelectArgs.Select<GXContentTypeField>(s => s.Id, w => w.Id == it.Type.Id);
                        if (_host.Connection.SelectById<GXContentTypeField>(it.Type.Id) == null)
                        {
                            throw new ArgumentException("Content field type is invalid.");
                        }
                    }
                    it.CreationTime = now;
                    it.Type = new GXContentTypeField() { Id = it.Type.Id };
                }
                _host.Connection.Insert(GXInsertArgs.InsertRange(fields));
            }
        }

        /// <summary>
        /// Map content group to user groups.
        /// </summary>
        /// <param name="contentId">Content ID.</param>
        /// <param name="groups">Group IDs of the content groups where the content is added.</param>
        public void AddContentToContentGroups(Guid contentId, IEnumerable<GXContentGroup> groups)
        {
            DateTime now = DateTime.Now;
            List<GXContentGroupContent> list = new();
            foreach (GXContentGroup it in groups)
            {
                list.Add(new GXContentGroupContent()
                {
                    ContentId = contentId,
                    ContentGroupId = it.Id,
                    CreationTime = now
                });
            }
            _host.Connection.Insert(GXInsertArgs.InsertRange(list));
        }

        /// <summary>
        /// Remove map between content group and content.
        /// </summary>
        /// <param name="contentId">Content ID.</param>
        /// <param name="groups">Group IDs of the content groups where the content is removed.</param>
        public void RemoveContentsFromContentGroup(Guid contentId, IEnumerable<GXContentGroup> groups)
        {
            foreach (GXContentGroup it in groups)
            {
                _host.Connection.Delete(GXDeleteArgs.Delete<GXContentGroupContent>(w => w.ContentId == contentId && w.ContentGroupId == it.Id));
            }
        }

        /// <inheritdoc />
        public async Task CloseAsync(IEnumerable<Guid> contents)
        {
            string? userId = ServerHelpers.GetUserId(User);
            Dictionary<Guid, List<string>> updates = new();
            DateTime now = DateTime.Now;
            List<GXUserContentSettings> inserted = new();
            List<GXUserContentSettings> updated = new();
            foreach (Guid it in contents)
            {
                GXSelectArgs args = GXSelectArgs.SelectAll<GXUserContentSettings>(where => where.ContentId == it && where.UserId == userId);
                GXUserContentSettings s = await _host.Connection.SingleOrDefaultAsync<GXUserContentSettings>(args);
                if (s == null)
                {
                    s = new GXUserContentSettings() { UserId = userId, ContentId = it, Closed = now };
                    inserted.Add(s);
                }
                else
                {
                    await _host.Connection.DeleteAsync(GXDeleteArgs.Delete<GXUserContentSettings>(where => where.ContentId == it && where.UserId == userId));
                    s = new GXUserContentSettings() { UserId = userId, ContentId = it, Closed = now };
                    updated.Add(s);
                }
                List<string> users = await GetUsersAsync(it);
                updates[it] = users;
            }
            if (updated.Count != 0)
            {
                await _host.Connection.InsertAsync(GXInsertArgs.InsertRange(updated));
                foreach (var it in updates)
                {
                    await _eventsNotifier.ContentClose(it.Value, new GXContent[] { new GXContent() { Id = it.Key } });
                }
            }
            if (inserted.Count != 0)
            {
                await _host.Connection.InsertAsync(GXInsertArgs.InsertRange(inserted));
                foreach (var it in updates)
                {
                    await _eventsNotifier.ContentClose(it.Value, new GXContent[] { new GXContent() { Id = it.Key } });
                }
            }
        }
    }
}
