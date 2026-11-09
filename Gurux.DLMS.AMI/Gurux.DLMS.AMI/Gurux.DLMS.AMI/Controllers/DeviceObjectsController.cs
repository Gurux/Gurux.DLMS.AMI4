using Gurux.DLMS.AMI.Client.Rest;
using Gurux.DLMS.AMI.Client.Shared;
using Gurux.DLMS.AMI.Db;
using Gurux.DLMS.AMI.Shared.DIs;
using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.DLMS.AMI.Shared.DTOs.Device;
using Gurux.Service.Orm;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Gurux.DLMS.AMI.Server.Controllers;

/// <summary>Resolves object editors only within an authorized device.</summary>
[ApiController]
[Route("api/Device/{deviceId:guid}/Objects")]
[Authorize(Policy = GXObjectPolicies.View)]
public sealed class DeviceObjectsController(IDeviceRepository devices, IObjectRepository objects, IDbConnectionFactory database) : ControllerBase
{
    /// <summary>Returns an owned object, validating an optional attribute and preserving late-bound links.</summary>
    [HttpGet("{objectId:guid}")]
    [HttpGet("{objectId:guid}/Attributes/{attributeId:guid}")]
    public async Task<ActionResult<GXDeviceObjectResponse>> Get(Guid deviceId, Guid objectId, Guid? attributeId = null, CancellationToken cancellationToken = default)
    {
        // The repository performs the user's device-membership and view-policy checks.
        var device = await devices.ReadAsync(deviceId, null, cancellationToken);
        await using var connection = await database.GetConnectionAsync(cancellationToken);
        var query = GXSelectArgs.Select<GXObject>(o => o.Id, o => o.Id == objectId && o.Removed == null);
        query.Joins.AddInnerJoin<GXObject, GXDevice>(o => o.Device!, d => d.Id);
        query.Where.And<GXDevice>(d => d.Id == deviceId && d.Removed == null);
        var stored = await connection.SingleOrDefaultAsync<GXObject>(query, cancellationToken);
        GXObject model;
        if (stored != null)
        {
            model = await objects.ReadAsync(stored.Id, null, cancellationToken);
        }
        else
        {
            // Virtual IDs are template IDs. The template must belong to this device's actual model.
            var templates = GXSelectArgs.SelectAll<GXObjectTemplate>(t => t.Id == objectId && t.Removed == null);
            templates.Joins.AddInnerJoin<GXObjectTemplate, GXDeviceTemplate>(t => t.DeviceTemplate!, d => d.Id);
            templates.Joins.AddInnerJoin<GXDeviceTemplate, GXDevice>(t => t.Id, d => d.Template!);
            templates.Where.And<GXDeviceTemplate>(t => t.Removed == null);
            templates.Where.And<GXDevice>(d => d.Id == deviceId && d.Removed == null);
            var template = await connection.SingleOrDefaultAsync<GXObjectTemplate>(templates, cancellationToken);
            if (template == null)
            {
                return NotFound();
            }
            var instances = GXSelectArgs.Select<GXObject>(o => new { o.Id, o.Removed });
            instances.Joins.AddInnerJoin<GXObject, GXDevice>(o => o.Device!, d => d.Id);
            instances.Joins.AddInnerJoin<GXObject, GXObjectTemplate>(o => o.Template!, t => t.Id);
            instances.Where.And<GXDevice>(d => d.Id == deviceId);
            instances.Where.And<GXObjectTemplate>(t => t.Id == objectId);
            var instance = await connection.SingleOrDefaultAsync<GXObject>(instances, cancellationToken);
            if (instance != null)
            {
                if (instance.Removed != null)
                {
                    return NotFound();
                }
                model = await objects.ReadAsync(instance.Id, null, cancellationToken);
            }
            else
            {
                var attributes = GXSelectArgs.SelectAll<GXAttributeTemplate>(a => a.Removed == null);
                attributes.Joins.AddInnerJoin<GXAttributeTemplate, GXObjectTemplate>(a => a.ObjectTemplate!, t => t.Id);
                attributes.Where.And<GXObjectTemplate>(t => t.Id == template.Id);
                attributes.Columns.Add<GXAttributeListItem>();
                attributes.Joins.AddLeftJoin<GXAttributeTemplate, GXAttributeListItem>(a => a.Id, l => l.Template!);
                attributes.Columns.Exclude<GXAttributeTemplate>(a => a.ObjectTemplate!);
                attributes.Columns.Exclude<GXAttributeListItem>(l => l.Template!);
                model = new GXObject(template) { Id = template.Id, Latebind = true };
                model.Attributes = (await connection.SelectAsync<GXAttributeTemplate>(attributes, cancellationToken))
                    .Select(a => new GXAttribute(a) { Id = a.Id, Value = a.DefaultValue }).ToList();
            }
        }
        if (model.Template == null)
        {
            return NotFound();
        }
        var selected = attributeId == null ? null : model.Attributes?.SingleOrDefault(a => a.Id == attributeId ||
            objectId == model.Template.Id && a.Template?.Id == attributeId);
        if (attributeId != null && selected == null)
        {
            return NotFound();
        }
        model.Device = new GXDevice { Id = device.Id, Name = device.Name };
        return new GXDeviceObjectResponse { Item = model, AttributeId = selected?.Id };
    }
}