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

using System.Data;
using System.Linq.Expressions;
using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.DLMS.AMI.Shared.DTOs.Agent;
using Gurux.DLMS.AMI.Shared.DTOs.Authentication;
using Gurux.DLMS.AMI.Shared.DTOs.Block;
using Gurux.DLMS.AMI.Shared.DTOs.ComponentView;
using Gurux.DLMS.AMI.Shared.DTOs.Device;
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
using Gurux.DLMS.AMI.Shared.DTOs.User;
using Gurux.DLMS.AMI.Shared.DTOs.View;
using Gurux.DLMS.AMI.Shared.DTOs.Workflow;
using Gurux.DLMS.AMI.Server.Internal;
using Gurux.Service.Orm;
using Gurux.Service.Orm.Common.Enums;

namespace Gurux.DLMS.AMI.Server.Repository;

public partial class UserRepository
{
    private static async Task LockUsersForDeletionAsync(GXDbConnection connection,
        IDbTransaction transaction, string[] userIds, CancellationToken cancellationToken)
    {
        if (connection.DatabaseType is DatabaseType.MSSQL or DatabaseType.PostgreSQL or DatabaseType.Oracle)
        {
            // SQL Server permits FK checks through an unchanged referenced key
            // even while other columns are being updated. Lock that key itself
            // before deleting children so log writers cannot recreate a reference.
            // GXUser.Id's unique Index attribute creates gxuser_id; SQL Server
            // binds the user foreign keys to it rather than the clustered PK.
            using var command = connection.Connection.CreateCommand();
            command.Transaction = (System.Data.Common.DbTransaction)transaction;
            bool oracle = connection.DatabaseType == DatabaseType.Oracle;
            if (GXHistoryScope.UnwrapCommand(command) is Oracle.ManagedDataAccess.Client.OracleCommand oracleCommand) {
                oracleCommand.BindByName = true;
            }
            var names = new List<string>();
            for (int index = 0; index != userIds.Length; ++index)
            {
                var parameter = command.CreateParameter();
                parameter.ParameterName = (oracle ? "user" : "@user") + index;
                parameter.Value = userIds[index];
                command.Parameters.Add(parameter);
                names.Add((oracle ? ":" : "") + parameter.ParameterName);
            }
            command.CommandText = oracle
                // Oracle FK checks can bypass a parent FOR UPDATE lock. Log
                // writers take this same row lock explicitly before inserting.
                ? $"SELECT ID FROM GXUSER WHERE ID IN ({string.Join(",", names)}) ORDER BY ID FOR UPDATE"
                : connection.DatabaseType == DatabaseType.PostgreSQL
                // FOR UPDATE conflicts with the KEY SHARE lock used by FK inserts.
                ? $"SELECT id FROM gxuser WHERE id IN ({string.Join(",", names)}) FOR UPDATE"
                : $"SELECT ID FROM GXUser WITH (XLOCK, HOLDLOCK, ROWLOCK, INDEX(gxuser_id)) WHERE ID IN ({string.Join(",", names)}) ORDER BY ID";
            using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken)) { }
        }
    }

    // The database removes Cascade relationships. These queries handle the
    // remaining foreign keys and source identifiers on the same transaction.
    private static async Task DeleteNonCascadingUserDataAsync(GXDbConnection connection,
        IDbTransaction transaction, string[] userIds, CancellationToken cancellationToken)
    {
        var userRows = GXSelectArgs.Select<GXUser>(r => r.Id!, r => userIds.Contains(r.Id!));
        var agentRows = GXSelectArgs.Select<GXAgent>(r => r.Id, r => GXSql.Exists<GXAgent, GXUser>(r => r.Creator!, p => p.Id!, userRows));
        var agentGroupRows = GXSelectArgs.Select<GXAgentGroup>(r => r.Id, r => GXSql.Exists<GXAgentGroup, GXUser>(r => r.Creator!, p => p.Id!, userRows));
        var appearanceRows = GXSelectArgs.Select<GXAppearance>(r => r.Id, r => GXSql.Exists<GXAppearance, GXUser>(r => r.Creator!, p => p.Id!, userRows));
        var blockGroupRows = GXSelectArgs.Select<GXBlockGroup>(r => r.Id, r => GXSql.Exists<GXBlockGroup, GXUser>(r => r.Creator!, p => p.Id!, userRows));
        var componentViewRows = GXSelectArgs.Select<GXComponentView>(r => r.Id, r => GXSql.Exists<GXComponentView, GXUser>(r => r.Creator!, p => p.Id!, userRows));
        var componentViewGroupRows = GXSelectArgs.Select<GXComponentViewGroup>(r => r.Id, r => GXSql.Exists<GXComponentViewGroup, GXUser>(r => r.Creator!, p => p.Id!, userRows));
        var deviceGroupRows = GXSelectArgs.Select<GXDeviceGroup>(r => r.Id, r => GXSql.Exists<GXDeviceGroup, GXUser>(r => r.Creator!, p => p.Id!, userRows));
        var deviceTemplateRows = GXSelectArgs.Select<GXDeviceTemplate>(r => r.Id, r => GXSql.Exists<GXDeviceTemplate, GXUser>(r => r.Creator!, p => p.Id!, userRows));
        var deviceTemplateGroupRows = GXSelectArgs.Select<GXDeviceTemplateGroup>(r => r.Id, r => GXSql.Exists<GXDeviceTemplateGroup, GXUser>(r => r.Creator!, p => p.Id!, userRows));
        var gatewayRows = GXSelectArgs.Select<GXGateway>(r => r.Id, r => GXSql.Exists<GXGateway, GXUser>(r => r.Creator!, p => p.Id!, userRows)
            || GXSql.Exists<GXGateway, GXAgent>(r => r.Agent!, p => p.Id, agentRows));
        var gatewayGroupRows = GXSelectArgs.Select<GXGatewayGroup>(r => r.Id, r => GXSql.Exists<GXGatewayGroup, GXUser>(r => r.Creator!, p => p.Id!, userRows));
        var keyManagementRows = GXSelectArgs.Select<GXKeyManagement>(r => r.Id, r => GXSql.Exists<GXKeyManagement, GXUser>(r => r.Creator!, p => p.Id!, userRows));
        var keyManagementGroupRows = GXSelectArgs.Select<GXKeyManagementGroup>(r => r.Id, r => GXSql.Exists<GXKeyManagementGroup, GXUser>(r => r.Creator!, p => p.Id!, userRows));
        var logTypeRows = GXSelectArgs.Select<GXLogType>(r => r.Id, r => GXSql.Exists<GXLogType, GXUser>(r => r.Creator!, p => p.Id!, userRows));
        var manufacturerGroupRows = GXSelectArgs.Select<GXManufacturerGroup>(r => r.Id, r => GXSql.Exists<GXManufacturerGroup, GXUser>(r => r.Creator!, p => p.Id!, userRows));
        var menuRows = GXSelectArgs.Select<GXMenu>(r => r.Id, r => GXSql.Exists<GXMenu, GXUser>(r => r.Creator!, p => p.Id!, userRows));
        var menuGroupRows = GXSelectArgs.Select<GXMenuGroup>(r => r.Id, r => GXSql.Exists<GXMenuGroup, GXUser>(r => r.Creator!, p => p.Id!, userRows));
        var menuLinkRows = GXSelectArgs.Select<GXMenuLink>(r => r.Id, r => GXSql.Exists<GXMenuLink, GXMenu>(r => r.Menu!, p => p.Id, menuRows));
        var moduleRows = GXSelectArgs.Select<GXModule>(r => r.Id, r => GXSql.Exists<GXModule, GXUser>(r => r.Creator!, p => p.Id!, userRows));
        var moduleGroupRows = GXSelectArgs.Select<GXModuleGroup>(r => r.Id, r => GXSql.Exists<GXModuleGroup, GXUser>(r => r.Creator!, p => p.Id!, userRows));
        var objectTemplateRows = GXSelectArgs.Select<GXObjectTemplate>(r => r.Id, r => GXSql.Exists<GXObjectTemplate, GXDeviceTemplate>(r => r.DeviceTemplate!, p => p.Id, deviceTemplateRows));
        var scheduleRows = GXSelectArgs.Select<GXSchedule>(r => r.Id, r => GXSql.Exists<GXSchedule, GXUser>(r => r.Creator!, p => p.Id!, userRows));
        var scheduleGroupRows = GXSelectArgs.Select<GXScheduleGroup>(r => r.Id, r => GXSql.Exists<GXScheduleGroup, GXUser>(r => r.Creator!, p => p.Id!, userRows));
        var scriptRows = GXSelectArgs.Select<GXScript>(r => r.Id, r => GXSql.Exists<GXScript, GXUser>(r => r.Creator!, p => p.Id!, userRows));
        var scriptGroupRows = GXSelectArgs.Select<GXScriptGroup>(r => r.Id, r => GXSql.Exists<GXScriptGroup, GXUser>(r => r.Creator!, p => p.Id!, userRows));
        var scriptMethodRows = GXSelectArgs.Select<GXScriptMethod>(r => r.Id, r => GXSql.Exists<GXScriptMethod, GXScript>(r => r.Script!, p => p.Id, scriptRows));
        var triggerRows = GXSelectArgs.Select<GXTrigger>(r => r.Id, r => GXSql.Exists<GXTrigger, GXUser>(r => r.Creator!, p => p.Id!, userRows));
        var triggerGroupRows = GXSelectArgs.Select<GXTriggerGroup>(r => r.Id, r => GXSql.Exists<GXTriggerGroup, GXUser>(r => r.Creator!, p => p.Id!, userRows));
        var userGroupRows = GXSelectArgs.Select<GXUserGroup>(r => r.Id, r => GXSql.Exists<GXUserGroup, GXUser>(r => r.Creator!, p => p.Id!, userRows));
        var viewGroupRows = GXSelectArgs.Select<GXViewGroup>(r => r.Id, r => GXSql.Exists<GXViewGroup, GXUser>(r => r.Creator!, p => p.Id!, userRows));
        var workflowRows = GXSelectArgs.Select<GXWorkflow>(r => r.Id, r => GXSql.Exists<GXWorkflow, GXUser>(r => r.Creator!, p => p.Id!, userRows));
        var workflowGroupRows = GXSelectArgs.Select<GXWorkflowGroup>(r => r.Id, r => GXSql.Exists<GXWorkflowGroup, GXUser>(r => r.Creator!, p => p.Id!, userRows));
        var attributeTemplateRows = GXSelectArgs.Select<GXAttributeTemplate>(r => r.Id, r => GXSql.Exists<GXAttributeTemplate, GXObjectTemplate>(r => r.ObjectTemplate!, p => p.Id, objectTemplateRows));
        var blockRows = GXSelectArgs.Select<GXBlock>(r => r.Id, r => GXSql.Exists<GXBlock, GXUser>(r => r.Creator!, p => p.Id!, userRows)
            || GXSql.Exists<GXBlock, GXComponentView>(r => r.ComponentView!, p => p.Id, componentViewRows));
        var deviceRows = GXSelectArgs.Select<GXDevice>(r => r.Id, r => GXSql.Exists<GXDevice, GXUser>(r => r.Creator!, p => p.Id!, userRows)
            || GXSql.Exists<GXDevice, GXDeviceTemplate>(r => r.Template!, p => p.Id, deviceTemplateRows));
        var objectRows = GXSelectArgs.Select<GXObject>(r => r.Id, r => GXSql.Exists<GXObject, GXDevice>(r => r.Device!, p => p.Id, deviceRows));

        // Self-referencing EXISTS predicates cannot correlate two occurrences
        // of the same DTO table. Resolve hierarchy owners before clearing links.
        var blocks = await connection.SelectAsync<GXBlock>(transaction, blockRows, cancellationToken);
        var menus = await connection.SelectAsync<GXMenu>(transaction, menuRows, cancellationToken);
        var menuLinks = await connection.SelectAsync<GXMenuLink>(transaction, menuLinkRows, cancellationToken);

        // Logs carry source IDs rather than foreign keys.
        await GXLogStore.DeleteOwnedAsync(connection, transaction, "agent",
            (await connection.SelectAsync<GXAgent>(transaction, agentRows, cancellationToken)).Select(r => r.Id), cancellationToken);
        await GXLogStore.DeleteOwnedAsync(connection, transaction, "device",
            (await connection.SelectAsync<GXDevice>(transaction, deviceRows, cancellationToken)).Select(r => r.Id), cancellationToken);
        await GXLogStore.DeleteOwnedAsync(connection, transaction, "gateway",
            (await connection.SelectAsync<GXGateway>(transaction, gatewayRows, cancellationToken)).Select(r => r.Id), cancellationToken);
        await GXLogStore.DeleteOwnedAsync(connection, transaction, "key-management",
            (await connection.SelectAsync<GXKeyManagement>(transaction, keyManagementRows, cancellationToken)).Select(r => r.Id), cancellationToken);
        await GXLogStore.DeleteOwnedAsync(connection, transaction, "module",
            (await connection.SelectAsync<GXModule>(transaction, moduleRows, cancellationToken)).Select(r => r.Id), cancellationToken);
        foreach (var module in await connection.SelectAsync<GXModule>(transaction, moduleRows, cancellationToken))
            {
            await GXNotificationSourceStorage.DeleteModuleAsync(connection, transaction, module.Id, cancellationToken);
        }
        await GXLogStore.DeleteOwnedAsync(connection, transaction, "schedule",
            (await connection.SelectAsync<GXSchedule>(transaction, scheduleRows, cancellationToken)).Select(r => r.Id), cancellationToken);
        await GXLogStore.DeleteOwnedAsync(connection, transaction, "script",
            (await connection.SelectAsync<GXScript>(transaction, scriptRows, cancellationToken)).Select(r => r.Id), cancellationToken);
        await GXLogStore.DeleteOwnedAsync(connection, transaction, "workflow",
            (await connection.SelectAsync<GXWorkflow>(transaction, workflowRows, cancellationToken)).Select(r => r.Id), cancellationToken);

        await connection.DeleteAsync(transaction, GXDeleteArgs.Delete<GXPersistedGrants>(r => userIds.Contains(r.SubjectId!)), cancellationToken);

        await ClearUserDataReferenceAsync(connection, transaction, new GXAgentProperty(), r => r.Module!,
            r => GXSql.Exists<GXAgentProperty, GXModule>(r => r.Module!, p => p.Id, moduleRows), cancellationToken);
        await ClearUserDataReferenceAsync(connection, transaction, new GXAttributeProperty(), r => r.Module!,
            r => GXSql.Exists<GXAttributeProperty, GXModule>(r => r.Module!, p => p.Id, moduleRows), cancellationToken);
        await ClearUserDataReferenceAsync(connection, transaction, new GXBlock(), r => r.Parent!,
            r => blocks.Contains(r.Parent!), cancellationToken);
        await ClearUserDataReferenceAsync(connection, transaction, new GXBlock(), r => r.ScriptMethod!,
            r => GXSql.Exists<GXBlock, GXScriptMethod>(r => r.ScriptMethod!, p => p.Id, scriptMethodRows), cancellationToken);
        await ClearUserDataReferenceAsync(connection, transaction, new GXManufacturer(), r => r.ProfilePicture!,
            r => GXSql.Exists<GXManufacturer, GXAppearance>(r => r.ProfilePicture!, p => p.Id, appearanceRows), cancellationToken);
        await ClearUserDataReferenceAsync(connection, transaction, new GXDeviceModel(), r => r.ProfilePicture!,
            r => GXSql.Exists<GXDeviceModel, GXAppearance>(r => r.ProfilePicture!, p => p.Id, appearanceRows), cancellationToken);
        await ClearUserDataReferenceAsync(connection, transaction, new GXDeviceTemplate(), r => r.ProfilePicture!,
            r => GXSql.Exists<GXDeviceTemplate, GXAppearance>(r => r.ProfilePicture!, p => p.Id, appearanceRows), cancellationToken);
        await ClearUserDataReferenceAsync(connection, transaction, new GXDevice(), r => r.ProfilePicture!,
            r => GXSql.Exists<GXDevice, GXAppearance>(r => r.ProfilePicture!, p => p.Id, appearanceRows), cancellationToken);
        await ClearUserDataReferenceAsync(connection, transaction, new GXDeviceProperty(), r => r.Module!,
            r => GXSql.Exists<GXDeviceProperty, GXModule>(r => r.Module!, p => p.Id, moduleRows), cancellationToken);
        await ClearUserDataReferenceAsync(connection, transaction, new GXGateway(), r => r.ScriptMethod!,
            r => GXSql.Exists<GXGateway, GXScriptMethod>(r => r.ScriptMethod!, p => p.Id, scriptMethodRows), cancellationToken);
        await ClearUserDataReferenceAsync(connection, transaction, new GXKeyManagement(), r => r.Device!,
            r => GXSql.Exists<GXKeyManagement, GXDevice>(r => r.Device!, p => p.Id, deviceRows), cancellationToken);
        await ClearUserDataReferenceAsync(connection, transaction, new GXKeyManagement(), r => r.DeviceGroup!,
            r => GXSql.Exists<GXKeyManagement, GXDeviceGroup>(r => r.DeviceGroup!, p => p.Id, deviceGroupRows), cancellationToken);
        await ClearUserDataReferenceAsync(connection, transaction, new GXKeyManagementProperty(), r => r.Module!,
            r => GXSql.Exists<GXKeyManagementProperty, GXModule>(r => r.Module!, p => p.Id, moduleRows), cancellationToken);
        await ClearUserDataReferenceAsync(connection, transaction, new GXKeyManagement(), r => r.Template!,
            r => GXSql.Exists<GXKeyManagement, GXDeviceTemplate>(r => r.Template!, p => p.Id, deviceTemplateRows), cancellationToken);
        await ClearUserDataReferenceAsync(connection, transaction, new GXMenuLink(), r => r.Parent!,
            r => menuLinks.Contains(r.Parent!), cancellationToken);
        await ClearUserDataReferenceAsync(connection, transaction, new GXMenu(), r => r.Parent!,
            r => menus.Contains(r.Parent!), cancellationToken);
        await ClearUserDataReferenceAsync(connection, transaction, new GXNotificationProperty(), r => r.Module!,
            r => GXSql.Exists<GXNotificationProperty, GXModule>(r => r.Module!, p => p.Id, moduleRows), cancellationToken);
        await ClearUserDataReferenceAsync(connection, transaction, new GXNotification(), r => r.ScriptMethod!,
            r => GXSql.Exists<GXNotification, GXScriptMethod>(r => r.ScriptMethod!, p => p.Id, scriptMethodRows), cancellationToken);
        await ClearUserDataReferenceAsync(connection, transaction, new GXObjectProperty(), r => r.Module!,
            r => GXSql.Exists<GXObjectProperty, GXModule>(r => r.Module!, p => p.Id, moduleRows), cancellationToken);
        await ClearUserDataReferenceAsync(connection, transaction, new GXObject(), r => r.Template!,
            r => GXSql.Exists<GXObject, GXObjectTemplate>(r => r.Template!, p => p.Id, objectTemplateRows), cancellationToken);
        await ClearUserDataReferenceAsync(connection, transaction, new GXRawNotification(), r => r.Processor!,
            r => GXSql.Exists<GXRawNotification, GXAgent>(r => r.Processor!, p => p.Id, agentRows), cancellationToken);
        await ClearUserDataReferenceAsync(connection, transaction, new GXScript(), r => r.Module!,
            r => GXSql.Exists<GXScript, GXModule>(r => r.Module!, p => p.Id, moduleRows), cancellationToken);
        await ClearUserDataReferenceAsync(connection, transaction, new GXScriptProperty(), r => r.Module!,
            r => GXSql.Exists<GXScriptProperty, GXModule>(r => r.Module!, p => p.Id, moduleRows), cancellationToken);
        await ClearUserDataReferenceAsync(connection, transaction, new GXTriggerActivity(), r => r.Trigger!,
            r => GXSql.Exists<GXTriggerActivity, GXTrigger>(r => r.Trigger!, p => p.Id, triggerRows), cancellationToken);
        await ClearUserDataReferenceAsync(connection, transaction, new GXTrigger(), r => r.Device!,
            r => GXSql.Exists<GXTrigger, GXDevice>(r => r.Device!, p => p.Id, deviceRows), cancellationToken);
        await ClearUserDataReferenceAsync(connection, transaction, new GXTrigger(), r => r.DeviceGroup!,
            r => GXSql.Exists<GXTrigger, GXDeviceGroup>(r => r.DeviceGroup!, p => p.Id, deviceGroupRows), cancellationToken);
        await ClearUserDataReferenceAsync(connection, transaction, new GXTrigger(), r => r.Module!,
            r => GXSql.Exists<GXTrigger, GXModule>(r => r.Module!, p => p.Id, moduleRows), cancellationToken);
        await ClearUserDataReferenceAsync(connection, transaction, new GXTriggerProperty(), r => r.Module!,
            r => GXSql.Exists<GXTriggerProperty, GXModule>(r => r.Module!, p => p.Id, moduleRows), cancellationToken);
        await ClearUserDataReferenceAsync(connection, transaction, new GXTrigger(), r => r.User!,
            r => GXSql.Exists<GXTrigger, GXUser>(r => r.User!, p => p.Id!, userRows), cancellationToken);
        await ClearUserDataReferenceAsync(connection, transaction, new GXTrigger(), r => r.UserGroup!,
            r => GXSql.Exists<GXTrigger, GXUserGroup>(r => r.UserGroup!, p => p.Id, userGroupRows), cancellationToken);
        await ClearUserDataReferenceAsync(connection, transaction, new GXUser(), r => r.ProfilePicture!,
            r => GXSql.Exists<GXUser, GXAppearance>(r => r.ProfilePicture!, p => p.Id, appearanceRows), cancellationToken);
        await ClearUserDataReferenceAsync(connection, transaction, new GXUserProperty(), r => r.Module!,
            r => GXSql.Exists<GXUserProperty, GXModule>(r => r.Module!, p => p.Id, moduleRows), cancellationToken);
        await ClearUserDataReferenceAsync(connection, transaction, new GXWorkflow(), r => r.Device!,
            r => GXSql.Exists<GXWorkflow, GXDevice>(r => r.Device!, p => p.Id, deviceRows), cancellationToken);
        await ClearUserDataReferenceAsync(connection, transaction, new GXWorkflow(), r => r.DeviceGroup!,
            r => GXSql.Exists<GXWorkflow, GXDeviceGroup>(r => r.DeviceGroup!, p => p.Id, deviceGroupRows), cancellationToken);
        await ClearUserDataReferenceAsync(connection, transaction, new GXWorkflowProperty(), r => r.Module!,
            r => GXSql.Exists<GXWorkflowProperty, GXModule>(r => r.Module!, p => p.Id, moduleRows), cancellationToken);
        await ClearUserDataReferenceAsync(connection, transaction, new GXWorkflow(), r => r.TriggerMethod!,
            r => GXSql.Exists<GXWorkflow, GXScriptMethod>(r => r.TriggerMethod!, p => p.Id, scriptMethodRows), cancellationToken);
        await ClearUserDataReferenceAsync(connection, transaction, new GXWorkflow(), r => r.User!,
            r => GXSql.Exists<GXWorkflow, GXUser>(r => r.User!, p => p.Id!, userRows), cancellationToken);
        await ClearUserDataReferenceAsync(connection, transaction, new GXWorkflow(), r => r.UserGroup!,
            r => GXSql.Exists<GXWorkflow, GXUserGroup>(r => r.UserGroup!, p => p.Id, userGroupRows), cancellationToken);

        await connection.DeleteAsync(transaction, GXDeleteArgs.Delete<GXAgentGroupAgent>(r => GXSql.Exists<GXAgentGroupAgent, GXAgentGroup>(r => r.AgentGroupId, p => p.Id, agentGroupRows)), cancellationToken);
        await connection.DeleteAsync(transaction, GXDeleteArgs.Delete<GXAgentGroupDeviceGroup>(r => GXSql.Exists<GXAgentGroupDeviceGroup, GXAgentGroup>(r => r.AgentGroupId, p => p.Id, agentGroupRows)), cancellationToken);
        await connection.DeleteAsync(transaction, GXDeleteArgs.Delete<GXAgentScriptMethod>(r => GXSql.Exists<GXAgentScriptMethod, GXScriptMethod>(r => r.MethodId, p => p.Id, scriptMethodRows)), cancellationToken);
        await connection.DeleteAsync(transaction, GXDeleteArgs.Delete<GXBlockGroupBlock>(r => GXSql.Exists<GXBlockGroupBlock, GXBlockGroup>(r => r.BlockGroupId, p => p.Id, blockGroupRows)), cancellationToken);
        await connection.DeleteAsync(transaction, GXDeleteArgs.Delete<GXComponentViewGroupComponentView>(r => GXSql.Exists<GXComponentViewGroupComponentView, GXComponentViewGroup>(r => r.ComponentViewGroupId, p => p.Id, componentViewGroupRows)), cancellationToken);
        await connection.DeleteAsync(transaction, GXDeleteArgs.Delete<GXDeviceGroupDevice>(r => GXSql.Exists<GXDeviceGroupDevice, GXDeviceGroup>(r => r.DeviceGroupId, p => p.Id, deviceGroupRows)), cancellationToken);
        await connection.DeleteAsync(transaction, GXDeleteArgs.Delete<GXDeviceGroupProperty>(r => GXSql.Exists<GXDeviceGroupProperty, GXDeviceGroup>(r => r.DeviceGroup!, p => p.Id, deviceGroupRows)), cancellationToken);
        await connection.DeleteAsync(transaction, GXDeleteArgs.Delete<GXDeviceTemplateGroupDeviceTemplate>(r => GXSql.Exists<GXDeviceTemplateGroupDeviceTemplate, GXDeviceTemplateGroup>(r => r.DeviceTemplateGroupId, p => p.Id, deviceTemplateGroupRows)), cancellationToken);
        await connection.DeleteAsync(transaction, GXDeleteArgs.Delete<GXFavorite>(r => GXSql.Exists<GXFavorite, GXUser>(r => r.User!, p => p.Id!, userRows)), cancellationToken);
        await connection.DeleteAsync(transaction, GXDeleteArgs.Delete<GXGatewayDevice>(r => GXSql.Exists<GXGatewayDevice, GXGateway>(r => r.GatewayId, p => p.Id, gatewayRows)), cancellationToken);
        await connection.DeleteAsync(transaction, GXDeleteArgs.Delete<GXGatewayDeviceGroup>(r => GXSql.Exists<GXGatewayDeviceGroup, GXDeviceGroup>(r => r.DeviceGroupId, p => p.Id, deviceGroupRows)), cancellationToken);
        await connection.DeleteAsync(transaction, GXDeleteArgs.Delete<GXGatewayGroupGateway>(r => GXSql.Exists<GXGatewayGroupGateway, GXGatewayGroup>(r => r.GatewayGroupId, p => p.Id, gatewayGroupRows)), cancellationToken);
        await connection.DeleteAsync(transaction, GXDeleteArgs.Delete<GXKeyManagementGroupKeyManagement>(r => GXSql.Exists<GXKeyManagementGroupKeyManagement, GXKeyManagementGroup>(r => r.KeyManagementGroupId, p => p.Id, keyManagementGroupRows)), cancellationToken);
        await connection.DeleteAsync(transaction, GXDeleteArgs.Delete<GXKeyManagementKey>(r => GXSql.Exists<GXKeyManagementKey, GXKeyManagement>(r => r.KeyManagement!, p => p.Id, keyManagementRows)), cancellationToken);
        await connection.DeleteAsync(transaction, GXDeleteArgs.Delete<GXLog>(r => GXSql.Exists<GXLog, GXLogType>(r => r.LogType!, p => p.Id, logTypeRows)
            || GXSql.Exists<GXLog, GXUser>(r => r.User!, p => p.Id!, userRows)), cancellationToken);
        await connection.DeleteAsync(transaction, GXDeleteArgs.Delete<GXLogType>(r => GXSql.Exists<GXLogType, GXUser>(r => r.Creator!, p => p.Id!, userRows)), cancellationToken);
        await connection.DeleteAsync(transaction, GXDeleteArgs.Delete<GXManufacturerGroupManufacturer>(r => GXSql.Exists<GXManufacturerGroupManufacturer, GXManufacturerGroup>(r => r.ManufacturerGroupId, p => p.Id, manufacturerGroupRows)), cancellationToken);
        await connection.DeleteAsync(transaction, GXDeleteArgs.Delete<GXMenuGroupMenu>(r => GXSql.Exists<GXMenuGroupMenu, GXMenuGroup>(r => r.MenuGroupId, p => p.Id, menuGroupRows)), cancellationToken);
        await connection.DeleteAsync(transaction, GXDeleteArgs.Delete<GXModuleGroupModule>(r => GXSql.Exists<GXModuleGroupModule, GXModuleGroup>(r => r.ModuleGroupId, p => p.Id, moduleGroupRows)), cancellationToken);
        // Module-to-source cascading is disabled to avoid multiple cascade paths in MSSQL.
        // Deleting sources still cascades to their subscription mappings.
        await connection.DeleteAsync(transaction, GXDeleteArgs.Delete<GXNotificationSource>(r => GXSql.Exists<GXNotificationSource, GXModule>(r => r.ModuleId!, p => p.Id, moduleRows)), cancellationToken);
        await connection.DeleteAsync(transaction, GXDeleteArgs.Delete<GXNotificationType>(r => GXSql.Exists<GXNotificationType, GXUser>(r => r.Creator!, p => p.Id!, userRows)), cancellationToken);
        await connection.DeleteAsync(transaction, GXDeleteArgs.Delete<GXScheduleGroupSchedule>(r => GXSql.Exists<GXScheduleGroupSchedule, GXScheduleGroup>(r => r.ScheduleGroupId, p => p.Id, scheduleGroupRows)), cancellationToken);
        await connection.DeleteAsync(transaction, GXDeleteArgs.Delete<GXScheduleModule>(r => GXSql.Exists<GXScheduleModule, GXModule>(r => r.ModuleId, p => p.Id, moduleRows)), cancellationToken);
        await connection.DeleteAsync(transaction, GXDeleteArgs.Delete<GXScheduleScript>(r => GXSql.Exists<GXScheduleScript, GXSchedule>(r => r.ScheduleId, p => p.Id, scheduleRows)), cancellationToken);
        await connection.DeleteAsync(transaction, GXDeleteArgs.Delete<GXScheduleToDevice>(r => GXSql.Exists<GXScheduleToDevice, GXSchedule>(r => r.ScheduleId, p => p.Id, scheduleRows)), cancellationToken);
        await connection.DeleteAsync(transaction, GXDeleteArgs.Delete<GXScheduleToDeviceAttributeTemplate>(r => GXSql.Exists<GXScheduleToDeviceAttributeTemplate, GXSchedule>(r => r.ScheduleId, p => p.Id, scheduleRows)), cancellationToken);
        await connection.DeleteAsync(transaction, GXDeleteArgs.Delete<GXScheduleToDeviceGroup>(r => GXSql.Exists<GXScheduleToDeviceGroup, GXSchedule>(r => r.ScheduleId, p => p.Id, scheduleRows)), cancellationToken);
        await connection.DeleteAsync(transaction, GXDeleteArgs.Delete<GXScheduleToDeviceGroupAttributeTemplate>(r => GXSql.Exists<GXScheduleToDeviceGroupAttributeTemplate, GXSchedule>(r => r.ScheduleId, p => p.Id, scheduleRows)), cancellationToken);
        await connection.DeleteAsync(transaction, GXDeleteArgs.Delete<GXScheduleToDeviceGroupObjectTemplate>(r => GXSql.Exists<GXScheduleToDeviceGroupObjectTemplate, GXSchedule>(r => r.ScheduleId, p => p.Id, scheduleRows)), cancellationToken);
        await connection.DeleteAsync(transaction, GXDeleteArgs.Delete<GXScheduleToDeviceObjectTemplate>(r => GXSql.Exists<GXScheduleToDeviceObjectTemplate, GXSchedule>(r => r.ScheduleId, p => p.Id, scheduleRows)), cancellationToken);
        await connection.DeleteAsync(transaction, GXDeleteArgs.Delete<GXScheduleTrigger>(r => GXSql.Exists<GXScheduleTrigger, GXSchedule>(r => r.ScheduleId, p => p.Id, scheduleRows)), cancellationToken);
        await connection.DeleteAsync(transaction, GXDeleteArgs.Delete<GXScriptGroupScript>(r => GXSql.Exists<GXScriptGroupScript, GXScriptGroup>(r => r.ScriptGroupId, p => p.Id, scriptGroupRows)), cancellationToken);
        await connection.DeleteAsync(transaction, GXDeleteArgs.Delete<GXTask>(r => GXSql.Exists<GXTask, GXUser>(r => r.Creator!, p => p.Id!, userRows)
            || GXSql.Exists<GXTask, GXScriptMethod>(r => r.ScriptMethod!, p => p.Id, scriptMethodRows)
            || GXSql.Exists<GXTask, GXModule>(r => r.Module!, p => p.Id, moduleRows)
            || GXSql.Exists<GXTask, GXDeviceGroup>(r => r.DeviceGroup!, p => p.Id, deviceGroupRows)
            || GXSql.Exists<GXTask, GXDevice>(r => r.Device!, p => p.Id, deviceRows)
            || GXSql.Exists<GXTask, GXObject>(r => r.Object!, p => p.Id, objectRows)
            || GXSql.Exists<GXTask, GXSchedule>(r => r.TriggerSchedule!, p => p.Id, scheduleRows)
            || GXSql.Exists<GXTask, GXUser>(r => r.TriggerUser!, p => p.Id!, userRows)
            || GXSql.Exists<GXTask, GXScript>(r => r.TriggerScript!, p => p.Id, scriptRows)
            || GXSql.Exists<GXTask, GXScript>(r => r.TriggerModule!, p => p.Id, scriptRows)
            || GXSql.Exists<GXTask, GXAgent>(r => r.OperatingAgent!, p => p.Id, agentRows)
            || GXSql.Exists<GXTask, GXScriptMethod>(r => r.Condition!, p => p.Id, scriptMethodRows)
            || GXSql.Exists<GXTask, GXScriptMethod>(r => r.Action!, p => p.Id, scriptMethodRows)
            || GXSql.Exists<GXTask, GXDevice>(r => r.TargetDevice!, p => p.Id, deviceRows)), cancellationToken);
        await connection.DeleteAsync(transaction, GXDeleteArgs.Delete<GXTriggerGroupTrigger>(r => GXSql.Exists<GXTriggerGroupTrigger, GXTriggerGroup>(r => r.TriggerGroupId, p => p.Id, triggerGroupRows)), cancellationToken);
        await connection.DeleteAsync(transaction, GXDeleteArgs.Delete<GXUserBlockSettings>(r => GXSql.Exists<GXUserBlockSettings, GXUser>(r => r.UserId!, p => p.Id!, userRows)), cancellationToken);
        await connection.DeleteAsync(transaction, GXDeleteArgs.Delete<GXUserGroupAgentGroup>(r => GXSql.Exists<GXUserGroupAgentGroup, GXUserGroup>(r => r.UserGroupId, p => p.Id, userGroupRows)), cancellationToken);
        await connection.DeleteAsync(transaction, GXDeleteArgs.Delete<GXUserGroupBlockGroup>(r => GXSql.Exists<GXUserGroupBlockGroup, GXUserGroup>(r => r.UserGroupId, p => p.Id, userGroupRows)), cancellationToken);
        await connection.DeleteAsync(transaction, GXDeleteArgs.Delete<GXUserGroupComponentViewGroup>(r => GXSql.Exists<GXUserGroupComponentViewGroup, GXUserGroup>(r => r.UserGroupId, p => p.Id, userGroupRows)), cancellationToken);
        await connection.DeleteAsync(transaction, GXDeleteArgs.Delete<GXUserGroupDeviceGroup>(r => GXSql.Exists<GXUserGroupDeviceGroup, GXUserGroup>(r => r.UserGroupId, p => p.Id, userGroupRows)), cancellationToken);
        await connection.DeleteAsync(transaction, GXDeleteArgs.Delete<GXUserGroupDeviceTemplateGroup>(r => GXSql.Exists<GXUserGroupDeviceTemplateGroup, GXUserGroup>(r => r.UserGroupId, p => p.Id, userGroupRows)), cancellationToken);
        await connection.DeleteAsync(transaction, GXDeleteArgs.Delete<GXUserGroupGatewayGroup>(r => GXSql.Exists<GXUserGroupGatewayGroup, GXUserGroup>(r => r.UserGroupId, p => p.Id, userGroupRows)), cancellationToken);
        await connection.DeleteAsync(transaction, GXDeleteArgs.Delete<GXUserGroupWorkflowGroup>(r => GXSql.Exists<GXUserGroupWorkflowGroup, GXUserGroup>(r => r.UserGroupId, p => p.Id, userGroupRows)), cancellationToken);
        await connection.DeleteAsync(transaction, GXDeleteArgs.Delete<GXUserGroupKeyManagementGroup>(r => GXSql.Exists<GXUserGroupKeyManagementGroup, GXUserGroup>(r => r.UserGroupId, p => p.Id, userGroupRows)), cancellationToken);
        await connection.DeleteAsync(transaction, GXDeleteArgs.Delete<GXUserGroupManufacturerGroup>(r => GXSql.Exists<GXUserGroupManufacturerGroup, GXUserGroup>(r => r.UserGroupId, p => p.Id, userGroupRows)), cancellationToken);
        await connection.DeleteAsync(transaction, GXDeleteArgs.Delete<GXUserGroupMenuGroup>(r => GXSql.Exists<GXUserGroupMenuGroup, GXUserGroup>(r => r.UserGroupId, p => p.Id, userGroupRows)), cancellationToken);
        await connection.DeleteAsync(transaction, GXDeleteArgs.Delete<GXUserGroupModuleGroup>(r => GXSql.Exists<GXUserGroupModuleGroup, GXUserGroup>(r => r.UserGroupId, p => p.Id, userGroupRows)), cancellationToken);
        await connection.DeleteAsync(transaction, GXDeleteArgs.Delete<GXUserGroupScheduleGroup>(r => GXSql.Exists<GXUserGroupScheduleGroup, GXUserGroup>(r => r.UserGroupId, p => p.Id, userGroupRows)), cancellationToken);
        await connection.DeleteAsync(transaction, GXDeleteArgs.Delete<GXUserGroupScriptGroup>(r => GXSql.Exists<GXUserGroupScriptGroup, GXUserGroup>(r => r.UserGroupId, p => p.Id, userGroupRows)), cancellationToken);
        await connection.DeleteAsync(transaction, GXDeleteArgs.Delete<GXUserGroupTriggerGroup>(r => GXSql.Exists<GXUserGroupTriggerGroup, GXUserGroup>(r => r.UserGroupId, p => p.Id, userGroupRows)), cancellationToken);
        await connection.DeleteAsync(transaction, GXDeleteArgs.Delete<GXUserGroupUser>(r => GXSql.Exists<GXUserGroupUser, GXUser>(r => r.UserId!, p => p.Id!, userRows)), cancellationToken);
        await connection.DeleteAsync(transaction, GXDeleteArgs.Delete<GXUserGroupViewGroup>(r => GXSql.Exists<GXUserGroupViewGroup, GXUserGroup>(r => r.UserGroupId, p => p.Id, userGroupRows)), cancellationToken);
        await connection.DeleteAsync(transaction, GXDeleteArgs.Delete<GXViewGroupView>(r => GXSql.Exists<GXViewGroupView, GXViewGroup>(r => r.ViewGroupId, p => p.Id, viewGroupRows)), cancellationToken);
        await connection.DeleteAsync(transaction, GXDeleteArgs.Delete<GXWorkflowGroupWorkflow>(r => GXSql.Exists<GXWorkflowGroupWorkflow, GXWorkflowGroup>(r => r.WorkflowGroupId, p => p.Id, workflowGroupRows)), cancellationToken);
        await connection.DeleteAsync(transaction, GXDeleteArgs.Delete<GXWorkflowModule>(r => GXSql.Exists<GXWorkflowModule, GXWorkflow>(r => r.WorkflowId, p => p.Id, workflowRows)), cancellationToken);
        await connection.DeleteAsync(transaction, GXDeleteArgs.Delete<GXWorkflowScriptMethod>(r => GXSql.Exists<GXWorkflowScriptMethod, GXWorkflow>(r => r.WorkflowId, p => p.Id, workflowRows)), cancellationToken);
        await connection.DeleteAsync(transaction, GXDeleteArgs.Delete<GXAttribute>(r => GXSql.Exists<GXAttribute, GXAttributeTemplate>(r => r.Template!, p => p.Id, attributeTemplateRows)), cancellationToken);
        await connection.DeleteAsync(transaction, GXDeleteArgs.Delete<GXBlock>(r => GXSql.Exists<GXBlock, GXUser>(r => r.Creator!, p => p.Id!, userRows)), cancellationToken);
        await connection.DeleteAsync(transaction, GXDeleteArgs.Delete<GXGateway>(r => GXSql.Exists<GXGateway, GXUser>(r => r.Creator!, p => p.Id!, userRows)), cancellationToken);
        await connection.DeleteAsync(transaction, GXDeleteArgs.Delete<GXKeyManagement>(r => GXSql.Exists<GXKeyManagement, GXUser>(r => r.Creator!, p => p.Id!, userRows)), cancellationToken);
        await connection.DeleteAsync(transaction, GXDeleteArgs.Delete<GXDevice>(r => GXSql.Exists<GXDevice, GXUser>(r => r.Creator!, p => p.Id!, userRows)), cancellationToken);
    }

    private static async Task ClearUserDataReferenceAsync<T>(GXDbConnection connection, IDbTransaction transaction,
        T row, Expression<Func<T, object>> column, Expression<Func<T, bool>> predicate, CancellationToken cancellationToken)
    {
        // Resolve the affected keys before updating. MySQL cannot update a table
        // while a nested ownership query also reads that table. Selecting just
        // the key also leaves the reference column null on each update object.
        var parameter = Expression.Parameter(typeof(T), "row");
        var key = Expression.Lambda<Func<T, object>>(
            Expression.Convert(Expression.Property(parameter, "Id"), typeof(object)), parameter);
        var rows = await connection.SelectAsync<T>(transaction,
            GXSelectArgs.Select(key, predicate), cancellationToken);
        foreach (var item in rows)
        {
            // Update(value, column) supplies the selected primary key predicate.
            await connection.UpdateAsync(transaction, GXUpdateArgs.Update(item, column), cancellationToken);
        }
    }
}
