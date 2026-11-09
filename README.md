<a id="web-client-ui"></a>

# Gurux DLMS AMI UI Guide

Gurux DLMS AMI manages DLMS meters, communication services and automated meter operations. This guide describes the default UI tabs; permissions, configured menus and installed modules determine which tabs you see.

## Getting started

Sign in at the address supplied by your administrator. Choose an area from the navigation menu, filter its list and open an entry to view or edit it. Save the containing form after changing settings or selected members. Favorites provide shortcuts to frequently used pages.

To connect your first meter:

1. Select an existing meter model from Manufacturer settings, or import a device template created with GXDLMSDirector.
2. Create a meter in Devices, select the device template and set the correct communication parameters for the meter.
3. Install an agent and create an access token for it.

An administrator also configures communication services and grants user access. Creation of the first administrator account starts background refreshes of the agent, module and manufacturer catalogs; these lists may populate as the refreshes finish.

Open a device's Objects tab to inspect meter values and supported operations, then follow Tasks and logs to see the result. Stored values can be older readings; request a read when you need a current value.

Management tabs list all entries you can access. Tabs inside an editor select settings or associations for that one entry. Group membership tabs change associations. The help control opens a side panel for the selected tab without leaving the current form. Close it with the close button or Escape. The panel follows the active tab and also shows object and module guides.

## Devices

<a id="ActiveDeviceErrors"></a><a id="CommunicationWindow"></a><a id="CommunicationWindowDialog"></a><a id="Device"></a><a id="DeviceAction"></a><a id="DeviceActions"></a><a id="DeviceDropdownSelector"></a><a id="DeviceGroup"></a><a id="DeviceGroups"></a><a id="DeviceGroupSelector"></a><a id="DeviceGroupsSelector"></a><a id="DeviceGroupTab"></a><a id="DeviceLog"></a><a id="DeviceLogs"></a><a id="DeviceManage"></a><a id="Devices"></a><a id="DeviceSelector"></a><a id="DevicesSelector"></a><a id="DeviceTab"></a><a id="DeviceTrace"></a><a id="DeviceTraces"></a><a id="NetworkSettings"></a><a id="SerialSettings"></a><a id="DeviceGroupGeneral"></a><a id="DeviceGeneral"></a>

### Management tabs

| Tab | Purpose |
| --- | --- |
| <a id="devices"></a>Devices | Browse devices and open entries to edit them. |
| <a id="devices.groups"></a>Device groups | Browse device groups. |
| <a id="devices.logs"></a>Device logs | Review recorded device logs. |
| <a id="devices.actions"></a>Device actions | Review recorded device actions. |
| <a id="devices.traces"></a>Device traces | Inspect communication traces. |

### Device editor

| Tab | Purpose |
| --- | --- |
| <a id="device"></a>General information | Edit the device information and settings. |
| <a id="device.profile-picture"></a>Profile picture | Choose the picture for this entry. |
| <a id="device.groups"></a>Device groups | Choose the groups this entry belongs to. |
| <a id="device.objects"></a>Objects | Inspect COSEM objects, attributes and supported operations. |

### Device group editor

| Tab | Purpose |
| --- | --- |
| <a id="device-group"></a>General information | Edit the group name and settings. |
| <a id="device-group.devices"></a>Devices | Choose the devices associated with this group. |
| <a id="device-group.user-groups"></a>User groups | Choose the user groups associated with this group. |
| <a id="device-group.agent-groups"></a>Agent groups | Choose the agent groups associated with this group. |

## Schedules

<a id="Schedule"></a><a id="ScheduleEdit"></a><a id="ScheduleGroup"></a><a id="ScheduleGroups"></a><a id="ScheduleGroupsSelector"></a><a id="ScheduleGroupTab"></a><a id="ScheduleLog"></a><a id="ScheduleLogs"></a><a id="ScheduleManage"></a><a id="scheduler"></a><a id="SchedulesSelector"></a><a id="ScheduleTab"></a>

### Management tabs

| Tab | Purpose |
| --- | --- |
| <a id="schedules"></a>Schedules | Browse schedules and open entries to edit them. |
| <a id="schedules.groups"></a>Schedule groups | Browse schedule groups. |
| <a id="schedules.logs"></a>Schedule logs | Review recorded schedule logs. |

### Schedule editor

| Tab | Purpose |
| --- | --- |
| <a id="schedule"></a>General information | Edit the schedule information and settings. |
| <a id="schedule.groups"></a>Schedule groups | Choose the groups this entry belongs to. |
| <a id="schedule.devices"></a>Devices | Select the devices, objects and attributes used by this schedule. |
| <a id="schedule.device-groups"></a>Device groups | Select target device groups, objects and attributes. |
| <a id="schedule.modules"></a>Modules | Choose modules associated with this schedule. |

### Schedule group editor

| Tab | Purpose |
| --- | --- |
| <a id="schedule-group"></a>General information | Edit the group name and settings. |
| <a id="schedule-group.schedules"></a>Schedules | Choose the schedules associated with this group. |
| <a id="schedule-group.user-groups"></a>User groups | Choose the user groups associated with this group. |

## Device templates

<a id="Advanced"></a><a id="AdvancedEdit"></a><a id="Ciphering"></a><a id="DeviceTemplate"></a><a id="DeviceTemplateGroup"></a><a id="DeviceTemplateGroups"></a><a id="DeviceTemplateGroupsSelector"></a><a id="DeviceTemplateGroupTab"></a><a id="DeviceTemplateManage"></a><a id="DeviceTemplates"></a><a id="DeviceTemplateSelector"></a><a id="DeviceTemplatesSelector"></a><a id="DeviceTemplateTab"></a><a id="Profiles"></a><a id="DeviceTemplateGeneral"></a><a id="DeviceTemplateGroupGeneral"></a>

### Management tabs

| Tab | Purpose |
| --- | --- |
| <a id="device-templates"></a>Device templates | Browse device templates and open entries to edit them. |
| <a id="device-templates.groups"></a>Device template groups | Browse device template groups. |

### Device template editor

| Tab | Purpose |
| --- | --- |
| <a id="device-template"></a>General information | Edit the device template information and settings. |
| <a id="device-template.profile-picture"></a>Profile picture | Choose the picture for this entry. |
| <a id="device-template.groups"></a>Device template groups | Choose the groups this entry belongs to. |
| <a id="device-template.object-templates"></a>Object templates | Maintain object definitions and default values. |

### Communication settings inside General information

| Tab | Purpose |
| --- | --- |
| <a id="device-template.profiles"></a>Profiles | Configure communication profiles. |
| <a id="device-template.ciphering"></a>Ciphering | Configure ciphering and security keys. |
| <a id="device-template.advanced"></a>Advanced | Configure advanced DLMS communication options. |

### Device template group editor

| Tab | Purpose |
| --- | --- |
| <a id="device-template-group"></a>General information | Edit the group name and settings. |
| <a id="device-template-group.device-templates"></a>Device templates | Choose the device templates associated with this group. |
| <a id="device-template-group.user-groups"></a>User groups | Choose the user groups associated with this group. |

## Agents

<a id="Agent"></a><a id="AgentAdd"></a><a id="AgentGroup"></a><a id="AgentGroups"></a><a id="AgentGroupsSelector"></a><a id="AgentGroupTab"></a><a id="AgentInstallers"></a><a id="AgentLog"></a><a id="AgentLogs"></a><a id="AgentManage"></a><a id="Agents"></a><a id="AgentsSelector"></a><a id="AgentTab"></a><a id="AutoConnectOptions"></a><a id="GatewayOptions"></a><a id="NotifyOptions"></a><a id="ReaderOptions"></a><a id="RawNotificationOptions"></a>

To install an agent, open Add agent and choose a version before selecting Download the agent. The list contains all available installer versions, with the newest selected by default. Downloads come from Gurux.AMI.Catalog. Agent versions are available independently of the server's Script and Shared library versions. After an agent update and restart, the agent prepares its configured scripts again. If a server-provided script was compiled against different library versions, the agent downloads its source and compiles it against its own libraries before use. Compilation failures are reported and the incompatible server DLL is not executed.

Watch the YouTube tutorial: [Gurux.DLMS.AMI Tutorial: Agent Installation and Updates](https://youtu.be/Mw7RpY1ZjEc?si=DRUmR9B6Hi8w7SR9).

### Management tabs

| Tab | Purpose |
| --- | --- |
| <a id="agents"></a>Agents | Browse agents and open entries to edit them. |
| <a id="agents.groups"></a>Agent groups | Browse agent groups. |
| <a id="agents.logs"></a>Agent logs | Review recorded agent logs. |

### Agent editor

| Tab | Purpose |
| --- | --- |
| <a id="agent"></a>General information | Edit the agent information and settings. |
| <a id="agent.groups"></a>Agent groups | Choose the groups this entry belongs to. |

### Service settings inside General information

| Tab | Purpose |
| --- | --- |
| <a id="agent.reader"></a>Reader | Configure meter reading services. |
| <a id="agent.push-events"></a>Push events | Configure reception of meter push events. |
| <a id="agent.raw-notifications"></a>Raw notifications | Configure reception of raw notifications. |
| <a id="agent.auto-connect"></a>Auto connect | Configure incoming automatic connections. |
| <a id="agent.gateway"></a>Gateway | Configure the agent gateway service. |

### Agent group editor

| Tab | Purpose |
| --- | --- |
| <a id="agent-group"></a>General information | Edit the group name and settings. |
| <a id="agent-group.agents"></a>Agents | Choose the agents associated with this group. |
| <a id="agent-group.user-groups"></a>User groups | Choose the user groups associated with this group. |
| <a id="agent-group.device-groups"></a>Device groups | Choose the device groups associated with this group. |

## Gateways

<a id="Gateway"></a><a id="GatewayGroup"></a><a id="GatewayGroups"></a><a id="GatewayGroupsSelector"></a><a id="GatewayGroupTab"></a><a id="GatewayLog"></a><a id="GatewayLogs"></a><a id="GatewayManage"></a><a id="Gateways"></a><a id="GatewaysSelector"></a><a id="GatewayTab"></a><a id="GatewayGroupGeneral"></a><a id="GatewayGeneral"></a>

### Management tabs

| Tab | Purpose |
| --- | --- |
| <a id="gateways"></a>Gateways | Browse gateways and open entries to edit them. |
| <a id="gateways.groups"></a>Gateway groups | Browse gateway groups. |
| <a id="gateways.logs"></a>Gateway logs | Review recorded gateway logs. |

### Gateway editor

| Tab | Purpose |
| --- | --- |
| <a id="gateway"></a>General information | Edit the gateway information and settings. |
| <a id="gateway.groups"></a>Gateway groups | Choose the groups this entry belongs to. |
| <a id="gateway.device-groups"></a>Device groups | Choose device groups assigned to this gateway. |
| <a id="gateway.devices"></a>Devices | Choose devices assigned to this gateway. |

### Gateway group editor

| Tab | Purpose |
| --- | --- |
| <a id="gateway-group"></a>General information | Edit the group name and settings. |
| <a id="gateway-group.gateways"></a>Gateways | Choose the gateways associated with this group. |
| <a id="gateway-group.user-groups"></a>User groups | Choose the user groups associated with this group. |

## Users and accounts

<a id="Account"></a><a id="PolicyList"></a><a id="Privacy"></a><a id="Profile"></a><a id="RoleEditor"></a><a id="Roles"></a><a id="User"></a><a id="UserAction"></a><a id="UserActions"></a><a id="UserError"></a><a id="UserErrors"></a><a id="UserGroup"></a><a id="UserGroups"></a><a id="UserGroupsSelector"></a><a id="UserGroupTab"></a><a id="UserManage"></a><a id="UserPermissionEditor"></a><a id="UserRoleEditor"></a><a id="Users"></a><a id="UserSelector"></a><a id="UsersSelector"></a><a id="UserTab"></a><a id="PersonalAccessTokens"></a><a id="UserGroupGeneral"></a><a id="UserGeneral"></a>

### Management tabs

| Tab | Purpose |
| --- | --- |
| <a id="users"></a>Users | Browse users and open entries to edit them. |
| <a id="users.groups"></a>User groups | Browse user groups. |
| <a id="users.errors"></a>User errors | Review recorded user errors. |
| <a id="users.actions"></a>User actions | Review recorded user actions. |

### User editor

| Tab | Purpose |
| --- | --- |
| <a id="user"></a>General information | Edit the user information and settings. |
| <a id="user.profile-picture"></a>Profile picture | Choose the picture for this entry. |
| <a id="user.roles"></a>Roles | Assign roles to this user. |
| <a id="user.permissions"></a>Permissions | Review and change user permissions. |
| <a id="user.groups"></a>Groups | Choose the groups this entry belongs to. |
| <a id="user.notifications"></a>Notifications | Configure this user's notification subscriptions. |
| <a id="user.personal-access-tokens"></a>Personal access tokens | Manage tokens for API access. |

### User group editor

| Tab | Purpose |
| --- | --- |
| <a id="user-group"></a>General information | Edit the group name and settings. |
| <a id="user-group.users"></a>Users | Choose the users associated with this group. |
| <a id="user-group.device-groups"></a>Device groups | Choose the device groups associated with this group. |
| <a id="user-group.schedule-groups"></a>Schedule groups | Choose the schedule groups associated with this group. |

Your account menu provides Profile, Email, Password, Two-factor authentication and Personal data pages; External logins appears when an external sign-in provider is available. Configuration > Account controls who can register accounts and whether administrator approval is required.

## Modules

<a id="Module"></a><a id="ModuleGroup"></a><a id="ModuleGroups"></a><a id="ModuleGroupsSelector"></a><a id="ModuleGroupTab"></a><a id="ModuleLog"></a><a id="ModuleLogs"></a><a id="ModuleManage"></a><a id="Modules"></a><a id="ModuleSelector"></a><a id="ModulesManage"></a><a id="ModulesSelector"></a><a id="ModuleTab"></a><a id="ModuleGroupGeneral"></a><a id="ModuleGeneral"></a>

### Management tabs

| Tab | Purpose |
| --- | --- |
| <a id="modules"></a>Modules | Browse modules and open entries to edit them. |
| <a id="modules.groups"></a>Module groups | Browse module groups. |
| <a id="modules.logs"></a>Module logs | Review recorded module logs. |

### Module editor

| Tab | Purpose |
| --- | --- |
| <a id="module"></a>General information | Edit the module information and settings. |
| <a id="module.groups"></a>Module groups | Choose the groups this entry belongs to. |
| <a id="module.policies"></a>Policies | Configure the module access policies. |

### Module group editor

| Tab | Purpose |
| --- | --- |
| <a id="module-group"></a>General information | Edit the group name and settings. |
| <a id="module-group.modules"></a>Modules | Choose the modules associated with this group. |
| <a id="module-group.user-groups"></a>User groups | Choose the user groups associated with this group. |

Modules can add their own tabs to management pages and editors. Their titles and functions come from the installed module; use its documentation for those tabs.

## Scripts

<a id="ScriptGroup"></a><a id="ScriptGroups"></a><a id="ScriptGroupsSelector"></a><a id="ScriptGroupTab"></a><a id="ScriptLog"></a><a id="ScriptLogs"></a><a id="ScriptManage"></a><a id="ScriptMethodSelector"></a><a id="ScriptMethodsSelector"></a><a id="ScriptRebuild"></a><a id="Scripts"></a><a id="ScriptsSelector"></a><a id="ScriptTab"></a><a id="ScriptView"></a><a id="ScriptGroupGeneral"></a><a id="ScriptGeneral"></a><a id="Script"></a>

### Management tabs

| Tab | Purpose |
| --- | --- |
| <a id="scripts"></a>Scripts | Browse scripts and open entries to edit them. |
| <a id="scripts.groups"></a>Script groups | Browse script groups. |
| <a id="scripts.logs"></a>Script logs | Review recorded script logs. |

### Script editor

| Tab | Purpose |
| --- | --- |
| <a id="script"></a>General information | Edit the script information and settings. |
| <a id="script.groups"></a>Script groups | Choose the groups this entry belongs to. |
| <a id="script.policies"></a>Policies (inside General information) | Configure access policies for this entry. |

### Script group editor

| Tab | Purpose |
| --- | --- |
| <a id="script-group"></a>General information | Edit the group name and settings. |
| <a id="script-group.scripts"></a>Scripts | Choose the scripts associated with this group. |
| <a id="script-group.user-groups"></a>User groups | Choose the user groups associated with this group. |

## Workflows

<a id="Workflow"></a><a id="WorkflowGroup"></a><a id="WorkflowGroups"></a><a id="WorkflowGroupsSelector"></a><a id="WorkflowGroupTab"></a><a id="WorkflowLog"></a><a id="WorkflowLogs"></a><a id="WorkflowManage"></a><a id="Workflows"></a><a id="WorkflowsSelector"></a><a id="WorkflowTab"></a><a id="WorkflowGeneral"></a><a id="WorkflowGroupGeneral"></a>

### Management tabs

| Tab | Purpose |
| --- | --- |
| <a id="workflows"></a>Workflows | Browse workflows and open entries to edit them. |
| <a id="workflows.groups"></a>Workflow groups | Browse workflow groups. |
| <a id="workflows.logs"></a>Workflow logs | Review recorded workflow logs. |

### Workflow editor

| Tab | Purpose |
| --- | --- |
| <a id="workflow"></a>General information | Edit the workflow information and settings. |
| <a id="workflow.groups"></a>Workflow groups | Choose the groups this entry belongs to. |
| <a id="workflow.script-methods"></a>Script methods | Choose the script methods used by this workflow. |
| <a id="workflow.policies"></a>Policies (inside General information) | Configure access policies for this entry. |

### Workflow group editor

| Tab | Purpose |
| --- | --- |
| <a id="workflow-group"></a>General information | Edit the group name and settings. |
| <a id="workflow-group.workflows"></a>Workflows | Choose the workflows associated with this group. |
| <a id="workflow-group.user-groups"></a>User groups | Choose the user groups associated with this group. |

## Triggers

<a id="Trigger"></a><a id="TriggerGroup"></a><a id="TriggerGroups"></a><a id="TriggerGroupTab"></a><a id="TriggerManage"></a><a id="Triggers"></a><a id="TriggersSelector"></a><a id="TriggerGroupGeneral"></a>

### Management tabs

| Tab | Purpose |
| --- | --- |
| <a id="triggers"></a>Triggers | Browse triggers and open entries to edit them. |

### Trigger editor

| Tab | Purpose |
| --- | --- |
| <a id="trigger"></a>General information | Edit the trigger information and settings. |
| <a id="trigger.policies"></a>Policies (inside General information) | Configure access policies for this entry. |

### Trigger group editor

| Tab | Purpose |
| --- | --- |
| <a id="trigger-group"></a>General information | Edit the group name and settings. |
| <a id="trigger-group.triggers"></a>Triggers | Choose the triggers associated with this group. |
| <a id="trigger-group.user-groups"></a>User groups | Choose the user groups associated with this group. |

## Views

<a id="Columns"></a><a id="View"></a><a id="ViewGeneral"></a><a id="ViewGroup"></a><a id="ViewGroups"></a><a id="ViewGroupsSelector"></a><a id="ViewManage"></a><a id="Views"></a><a id="ViewsSelector"></a>

### Management tabs

| Tab | Purpose |
| --- | --- |
| <a id="views"></a>Views | Browse views and open entries to edit them. |
| <a id="views.groups"></a>View groups | Browse view groups. |

### View editor

| Tab | Purpose |
| --- | --- |
| <a id="view"></a>General information | Edit the view information and settings. |
| <a id="view.groups"></a>View groups | Choose the groups this entry belongs to. |
| <a id="view.policies"></a>Policies (inside General information) | Configure access policies for this entry. |

### View group editor

| Tab | Purpose |
| --- | --- |
| <a id="view-group"></a>General information | Edit the group name and settings. |
| <a id="view-group.views"></a>Views | Choose the views associated with this group. |
| <a id="view-group.user-groups"></a>User groups | Choose the user groups associated with this group. |

### View column editor

| Tab | Purpose |
| --- | --- |
| General information | Edit the column information and display settings. |
| <a id="columns.policies"></a>Policies | Configure access policies for the column information and display settings. |

## Component views

<a id="Component"></a><a id="ComponentView"></a><a id="ComponentViewGroup"></a><a id="ComponentViewGroups"></a><a id="ComponentViewGroupsSelector"></a><a id="ComponentViewGroupTab"></a><a id="ComponentViewManage"></a><a id="ComponentViews"></a><a id="ComponentViewsSelector"></a><a id="ComponentViewTab"></a><a id="ComponentViewGroupGeneral"></a><a id="ComponentViewGeneral"></a>

### Management tabs

| Tab | Purpose |
| --- | --- |
| <a id="components"></a>Component views | Browse component views and open entries to edit them. |

### Component view editor

| Tab | Purpose |
| --- | --- |
| <a id="component-view"></a>General information | Edit the component view information and settings. |
| <a id="component-view.groups"></a>Component view groups | Choose the groups this entry belongs to. |

### Component view group editor

| Tab | Purpose |
| --- | --- |
| <a id="component-view-group"></a>General information | Edit the group name and settings. |
| <a id="component-view-group.component-views"></a>Component views | Choose the component views associated with this group. |
| <a id="component-view-group.user-groups"></a>User groups | Choose the user groups associated with this group. |

## Blocks

<a id="Block"></a><a id="BlockGroup"></a><a id="BlockGroups"></a><a id="BlockGroupsSelector"></a><a id="BlockGroupTab"></a><a id="BlockManage"></a><a id="Blocks"></a><a id="BlocksSelector"></a><a id="BlockTab"></a>

### Management tabs

| Tab | Purpose |
| --- | --- |
| <a id="blocks"></a>Blocks | Browse blocks and open entries to edit them. |
| <a id="blocks.groups"></a>Block groups | Browse block groups. |

### Block editor

| Tab | Purpose |
| --- | --- |
| <a id="block"></a>General information | Edit the block information and settings. |
| <a id="block.groups"></a>Block groups | Choose the groups this entry belongs to. |

### Block group editor

| Tab | Purpose |
| --- | --- |
| <a id="block-group"></a>General information | Edit the group name and settings. |
| <a id="block-group.blocks"></a>Blocks | Choose the blocks associated with this group. |
| <a id="block-group.user-groups"></a>User groups | Choose the user groups associated with this group. |
| <a id="block-group.policies"></a>Policies (inside General information) | Configure access policies for the block group. |

## Menus

<a id="Menu"></a><a id="MenuGroup"></a><a id="MenuGroups"></a><a id="MenuGroupsSelector"></a><a id="MenuGroupTab"></a><a id="MenuManage"></a><a id="Menus"></a><a id="MenusSelector"></a><a id="MenuTab"></a><a id="Links"></a><a id="MenuGroupGeneral"></a><a id="MenuGeneral"></a>

### Management tabs

| Tab | Purpose |
| --- | --- |
| <a id="menus"></a>Menus | Browse menus and open entries to edit them. |
| <a id="menus.groups"></a>Menu groups | Browse menu groups. |

### Menu editor

| Tab | Purpose |
| --- | --- |
| <a id="menu"></a>General information | Edit the menu information and settings. |
| <a id="menu.groups"></a>Menu groups | Choose the groups this entry belongs to. |
| <a id="menu.policies"></a>Policies (inside General information) | Configure access policies for this entry. |

### Menu group editor

| Tab | Purpose |
| --- | --- |
| <a id="menu-group"></a>General information | Edit the group name and settings. |
| <a id="menu-group.menus"></a>Menus | Choose the menus associated with this group. |
| <a id="menu-group.user-groups"></a>User groups | Choose the user groups associated with this group. |

### Menu link editor

| Tab | Purpose |
| --- | --- |
| General information | Edit the link destination, label and display settings. |
| <a id="links.policies"></a>Policies | Configure access policies for the link destination, label and display settings. |

## Key management

<a id="KeyManagement"></a><a id="KeyManagementGroup"></a><a id="KeyManagementGroups"></a><a id="KeyManagementGroupsSelector"></a><a id="KeyManagementGroupTab"></a><a id="KeyManagementKey"></a><a id="KeyManagementKeys"></a><a id="KeyManagementLog"></a><a id="KeyManagementLogs"></a><a id="KeyManagementManage"></a><a id="KeyManagements"></a><a id="KeyManagementsSelector"></a><a id="KeyManagementTab"></a><a id="KeyManagementGroupGeneral"></a><a id="KeyManagementGeneral"></a>

### Management tabs

| Tab | Purpose |
| --- | --- |
| <a id="key-managements"></a>Key management | Browse key management and open entries to edit them. |
| <a id="key-managements.groups"></a>Key management groups | Browse key management groups. |
| <a id="key-managements.logs"></a>Key management logs | Review recorded key management logs. |

### Key management editor

| Tab | Purpose |
| --- | --- |
| <a id="key-management"></a>General information | Edit the key management information and settings. |
| <a id="key-management.groups"></a>Key management groups | Choose the groups this entry belongs to. |

### Key management group editor

| Tab | Purpose |
| --- | --- |
| <a id="key-management-group"></a>General information | Edit the group name and settings. |
| <a id="key-management-group.key-managements"></a>Key managements | Choose the key managements associated with this group. |
| <a id="key-management-group.user-groups"></a>User groups | Choose the user groups associated with this group. |

## Manufacturers

<a id="Manufacturer"></a><a id="ManufacturerManage"></a><a id="Manufacturers"></a><a id="ManufacturerTab"></a><a id="Models"></a><a id="ManufacturerGeneral"></a>

### Management tabs

| Tab | Purpose |
| --- | --- |
| <a id="manufacturers"></a>Manufacturers | Browse manufacturers and open entries to edit them. |

### Manufacturer editor

| Tab | Purpose |
| --- | --- |
| <a id="manufacturer"></a>General information | Edit the manufacturer information and settings. |
| <a id="manufacturer.profile-picture"></a>Profile picture | Choose the picture for this entry. |
| <a id="manufacturer.model-profile-picture"></a>Device model profile picture | Choose the picture for a device model. |

## Appearance

<a id="AppearanceManage"></a><a id="Colors"></a><a id="IconPackEditor"></a><a id="IconPackTab"></a><a id="ImageEditor"></a><a id="Images"></a><a id="ImageSelector"></a><a id="Texts"></a><a id="ThemeEditor"></a><a id="Themes"></a><a id="ThemeTab"></a><a id="IconPack"></a><a id="Theme"></a>

### Management tabs

| Tab | Purpose |
| --- | --- |
| <a id="appearance.icons"></a>Icons | Manage images used as application icons. |
| <a id="appearance.themes"></a>Theme | Manage application themes. |
| <a id="appearance.icon-packs"></a>Icon pack | Manage collections of icons. |

### Theme tabs

| Tab | Purpose |
| --- | --- |
| <a id="theme"></a>Theme / General information | Choose or edit a theme. |
| <a id="theme.color"></a>Color | Edit theme colors. |
| <a id="theme.text"></a>Text | Edit text appearance. |
| <a id="theme.policies"></a>Policies (inside General information) | Configure access policies for the theme. |

### Image editor

| Tab | Purpose |
| --- | --- |
| General information | Edit the image and its information. |
| <a id="image-editor.policies"></a>Policies | Configure access policies for the image and its information. |

## Notifications

<a id="Notification"></a><a id="Notifications"></a><a id="NotificationTab"></a><a id="NotificationGeneral"></a>

### Management tabs

| Tab | Purpose |
| --- | --- |
| <a id="notifications.types"></a>Notifications | Manage notification delivery types. |
| <a id="notifications.server-notifications"></a>Server notifications | Configure server notification delivery. |

### Notification editor

| Tab | Purpose |
| --- | --- |
| <a id="notification"></a>General information | Set the delivery type, name, enabled state and delivery options. |
| <a id="notification.notifications"></a>Notifications | Choose which events produce notifications. |

### Delivery type editor

| Tab | Purpose |
| --- | --- |
| <a id="notification-type"></a>General information | Edit the delivery type name and enabled state. |
| <a id="notification-type.policies"></a>Policies | Configure access policies for the delivery type. |

For toaster messages, enable a notification subscription and select its events. A message appears when a matching event occurs; saved selections refresh without a page reload.

## Logs and updates

<a id="ActiveSystemErrors"></a><a id="Logs"></a><a id="LogTypes"></a><a id="Performances"></a><a id="Statistic"></a><a id="SystemLog"></a><a id="Tasks"></a><a id="UpdateEntry"></a><a id="Updates"></a><a id="Versions"></a><a id="logs"></a>

### Log tabs

| Tab | Purpose |
| --- | --- |
| <a id="logs.system"></a>System logs | Review server messages and errors. |
| <a id="logs.tasks"></a>Tasks | Follow requested and automated work. |
| <a id="logs.schedules"></a>Schedule logs | Review schedule executions. |
| <a id="logs.modules"></a>Module logs | Review module messages. |
| <a id="logs.scripts"></a>Script logs | Review script executions. |
| <a id="logs.workflows"></a>Workflow logs | Review workflow executions. |
| <a id="logs.general"></a>General | Review general application logs. |
| <a id="logs.key-management"></a>Key management logs | Review key management operations. |
| <a id="logs.gateways"></a>Gateway logs | Review gateway communication messages. |
| <a id="logs.agents"></a>Agent logs | Review agent messages. |
| <a id="logs.devices"></a>Device logs | Review device communication messages. |
| <a id="logs.device-actions"></a>Device actions | Review device actions. |
| <a id="logs.device-traces"></a>Device traces | Inspect communication traces. |
| <a id="logs.change-history"></a>Change history | Review recorded changes to application data. |
| <a id="updates"></a>Available updates | Review available application or resource updates. |

### Update tabs

| Tab | Purpose |
| --- | --- |
| <a id="updates.settings"></a>Update settings | Configure update notifications. |

## Configuration

<a id="config"></a><a id="Cron"></a><a id="History"></a><a id="Language"></a><a id="LanguageConfig"></a><a id="Languages"></a><a id="Localization"></a><a id="LocalizationUpdates"></a><a id="Maintenance"></a><a id="Prune"></a><a id="PruneAction"></a><a id="Security"></a><a id="Settings"></a><a id="Status"></a><a id="SystemConfig"></a>

### Configuration tabs

| Tab | Purpose |
| --- | --- |
| Settings | Open the available configuration settings. |
| <a id="config.layout"></a>Configuration layout | Arrange the configuration menu entries. |

The Settings list is sorted by name. Configuration settings open their own forms or management areas. Administrative settings and commands require the appropriate permissions.

### Settings entries

| Tab | Purpose |
| --- | --- |
| <a id="config.system"></a>System | Configure system-wide settings. |
| <a id="config.status"></a>Status | Inspect the server status. |
| <a id="config.security"></a>Security | Configure security and password requirements. |
| <a id="config.modules"></a>Modules | Open module management. |
| <a id="config.agents"></a>Agents | Configure agent defaults. |
| <a id="config.components"></a>Components | Open component view management. |
| <a id="config.blocks"></a>Blocks | Open block management. |
| <a id="config.menus"></a>Menus | Open menu management. |
| <a id="config.tabs"></a>Tabs | Configure tab menus. |
| <a id="config.logs"></a>Logs | Configure logging and log types. |
| <a id="config.views"></a>Views | Open view management. |
| <a id="config.triggers"></a>Triggers | Open trigger management. |
| <a id="config.workflows"></a>Workflows | Open workflow management. |
| <a id="config.notifications"></a>Notification | Open notification settings. |
| <a id="config.prune"></a>Prune | Configure retention and cleanup limits. |
| <a id="config.appearance"></a>Appearance | Open appearance settings. |
| <a id="config.account"></a>Account | Choose who can register accounts. |
| <a id="config.manufacturers"></a>Manufacturers | Open manufacturer management. |
| <a id="config.cron"></a>Cron | Configure scheduled server processing. |
| <a id="config.key-management"></a>Key management | Open key management settings. |
| <a id="config.maintenance"></a>Maintenance | Configure a maintenance interval and message. |
| <a id="config.scripts"></a>Scripts | Open script management. |
| <a id="config.http-proxy"></a>HTTP proxy | Set allowed HTTP servers, caching and request limits. |
| <a id="config.history"></a>History | Configure stored history. |

### Language tabs

| Tab | Purpose |
| --- | --- |
| <a id="config.languages"></a>Language | Configure the available languages. |
| <a id="config.languages.localize"></a>Localize | Edit localized application resources. |

### Access control tabs

| Tab | Purpose |
| --- | --- |
| <a id="config.access-control.roles"></a>Roles | Manage roles and their permissions. |
| <a id="config.access-control.policies"></a>Policies | Manage access policies. |

### Log type editor

| Tab | Purpose |
| --- | --- |
| <a id="log-type"></a>General information | Edit the log type settings. |
| <a id="log-type.policies"></a>Policies | Configure the log type access policies. |

Prune retention defines how long each category is kept; retention 0 disables cleanup for that category. Maximum limits removals per category in one run; maximum 0 means no count limit. Save changes before choosing Run, because Run uses the saved settings.

During an enabled maintenance interval, users see the maintenance message and normal access is restricted. A missing start time means immediately; a missing end time means until disabled. Administrators retain access and can choose Go on line to end maintenance.

## Objects and target selection

<a id="AttributesSelector"></a><a id="AttributeTemplate"></a><a id="AttributeTemplates"></a><a id="AttributeTemplatesSelector"></a><a id="AttributeTile"></a><a id="ChargeUnit"></a><a id="ChargeView"></a><a id="ClockView"></a><a id="DataView"></a><a id="DisconnectControlView"></a><a id="ImageTransferView"></a><a id="ObjectEditor"></a><a id="ObjectList"></a><a id="Objects"></a><a id="ObjectTemplate"></a><a id="ObjectTemplates"></a><a id="ObjectTemplateSelector"></a><a id="ObjectTemplatesSelector"></a><a id="ObjectView"></a><a id="ParametersView"></a><a id="ProfileGenericView"></a><a id="PushSetupView"></a><a id="RegisterView"></a><a id="UnixTimeData"></a>

### Object editors

| Tab | Purpose |
| --- | --- |
| <a id="object"></a>General information | Inspect the object identity and settings. |
| <a id="object-template.default-values"></a>Default values | Set initial values for an object template. |
| <a id="object-template.generic"></a>Generic | Edit the object template using the generic editor. |
| <a id="charge"></a>General | Inspect charge settings. |
| <a id="charge.active-unit-charge"></a>Unit charge active | Inspect the active unit charge. |
| <a id="charge.passive-unit-charge"></a>Unit charge passive | Inspect the passive unit charge. |

Meter object pages show the values and operations supported by that COSEM object. Read, write and method commands depend on the object and your permissions.

### Target selector tabs

| Tab | Purpose |
| --- | --- |
| <a id="device-selection"></a>Device selection / Devices | Choose the target devices. |
| <a id="device-selection.objects"></a>Device selection / Objects | Choose the target COSEM objects. |
| <a id="device-selection.attributes"></a>Device selection / Attributes | Choose attributes from the selected objects. |
| <a id="device-group-selection"></a>Device group selection / Device groups | Choose the target device groups. |
| <a id="device-group-selection.objects"></a>Device group selection / Objects | Choose the target COSEM objects. |
| <a id="device-group-selection.attributes"></a>Device group selection / Attributes | Choose attributes from the selected objects. |
| Schedule device selection / Devices | Choose the target devices. |
| <a id="schedule.devices.objects"></a>Schedule device selection / Objects | Choose the target COSEM objects. |
| <a id="schedule.devices.attributes"></a>Schedule device selection / Attributes | Choose attributes from the selected objects. |
| Schedule device group selection / Device groups | Choose the target device groups. |
| <a id="schedule.device-groups.objects"></a>Schedule device group selection / Objects | Choose the target COSEM objects. |
| <a id="schedule.device-groups.attributes"></a>Schedule device group selection / Attributes | Choose attributes from the selected objects. |

A selector belongs to its containing editor: Devices or Device groups chooses targets for that entry, while Objects and Attributes refine the selection where offered. Some editors show only the selection levels they support.

## Other pages and controls

<a id="policy"></a><a id="policy.policies"></a>

Forms with General information and Policies tabs separate the entry settings from the rules controlling access to that entry.

<a id="ActionButton"></a><a id="Favorites"></a><a id="Index"></a>

Home shows the application landing page, Favorites opens saved shortcuts, and profile or history pages display recorded data where available. Buttons, dropdowns and selection dialogs act on the current page or form; return to that form and save it to apply configuration changes.
