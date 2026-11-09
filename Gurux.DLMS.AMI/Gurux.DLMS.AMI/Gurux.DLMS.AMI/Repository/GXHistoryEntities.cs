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

using Gurux.DLMS.AMI.Shared.DTOs.History;
using Gurux.DLMS.AMI.Db;
using Gurux.DLMS.AMI.Shared.DIs;
using Gurux.DLMS.AMI.Shared.Rest;
using Gurux.Service.Orm;
using Gurux.Service.Orm.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using System.Globalization;
using System.Linq.Expressions;
using System.Security.Claims;
using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.DLMS.AMI.Shared.DTOs.Agent;
using Gurux.DLMS.AMI.Shared.DTOs.Authentication;
using Gurux.DLMS.AMI.Shared.DTOs.Block;
using Gurux.DLMS.AMI.Shared.DTOs.ComponentView;
using Gurux.DLMS.AMI.Shared.DTOs.Device;
using Gurux.DLMS.AMI.Shared.DTOs.Enums;
using Gurux.DLMS.AMI.Shared.DTOs.Gateway;
using Gurux.DLMS.AMI.Shared.DTOs.KeyManagement;
using Gurux.DLMS.AMI.Shared.DTOs.Log;
using Gurux.DLMS.AMI.Shared.DTOs.Manufacturer;
using Gurux.DLMS.AMI.Shared.DTOs.Menu;
using Gurux.DLMS.AMI.Shared.DTOs.Module;
using Gurux.DLMS.AMI.Shared.DTOs.Notification;
using Gurux.DLMS.AMI.Shared.DTOs.Schedule;
using Gurux.DLMS.AMI.Shared.DTOs.Script;
using Gurux.DLMS.AMI.Shared.DTOs.Trigger;
using Gurux.DLMS.AMI.Shared.DTOs.Update;
using Gurux.DLMS.AMI.Shared.DTOs.UrlAlias;
using Gurux.DLMS.AMI.Shared.DTOs.User;
using Gurux.DLMS.AMI.Shared.DTOs.View;
using Gurux.DLMS.AMI.Shared.DTOs.Workflow;
using Gurux.DLMS.AMI.Shared.Enums;

namespace Gurux.DLMS.AMI.Server.Repository;

/// <summary>Explicit domain operations available to history; arbitrary CLR types are never activated.</summary>
internal interface IGXHistoryEntity
{
    Guid Id { get; }
    GXChangeHistoryType HistoryType { get; }
    string Name { get; }
    Type EntityType { get; }
    object GetId(object entity);
    object ParseId(string id);
    Task<object?> ReadAsync(IServiceProvider services, object id, CancellationToken token);
    Task WriteAsync(IServiceProvider services, object entity, CancellationToken token);
    Task<object?> ReadStoredAsync(GXDbConnection db, object id, CancellationToken token);
    Task<IReadOnlyList<object>> ListIdsAsync(GXDbConnection db, CancellationToken token);
}

/// <summary>Authorized bridge to the owning module's user-settings API.</summary>
internal interface IGXHistoryModuleUserSettings
{
    Task<GXUserProperty?> ReadAsync(GXUserProperty row, CancellationToken token);
    Task WriteAsync(GXUserProperty row, CancellationToken token);
}

/// <summary>Compile-time registry of editable aggregates and their authorized domain read/write operations.</summary>
internal static class GXHistoryEntities
{
    private static readonly IGXHistoryEntity[] Entries = Create();
    internal static IReadOnlyCollection<IGXHistoryEntity> All => Entries;
    internal static IGXHistoryEntity Get(GXChangeHistoryType type) => Entries.SingleOrDefault(e => e.Id == type.Id)
        ?? throw new ArgumentException("Unsupported history entity type GUID.", nameof(type));
    internal static IGXHistoryEntity Get(string name) => Entries.SingleOrDefault(e => e.Name == name)
        ?? throw new ArgumentException($"Unsupported history entity type '{name}'.", nameof(name));
    internal static IGXHistoryEntity Get(Type type) => Entries.SingleOrDefault(e => e.EntityType == type)
        ?? throw new ArgumentException($"Unsupported history entity type '{type.Name}'.", nameof(type));

    // Persisted type identities: never regenerate these GUIDs when renaming a type or its CLR class.
    private static IGXHistoryEntity[] Create() =>
    [
        new Entity<GXAgentGroup, Guid, IAgentGroupRepository>(new Guid("5712d9aa-4ae3-4821-a195-0ac084e092ab"), "AgentGroup", async (r, id, ct, _) => await r.ReadAsync(id, null, ct), async (r, e, ct) => { await r.UpsertAsync([e], GXHistorySnapshot.Columns<GXAgentGroup>(), ct); }),
        new Entity<GXAgent, Guid, IAgentRepository>(new Guid("ecc28ac3-1a6a-44ee-a005-630b57625137"), "Agent", async (r, id, ct, _) => await r.ReadAsync(id, null, ct), async (r, e, ct) => { await r.UpsertAsync([e], GXHistorySnapshot.Columns<GXAgent>(), ct); }),
        new Entity<GXAttribute, Guid, IAttributeRepository>(new Guid("dd64d50f-0cc0-41ea-9dba-6198974c1719"), "Attribute", async (r, id, ct, _) => await r.ReadAsync(id, null, ct), async (r, e, ct) => { await r.UpsertAsync([e], GXHistorySnapshot.Columns<GXAttribute>(), ct); }),
        new Entity<GXAttributeTemplate, Guid, IAttributeTemplateRepository>(new Guid("ffc76d78-fd4e-428a-95ac-218a18218b5e"), "AttributeTemplate", async (r, id, ct, _) => await r.ReadAsync(id, null, ct), async (r, e, ct) => { await r.UpsertAsync([e], GXHistorySnapshot.Columns<GXAttributeTemplate>(), ct); }),
        new Entity<GXBlockGroup, Guid, IBlockGroupRepository>(new Guid("3f5b43cd-f5e4-48ac-ad91-edc2bb248dc4"), "BlockGroup", async (r, id, ct, _) => await r.ReadAsync(id, null, ct), async (r, e, ct) => { await r.UpsertAsync([e], GXHistorySnapshot.Columns<GXBlockGroup>(), ct); }),
        new Entity<GXBlock, Guid, IBlockRepository>(new Guid("b0ace824-a248-44c1-b0b9-f91d0b9503e5"), "Block", async (r, id, ct, _) => await r.ReadAsync(id, null, ct), async (r, e, ct) => { await r.UpsertAsync([e], GXHistorySnapshot.Columns<GXBlock>(), ct); }),
        new Entity<GXComponentViewGroup, Guid, IComponentViewGroupRepository>(new Guid("7bae25cd-b2a0-471e-ab43-1414fd9ed9a4"), "ComponentViewGroup", async (r, id, ct, _) => await r.ReadAsync(id, null, ct), async (r, e, ct) => { await r.UpsertAsync([e], GXHistorySnapshot.Columns<GXComponentViewGroup>(), ct); }),
        new Entity<GXComponentView, Guid, IComponentViewRepository>(new Guid("ca5eb96b-4662-4f25-8ddf-9001ef5ed722"), "ComponentView", async (r, id, ct, _) => await r.ReadAsync(id, null, ct), async (r, e, ct) => { await r.UpsertAsync([e], GXHistorySnapshot.Columns<GXComponentView>(), ct); }),
        new Entity<GXDeviceGroup, Guid, IDeviceGroupRepository>(new Guid("67f4b134-5c3e-45c4-ad35-42e4a7332d66"), "DeviceGroup", async (r, id, ct, _) => await r.ReadAsync(id, null, ct), async (r, e, ct) => { await r.UpsertAsync([e], GXHistorySnapshot.Columns<GXDeviceGroup>(), ct); }),
        new Entity<GXDevice, Guid, IDeviceRepository>(new Guid("e8b2d52d-b801-4428-baee-3bd7188603b4"), "Device", async (r, id, ct, _) => await r.ReadAsync(id, null, ct), async (r, e, ct) => { await r.UpsertAsync([e], ct, GXHistorySnapshot.Columns<GXDevice>()); }),
        new Entity<GXDeviceTemplateGroup, Guid, IDeviceTemplateGroupRepository>(new Guid("ffb1fc03-3476-4271-8bbb-9769310440b5"), "DeviceTemplateGroup", async (r, id, ct, _) => await r.ReadAsync(id, null, ct), async (r, e, ct) => { await r.UpsertAsync([e], GXHistorySnapshot.Columns<GXDeviceTemplateGroup>(), ct); }),
        new Entity<GXDeviceTemplate, Guid, IDeviceTemplateRepository>(new Guid("3df7b7df-a9d8-4d5b-aaa5-705b1724bb5a"), "DeviceTemplate", async (r, id, ct, _) => await r.ReadAsync(id, null, ct), async (r, e, ct) => { await r.UpsertAsync([e], GXHistorySnapshot.Columns<GXDeviceTemplate>(), ct); }),
        new Entity<GXFavorite, Guid, IFavoriteRepository>(new Guid("a2d75b4e-e6cc-4955-9eea-cfcbe3deae84"), "Favorite", async (r, id, ct, _) => await r.ReadAsync(id, null, ct), async (r, e, ct) => { await r.UpsertAsync([e], GXHistorySnapshot.Columns<GXFavorite>(), ct); }),
        new Entity<GXGatewayGroup, Guid, IGatewayGroupRepository>(new Guid("173f4c6d-10e5-4ff0-8ef8-2de4dcac427c"), "GatewayGroup", async (r, id, ct, _) => await r.ReadAsync(id, null, ct), async (r, e, ct) => { await r.UpsertAsync([e], GXHistorySnapshot.Columns<GXGatewayGroup>(), ct); }),
        new Entity<GXGateway, Guid, IGatewayRepository>(new Guid("10706ee9-2635-4a55-83f1-6cb13e200cac"), "Gateway", async (r, id, ct, _) => await r.ReadAsync(id, null, ct), async (r, e, ct) => { await r.UpsertAsync([e], GXHistorySnapshot.Columns<GXGateway>(), ct); }),
        new Entity<GXKeyManagementGroup, Guid, IKeyManagementGroupRepository>(new Guid("9a16b819-efb1-415d-b437-e6c128c6d4b9"), "KeyManagementGroup", async (r, id, ct, _) => await r.ReadAsync(id, null, ct), async (r, e, ct) => { await r.UpsertAsync([e], GXHistorySnapshot.Columns<GXKeyManagementGroup>(), ct); }),
        new Entity<GXKeyManagement, Guid, IKeyManagementRepository>(new Guid("a946522b-c1b3-4d8c-b938-44926504fa8e"), "KeyManagement", async (r, id, ct, _) => await r.ReadAsync(id, null, ct), async (r, e, ct) => { await r.UpsertAsync([e], GXHistorySnapshot.Columns<GXKeyManagement>(), ct); }),
        new Entity<GXLocalizedResource, Guid, ILocalizedResourceRepository>(new Guid("036d8030-3e1e-40cd-9c8b-1f8923698965"), "LocalizedResource", async (r, id, ct, _) => await r.ReadAsync(id, null, ct), async (r, e, ct) => { await r.UpsertAsync([e], ct); }),
        new Entity<GXLogType, Guid, ILogTypeRepository>(new Guid("af915b22-585e-471f-bed2-066bbe512fc3"), "LogType", async (r, id, ct, _) => await r.ReadAsync(id, null, ct), async (r, e, ct) => { await r.UpsertAsync([e], GXHistorySnapshot.Columns<GXLogType>(), ct); }, idMember: "PublicId"),
        new Entity<GXManufacturerGroup, Guid, IManufacturerGroupRepository>(new Guid("8ff46be8-893e-45a0-a63e-eb60c1cec79c"), "ManufacturerGroup", async (r, id, ct, _) => await r.ReadAsync(id, null, ct), async (r, e, ct) => { await r.UpsertAsync([e], GXHistorySnapshot.Columns<GXManufacturerGroup>(), ct); }),
        new Entity<GXManufacturer, Guid, IManufacturerRepository>(new Guid("7d0df823-387d-4ff3-9c60-f4c32a91658c"), "Manufacturer", async (r, id, ct, _) => await r.ReadAsync(id, null, ct), async (r, e, ct) => { await r.UpsertAsync([e], GXHistorySnapshot.Columns<GXManufacturer>(), ct); }),
        new Entity<GXMenuGroup, Guid, IMenuGroupRepository>(new Guid("1a9f52ff-0687-4252-8dca-47f44cd0084c"), "MenuGroup", async (r, id, ct, _) => await r.ReadAsync(id, null, ct), async (r, e, ct) => { await r.UpsertAsync([e], GXHistorySnapshot.Columns<GXMenuGroup>(), ct); }),
        new Entity<GXMenu, Guid, IMenuRepository>(new Guid("23efcd3d-58b9-40e7-b527-685ee9286233"), "Menu", async (r, id, ct, _) => await r.ReadAsync(id, null, ct), async (r, e, ct) => { await r.UpsertAsync([e], GXHistorySnapshot.Columns<GXMenu>(), ct); }),
        new Entity<GXModuleGroup, Guid, IModuleGroupRepository>(new Guid("be3e9103-1561-49b2-8911-74266c1969ea"), "ModuleGroup", async (r, id, ct, _) => await r.ReadAsync(id, null, ct), async (r, e, ct) => { await r.UpsertAsync([e], GXHistorySnapshot.Columns<GXModuleGroup>(), ct); }),
        new Entity<GXNotification, Guid, INotificationRepository>(new Guid("782a66c9-1644-4e6c-ba8e-e76781b2a520"), "Notification", async (r, id, ct, _) => await r.ReadAsync(id, null, ct), async (r, e, ct) => { await r.UpsertAsync([e], GXHistorySnapshot.Columns<GXNotification>(), ct); }),
        new Entity<GXNotificationType, Guid, INotificationTypeRepository>(new Guid("4233d92c-9e07-46e0-9bef-ec3b4a0dcd2b"), "NotificationType", async (r, id, ct, _) => await r.ReadAsync(id, null, ct), async (r, e, ct) => { await r.UpsertAsync([e], GXHistorySnapshot.Columns<GXNotificationType>(), ct); }),
        new Entity<GXObject, Guid, IObjectRepository>(new Guid("d05ca84f-9dd9-44d7-80e5-a8bfe3da2ee2"), "Object", async (r, id, ct, _) => await r.ReadAsync(id, null, ct), async (r, e, ct) => { await r.UpsertAsync([e], GXHistorySnapshot.Columns<GXObject>(), ct); }),
        new Entity<GXObjectTemplate, Guid, IObjectTemplateRepository>(new Guid("3dd34f01-9678-4f80-b73d-d5365fc10c4e"), "ObjectTemplate", async (r, id, ct, _) => await r.ReadAsync(id, null, ct), async (r, e, ct) => { await r.UpsertAsync([e], GXHistorySnapshot.Columns<GXObjectTemplate>(), ct); }),
        new Entity<GXPolicy, Guid, IPolicyRepository>(new Guid("ae4da6cb-a625-4768-9834-4b1c07ad1cb4"), "Policy", async (r, id, ct, _) => await r.ReadAsync(id, null, ct), async (r, e, ct) => { await r.UpsertAsync([e], ct); }),
        new Entity<GXRole, string, IRoleRepository>(new Guid("0d21df7e-339f-47b2-9b1c-b8a9c7b1753d"), "Role", async (r, id, ct, _) => await r.ReadAsync(id, null, ct), async (r, e, ct) => { await r.UpsertAsync([e], ct); }),
        new Entity<GXScheduleGroup, Guid, IScheduleGroupRepository>(new Guid("2d821349-f6ab-4f60-b106-b54e091cf504"), "ScheduleGroup", async (r, id, ct, _) => await r.ReadAsync(id, null, ct), async (r, e, ct) => { await r.UpsertAsync([e], GXHistorySnapshot.Columns<GXScheduleGroup>(), ct); }),
        new Entity<GXSchedule, Guid, IScheduleRepository>(new Guid("33978429-12bf-4ae2-a5e0-043f7c9e27c7"), "Schedule", async (r, id, ct, _) => await r.ReadAsync(id, null, ct), async (r, e, ct) => { await r.UpsertAsync([e], GXHistorySnapshot.Columns<GXSchedule>(), ct); }),
        new Entity<GXScriptGroup, Guid, IScriptGroupRepository>(new Guid("77a811fe-cc67-4032-b262-486bf5bd1c3c"), "ScriptGroup", async (r, id, ct, _) => await r.ReadAsync(id, null, ct), async (r, e, ct) => { await r.UpsertAsync([e], GXHistorySnapshot.Columns<GXScriptGroup>(), ct); }),
        new Entity<GXScript, Guid, IScriptRepository>(new Guid("4a736fdf-6ec5-4dd5-81d2-e2fd04adb960"), "Script", async (r, id, ct, _) => await r.ReadAsync(id, null, ct), async (r, e, ct) => { await r.UpsertAsync([e], GXHistorySnapshot.Columns<GXScript>(), ct); }),
        new Entity<GXTriggerGroup, Guid, ITriggerGroupRepository>(new Guid("01b63b87-c790-4658-be16-c677290e5c1d"), "TriggerGroup", async (r, id, ct, _) => await r.ReadAsync(id, null, ct), async (r, e, ct) => { await r.UpsertAsync([e], GXHistorySnapshot.Columns<GXTriggerGroup>(), ct); }),
        new Entity<GXTrigger, Guid, ITriggerRepository>(new Guid("fa618ff9-8cf8-4208-8756-a98d919915da"), "Trigger", async (r, id, ct, _) => await r.ReadAsync(id, null, ct), async (r, e, ct) => { await r.UpsertAsync([e], GXHistorySnapshot.Columns<GXTrigger>(), ct); }),
        new Entity<GXUserGroup, Guid, IUserGroupRepository>(new Guid("9a1af730-6b19-4dfe-a405-234e6c534b84"), "UserGroup", async (r, id, ct, _) => await r.ReadAsync(id, null, ct), async (r, e, ct) => { await r.UpsertAsync([e], GXHistorySnapshot.Columns<GXUserGroup>(), ct); }),
        new Entity<GXUser, string, IUserRepository>(new Guid("2e0a4c1e-0bb1-4bf6-8512-0256789241bc"), "User", async (r, id, ct, _) => await r.ReadAsync(id, null, ct), async (r, e, ct) => { await r.UpsertAsync([e], GXHistorySnapshot.Columns<GXUser>(), ct); }),
        new Entity<GXViewGroup, Guid, IViewGroupRepository>(new Guid("d3e94146-3ace-491a-ac42-3a7fa3a96855"), "ViewGroup", async (r, id, ct, _) => await r.ReadAsync(id, null, ct), async (r, e, ct) => { await r.UpsertAsync([e], GXHistorySnapshot.Columns<GXViewGroup>(), ct); }),
        new Entity<GXView, Guid, IViewRepository>(new Guid("453130ea-2085-45ce-b0f8-2da66aaba7ad"), "View", async (r, id, ct, _) => await r.ReadAsync(id, null, ct), async (r, e, ct) => { await r.UpsertAsync([e], GXHistorySnapshot.Columns<GXView>(), ct); }),
        new Entity<GXWorkflowGroup, Guid, IWorkflowGroupRepository>(new Guid("ae6945a2-3ab4-46d9-9804-4e5e964df7ee"), "WorkflowGroup", async (r, id, ct, _) => await r.ReadAsync(id, null, ct), async (r, e, ct) => { await r.UpsertAsync([e], GXHistorySnapshot.Columns<GXWorkflowGroup>(), ct); }),
        new Entity<GXWorkflow, Guid, IWorkflowRepository>(new Guid("9df68274-e67e-43fb-b3b7-d8b8817abf43"), "Workflow", async (r, id, ct, _) => await r.ReadAsync(id, true, null, ct),
            async (r, e, ct) => { await r.UpsertAsync([e], GXHistorySnapshot.Columns<GXWorkflow>(), ct); }),
        new Entity<GXModule, string, IModuleRepository>(new Guid("64eeec19-7b3f-4ad1-8c53-590e61381a17"), "Module", async (r, id, ct, _) => await r.ReadAsync(id, null, ct),
            (r, e, ct) => r.UpsertAsync(e, GXHistorySnapshot.Columns<GXModule>(), ct)),
        new Entity<GXLanguage, string, ILocalizationRepository>(new Guid("965d658c-7d77-49ec-a5ad-9e74ef4e7cfd"), "Language", async (r, id, ct, _) => (await r.ListAsync(new ListLanguages { Filter = new GXLanguage { Id = id } }, null, ct)).SingleOrDefault(),
            (r, e, ct) => r.UpdateCulturesAsync([e], ct)),
        new Entity<GXConfiguration, Guid, IConfigurationRepository>(new Guid("06b255f3-c393-4dad-9405-4fbe15cc00fd"), "Configuration",
            async (r, id, ct, sp) => await r.ReadAsync(Actor(sp), id, null, ct),
            null, async (sp, e, ct) => await sp.GetRequiredService<IConfigurationRepository>().UpsertAsync(Actor(sp), [e], true, ct)),
        new Entity<GXAppearance, Guid, IAppearanceRepository>(new Guid("35b8ab0f-fa86-4d87-a4de-5912b92c2f2f"), "Appearance", async (r, id, ct, sp) =>
            {
                var row = await Stored<GXAppearance, Guid>(sp, id, ct);
                return row == null ? null : await r.ReadAsync(row.ResourceType ?? 0, id, null, ct);
            }, async (r, e, ct) => { await r.UpsertAsync([e], null, ct); }),
        new Entity<GXAgentProperty, Guid, IAgentPropertiesRepository>(new Guid("701aa916-9506-4dc8-8e2c-3b040bd5e113"), "AgentProperty", async (r, id, ct, sp) =>
            {
                var row = await Stored<GXAgentProperty, Guid>(sp, id, ct);
                return row?.Agent == null || row.Name == null ? null : await r.ReadAsync(row.Agent.Id.ToString(), row.Name, row.Module?.Id, null, ct);
            }, (r, e, ct) => r.UpsertAsync([e], GXHistorySnapshot.Columns<GXAgentProperty>(), ct)),
        new Entity<GXUserProperty, Guid, IUserPropertiesRepository>(new Guid("15ca85c2-1566-433a-b2d8-e5504091a5d0"), "UserProperty", async (r, id, ct, sp) =>
            {
                var row = await Stored<GXUserProperty, Guid>(sp, id, ct);
                if (row?.Module != null) { return await sp.GetRequiredService<IGXHistoryModuleUserSettings>().ReadAsync(row, ct); } return row?.User == null || row.Name == null ? null : await r.ReadAsync(row.Name, null, ct, row.User.Id);
            }, null, async (sp, e, ct) =>
            {
                if (e.Module != null) { await sp.GetRequiredService<IGXHistoryModuleUserSettings>().WriteAsync(e, ct); } else { await sp.GetRequiredService<IUserPropertiesRepository>().UpsertAsync([e], GXHistorySnapshot.Columns<GXUserProperty>(), ct); } }),
        new Entity<GXNotificationSource, int, INotificationSourceRepository>(new Guid("0bbd25e5-defe-4090-9e0f-5dc3e95f6a02"), "NotificationSource", async (r, id, ct, _) =>
            (await r.ListAsync(new ListNotificationSources { Included = [id] }, null, ct)).SingleOrDefault(),
            (r, e, ct) => r.SetEnabledAsync([e.Id], e.Enabled ?? throw new ArgumentException("A saved enabled value is required."), ct)),
        new Entity<GXEnumType, int, IEnumTypeRepository>(new Guid("cda167c3-ac95-4584-a097-71e3ff0b7bdd"), "EnumType", async (r, id, ct, _) =>
            (await r.ListAsync(new ListEnumTypes { Included = [id] }, null, ct)).SingleOrDefault(),
            (_, _, _) => throw new InvalidOperationException("Enumeration definitions have no general domain update operation; restore their producer's configuration instead."))
    ];

    private static ClaimsPrincipal Actor(IServiceProvider services) => services.GetRequiredService<IHttpContextAccessor>().HttpContext?.User
        ?? throw new UnauthorizedAccessException("An authenticated history actor is required.");

    private static async Task<T?> Stored<T, TKey>(IServiceProvider services, TKey id, CancellationToken token, string idMember = "Id")
        where T : class, new()
    {
        await using var db = await services.GetRequiredService<IDbConnectionFactory>().GetHistoryConnectionAsync(token);
        return await db.SingleOrDefaultAsync<T>(Query<T, TKey>(id, idMember), token);
    }

    private static GXSelectArgs Query<T, TKey>(TKey id, string idMember = "Id") where T : class, new()
    {
        var p = Expression.Parameter(typeof(T), "entity");
        var predicate = Expression.Lambda<Func<T, bool>>(Expression.Equal(Expression.Property(p, idMember), Expression.Constant(id, typeof(TKey))), p);
        var query = GXSelectArgs.SelectAll<T>(predicate);
        // The ORM does not materialize foreign reference identities without an explicit join.
        // Natural-key repositories require these owner/module identities to authorize their read.
        if (typeof(T) == typeof(GXUserProperty))
        {
            query.Joins.AddLeftJoin<GXUserProperty, GXUser>(p => p.User!, u => u.Id!);
            query.Columns.Add<GXUser>(u => u.Id!);
            query.Joins.AddLeftJoin<GXUserProperty, GXModule>(p => p.Module!, m => m.Id);
            query.Columns.Add<GXModule>(m => m.Id);
        }
        else if (typeof(T) == typeof(GXAgentProperty))
        {
            query.Joins.AddLeftJoin<GXAgentProperty, GXAgent>(p => p.Agent!, a => a.Id);
            query.Columns.Add<GXAgent>(a => a.Id);
            query.Joins.AddLeftJoin<GXAgentProperty, GXModule>(p => p.Module!, m => m.Id);
            query.Columns.Add<GXModule>(m => m.Id);
        }
        return query;
    }

    private sealed class Entity<T, TKey, TRepository>(Guid typeId, string name,
        Func<TRepository, TKey, CancellationToken, IServiceProvider, Task<T?>> read,
        Func<TRepository, T, CancellationToken, Task>? write,
        Func<IServiceProvider, T, CancellationToken, Task>? scopedWrite = null, string idMember = "Id") : IGXHistoryEntity
        where T : class, new() where TRepository : notnull
    {
        public Guid Id => typeId;
        public GXChangeHistoryType HistoryType => new() { Id = typeId, Name = name };
        public string Name => name;
        public Type EntityType => typeof(T);
        public object GetId(object entity) => typeof(T).GetProperty(idMember)!.GetValue((T)entity) ?? string.Empty;
        public object ParseId(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                throw new ArgumentException("An entity identifier is required.", nameof(id));
            }

            if (typeof(TKey) == typeof(Guid))
            {
                return Guid.TryParse(id, out var guid) && guid != Guid.Empty ? guid : throw new ArgumentException("A nonempty GUID identifier is required.");
            }

            if (typeof(TKey) == typeof(int))
            {
                return int.TryParse(id, NumberStyles.None, CultureInfo.InvariantCulture, out var value) && value > 0 ? value : throw new ArgumentException("A positive integer identifier is required.");
            }

            if (typeof(TKey) == typeof(string))
            {
                return id;
            }

            throw new NotSupportedException("Unsupported entity identifier type.");
        }
        public async Task<object?> ReadAsync(IServiceProvider services, object id, CancellationToken token)
        {
            var entity = await read(services.GetRequiredService<TRepository>(), (TKey)id, token, services);
            if (entity != null)
            {
                await using var db = await services.GetRequiredService<IDbConnectionFactory>().GetHistoryConnectionAsync(token);
                await GXHistoryEntityRelations.HydrateAsync(db, entity, token);
                if (entity is GXSchedule schedule)
                {
                    GXHistorySnapshot.SetModuleSettings(schedule, await db.SelectAsync<GXScheduleModule>(GXSelectArgs.SelectAll<GXScheduleModule>(s => s.ScheduleId == schedule.Id && s.Removed == null), token));
                }
            }
            return entity;
        }
        public async Task WriteAsync(IServiceProvider services, object entity, CancellationToken token)
        {
            var current = await ReadAsync(services, GetId(entity), token)
                ?? throw new InvalidOperationException("The restoration target no longer exists.");
            // Some authorized read APIs intentionally redact credential values inside serialized settings.
            // Rehydrate only the current setting containers before merging; old secrets are never in the snapshot.
            var stored = await Stored<T, TKey>(services, (TKey)GetId(entity), token, idMember);
            if (stored != null)
            {
                foreach (var property in typeof(T).GetProperties().Where(p => p.CanRead && p.CanWrite && p.PropertyType == typeof(string) && p.Name.EndsWith("Settings", StringComparison.Ordinal)))
                {
                    property.SetValue(current, property.GetValue(stored));
                }
            }
            var restored = (T)GXHistorySnapshot.MergeForRestore(current, entity);
            if (scopedWrite != null)
            {
                await scopedWrite(services, restored, token);
            }
            else
            {
                await write!(services.GetRequiredService<TRepository>(), restored, token);
            }

            if (restored is GXSchedule schedule && GXHistorySnapshot.GetModuleSettings(restored) is { } settings)
            {
                var repository = services.GetRequiredService<IScheduleRepository>();
                foreach (var saved in settings)
                {
                    var existing = await repository.GetModuleSettingsAsync(new GXScheduleModule { ScheduleId = schedule.Id, ModuleId = saved.ModuleId }, token)
                        ?? throw new InvalidOperationException("A saved schedule module mapping no longer exists.");
                    existing.Settings = GXHistorySnapshot.RestoreSettings(existing.Settings, saved.Settings);
                    await repository.UpdateModuleSettingsAsync(existing, token);
                }
            }
        }
        public async Task<object?> ReadStoredAsync(GXDbConnection db, object id, CancellationToken token)
            => await db.SingleOrDefaultAsync<T>(Query<T, TKey>((TKey)id, idMember), token);
        public async Task<IReadOnlyList<object>> ListIdsAsync(GXDbConnection db, CancellationToken token)
        {
            var p = Expression.Parameter(typeof(T), "entity");
            var columns = Expression.Lambda<Func<T, object>>(Expression.Convert(Expression.Property(p, idMember), typeof(object)), p);
            return (await db.SelectAsync<T>(GXSelectArgs.Select(columns), token)).Select(row => GetId(row)).ToArray();
        }
    }
}
