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

using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.DLMS.AMI.Shared.Enums;
using Gurux.DLMS.Enums;
using Gurux.DLMS.Objects;
using Gurux.DLMS.Secure;
using Gurux.DLMS.UI.Components.Objects;
using System.Globalization;

namespace Gurux.DLMS.AMI.Client.Helpers;

/// <summary>Adapts persisted AMI objects to the COSEM editors and metadata based tasks.</summary>
public sealed class GXObjectCosem
{
    private GXObject _model;
    public GXDLMSSecureClient Client { get; } = new() { InterfaceType = InterfaceType.PDU, UseLogicalNameReferencing = true };
    public GXDLMSObject Target { get; }

    public GXObjectCosem(GXObject model)
    {
        _model = model ?? throw new ArgumentNullException(nameof(model));
        var template = model.Template ?? throw new ArgumentException("The object template is missing.", nameof(model));
        Target = GXDLMSClient.CreateObject((ObjectType)(template.ObjectType ?? 0));
        Target.Version = (byte)(template.Version ?? 0);
        Target.LogicalName = template.LogicalName;
        Target.Description = template.Name;
        Target.ShortName = template.ShortName ?? 0;
        if (Target is IGXDLMSBase details)
        {
            for (var index = 1; index <= details.GetAttributeCount(); ++index) {
                Target.SetAccess(index, AccessMode.NoAccess);
            }
            for (var index = 1; index <= details.GetMethodCount(); ++index) {
                Target.SetMethodAccess(index, MethodAccessMode.NoAccess);
            }
        }
        Client.Ciphering.Security = Security.None;
        Client.Objects.Add(Target);
        Refresh(model);
    }

    /// <summary>Refresh confirmed values without replacing pending editor changes.</summary>
    public void Refresh(GXObject latest)
    {
        _model = latest ?? throw new ArgumentNullException(nameof(latest));
        // The agent stores register readings after applying the scaler. Load it
        // first so initial and subsequent refreshes interpret the same UI value.
        var scalerIndex = Target is GXDLMSRegister ? 3 : Target is GXDLMSDemandRegister ? 4 : 0;
        foreach (var attribute in (latest.Attributes ?? []).OrderBy(attribute => attribute.Template?.Index == scalerIndex ? 0 : 1))
        {
            var template = attribute.Template;
            if (template is null) {
                continue;
            }
            var index = template.Index;
            if (template.DataType != 0) {
                Target.SetDataType(index, (DataType)template.DataType);
            }
            if (template.UIDataType != 0) {
                Target.SetUIDataType(index, (DataType)template.UIDataType);
            }
            Target.SetAccess(index, Target is GXDLMSProfileGeneric && index == 2
                ? AccessMode.Read : (AccessMode)template.AccessLevel);
            if (Target.GetDirty(index, out _) || Target is GXDLMSProfileGeneric && index == 2) {
                continue;
            }
            var stored = attribute.Value ?? attribute.Values?.OrderByDescending(value => value.Read).FirstOrDefault()?.Value;
            var type = ValueType(index);
            if (stored != null && (!string.IsNullOrWhiteSpace(stored) || type is DataType.String or DataType.StringUTF8 or DataType.OctetString))
            {
                var value = DecodeValue(index, stored);
                GXDLMSObjectEditState.For(Target).SetConfirmedAttribute(index, value);
                if (!IsXmlValue(stored) && Target is GXDLMSRegister register && index == 2)
                    {
                    register.Value = value;
                }
                else if (!IsXmlValue(stored) && Target is GXDLMSDemandRegister demand && index is 2 or 3)
                {
                    if (index == 2) {
                        demand.CurrentAverageValue = value;
                    }
                    else {
                        demand.LastAverageValue = value;
                    }
                }
                else {
                    Client.UpdateValue(Target, index, value);
                }
            }
        }
        var access = latest.Template?.ActionAccessLevels;
        if (access is not null)
        {
            var hex = access.StartsWith("0x", StringComparison.OrdinalIgnoreCase);
            var values = hex ? GXDLMSTranslator.HexToBytes(access[2..]) : access.Select(value => byte.Parse(value.ToString())).ToArray();
            for (var pos = 0; pos < values.Length; ++pos) {
                Target.SetMethodAccess(pos + 1, (MethodAccessMode)values[pos]);
            }
        }
    }

    /// <summary>Decodes the agent's scalar text or typed XML using the attribute metadata.</summary>
    public object? DecodeValue(int index, string stored)
    {
        ArgumentNullException.ThrowIfNull(stored);
        if (IsXmlValue(stored)) {
            return GXDLMSTranslator.XmlToValue(stored);
        }
        if (index == 1 && stored.Split('.').Length == 6) {
            return stored;
        }
        var type = ValueType(index);
        if (type is DataType.Array or DataType.Structure)
            {
            throw new FormatException($"Attribute {index} requires a typed XML {type} value.");
        }
        // The agent can persist a CLR enum's name (for example SortMethod.FiFo).
        // Resolve it using this attribute's actual enum before decoding the wire byte.
        if (type == DataType.Enum && Target.GetValues().ElementAtOrDefault(index - 1) is Enum current &&
            Enum.TryParse(current.GetType(), stored, ignoreCase: true, out var named))
            {
            stored = Convert.ToByte(named, CultureInfo.InvariantCulture).ToString(CultureInfo.InvariantCulture);
        }
        // Earlier agents formatted ordinary numbers using their process culture.
        // Convert.ToString does not add grouping, so a comma is a decimal separator.
        if (type is DataType.Float32 or DataType.Float64) {
            stored = stored.Replace(',', '.');
        }
        return type == DataType.None ? stored : GXDLMSConverter.ChangeType(stored, type, CultureInfo.InvariantCulture);
    }

    private DataType ValueType(int index)
    {
        var ui = Target.GetUIDataType(index);
        var type = ui != DataType.None ? ui : Target.GetDataType(index);
        var scaler = Target is GXDLMSRegister register && index == 2 ? register.Scaler
            : Target is GXDLMSDemandRegister demand && index is 2 or 3 ? demand.Scaler : 1;
        return scaler != 1 && type is DataType.Int8 or DataType.UInt8 or DataType.Int16 or DataType.UInt16
            or DataType.Int32 or DataType.UInt32 or DataType.Int64 or DataType.UInt64 or DataType.Float32 or DataType.Float64
            ? DataType.Float64 : type;
    }

    private static bool IsXmlValue(string stored)
    {
        var text = stored.AsSpan().TrimStart();
        if (!text.StartsWith("<")) {
            return false;
        }
        var end = text[1..].IndexOfAny(" \t\r\n/>".AsSpan());
        // Literal text can contain XML characters; only DLMS value tags encode values.
        return end >= 0 && Enum.TryParse<DataType>(text.Slice(1, end), true, out var type) && Enum.IsDefined(type);
    }

    public IReadOnlyList<GXTask> Read(IEnumerable<int> indexes) => Batch(indexes.Distinct().Select(index => Create(TaskType.Read, index)));

    public IReadOnlyList<GXTask> Write(IEnumerable<int> indexes) => Batch(indexes.Distinct()
        .Where(index => { Create(TaskType.Write, index); return Target.GetDirty(index, out _); }).Select(index =>
        {
            Target.GetDirty(index, out var value);
            return WriteTask(index, value);
        }));

    public IReadOnlyList<GXTask> Write(int index, object value) => Batch([WriteTask(index, value)]);

    private GXTask WriteTask(int index, object value)
    {
        if (Target is GXDLMSProfileGeneric && index == 2) {
            throw new InvalidOperationException("Profile history is read only.");
        }
        var task = Create(TaskType.Write, index);
        if (Target.GetAccess(index) is not (AccessMode.Write or AccessMode.ReadWrite or AccessMode.AuthenticatedWrite or AccessMode.AuthenticatedReadWrite))
            {
            throw new InvalidOperationException("The attribute is not writable.");
        }
        task.Data = GXDLMSTranslator.ValueToXml(value);
        return task;
    }

    public IReadOnlyList<GXTask> Action(GXDLMSActionRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (!ReferenceEquals(request.Target, Target)) {
            throw new ArgumentException("The action belongs to another object.", nameof(request));
        }
        if (request.MethodIndex < 1 || Target.GetMethodAccess(request.MethodIndex) == MethodAccessMode.NoAccess)
            {
            throw new InvalidOperationException("The method is not accessible.");
        }
        if (request.MethodIndex < 1 || request.MethodIndex > ((IGXDLMSBase)Target).GetMethodCount())
            {
            throw new ArgumentOutOfRangeException(nameof(request));
        }
        var messages = request.CreateMessages(Client);
        if (messages.Length == 0) {
            throw new InvalidOperationException("The method generated no requests.");
        }
        var tasks = new List<GXTask>();
        var logicalName = Target.LogicalName.Split('.').Select(byte.Parse).ToArray();
        foreach (var pdu in messages)
        {
        // A callback may generate several independent method calls, such as image blocks.
        // Protocol fragments cannot be represented as individual metadata-based tasks.
        if (pdu.Length < 13 || pdu[0] != 0xC3 || pdu[1] != 1
            || ((pdu[3] << 8) | pdu[4]) != (int)Target.ObjectType
            || !pdu.AsSpan(5, 6).SequenceEqual(logicalName) || pdu[11] != request.MethodIndex)
            {
            throw new InvalidOperationException("The generated method request does not match the selected object and method.");
        }
        var task = Create(TaskType.Action, request.MethodIndex);
        if (pdu[12] != 0)
        {
            new GXDLMSTranslator(TranslatorOutputType.SimpleXml) { Hex = false }.DataToXml(pdu[13..], out var xml);
            task.Data = GXDLMSTranslator.ValueToXml(GXDLMSTranslator.XmlToValue(xml));
        }
        else {
            task.Data = GXDLMSTranslator.ValueToXml(null);
        }
        tasks.Add(task);
        }
        return Batch(tasks);
    }

    private GXTask Create(TaskType type, int index)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(index, 1);
        var objectId = _model.Latebind ? _model.Template!.Id : _model.Id;
        var task = new GXTask { TaskType = type, Index = index,
            Object = type == TaskType.Action ? new GXObject { Id = objectId } : null,
            TargetDevice = _model.Latebind ? _model.Device?.Id : null };
        if (type != TaskType.Action)
        {
            var attribute = _model.Attributes?.SingleOrDefault(value => value.Template?.Index == index)
                ?? throw new ArgumentOutOfRangeException(nameof(index), "The attribute is missing.");
            task.Attribute = new GXAttribute { Id = attribute.Id, Object = new GXObject { Id = objectId } };
            if (type == TaskType.Read && Target.GetAccess(index) is not (AccessMode.Read or AccessMode.ReadWrite or AccessMode.AuthenticatedRead or AccessMode.AuthenticatedReadWrite))
                {
                throw new InvalidOperationException("The attribute is not readable.");
            }
        }
        return task;
    }

    private static IReadOnlyList<GXTask> Batch(IEnumerable<GXTask> source)
    {
        var tasks = source.ToArray();
        Guid? batch = tasks.Length > 1 ? Guid.NewGuid() : null;
        for (var pos = 0; pos < tasks.Length; ++pos) { tasks[pos].Order = pos + 1; tasks[pos].Batch = batch; }
        return tasks;
    }
}
