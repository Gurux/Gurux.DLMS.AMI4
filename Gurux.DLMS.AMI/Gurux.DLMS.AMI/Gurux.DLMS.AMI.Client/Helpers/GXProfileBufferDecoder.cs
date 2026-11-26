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

using System.Globalization;
using System.Xml.Linq;
using Gurux.DLMS.Enums;
using Task = System.Threading.Tasks.Task;
using Gurux.DLMS.AMI.Http;
using Gurux.DLMS.Objects;
using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.DLMS.AMI.Shared.DTOs.Device;
using Gurux.DLMS.AMI.Shared.Rest;

namespace Gurux.DLMS.AMI.Client.Helpers;

/// <summary>Builds isolated display profiles and lets the DLMS library decode and scale raw stored buffer values.</summary>
public static class GXProfileBufferDecoder
{
    /// <summary>Copies capture metadata into an empty profile without changing the source profile or its buffer.</summary>
    /// <param name="source">The profile whose capture layout and settings are copied.</param>
    /// <returns>An empty profile containing independent capture objects.</returns>
    /// <exception cref="ArgumentNullException">The source profile is null.</exception>
    public static GXDLMSProfileGeneric CreateProfile(GXDLMSProfileGeneric source)
    {
        ArgumentNullException.ThrowIfNull(source);
        var profile = new GXDLMSProfileGeneric(source.LogicalName)
        {
            CapturePeriod = source.CapturePeriod,
            SortMethod = source.SortMethod
        };
        foreach (var capture in source.CaptureObjects)
        {
            profile.CaptureObjects.Add(new(CopyCapture(capture.Key),
                new GXDLMSCaptureObject(capture.Value.AttributeIndex, capture.Value.DataIndex)));
        }
        return profile;
    }

    /// <summary>Copies the metadata required for profile parsing without cloning a captured object's live values.</summary>
    /// <param name="source">The captured object whose type metadata and scaler/unit descriptor are copied.</param>
    /// <returns>An independent object suitable for the display profile.</returns>
    private static GXDLMSObject CopyCapture(GXDLMSObject source)
    {
        var target = GXDLMSClient.CreateObject(source.ObjectType);
        target.LogicalName = source.LogicalName;
        target.Version = source.Version;
        target.Description = source.Description;
        for (int index = 1; index <= ((IGXDLMSBase)source).GetAttributeCount(); ++index)
        {
            target.SetDataType(index, source.GetDataType(index));
            target.SetUIDataType(index, source.GetUIDataType(index));
        }
        if (source is GXDLMSRegister register && target is GXDLMSRegister copiedRegister)
        {
            copiedRegister.Scaler = register.Scaler;
            copiedRegister.Unit = register.Unit;
        }
        if (source is GXDLMSDemandRegister demand && target is GXDLMSDemandRegister copiedDemand)
        {
            copiedDemand.Scaler = demand.Scaler;
            copiedDemand.Unit = demand.Unit;
        }
        return target;
    }

    /// <summary>Loads persisted scaler/unit descriptors for the captured registers of one device.</summary>
    /// <param name="http">The authorized application HTTP client.</param>
    /// <param name="profile">An isolated display profile whose capture metadata is updated.</param>
    /// <param name="deviceId">The owning device, or an empty identifier when no device context is available.</param>
    /// <param name="token">Cancels metadata requests when the history request is superseded.</param>
    /// <returns>A task completing when the captured register scaler/unit descriptors have been loaded.</returns>
    /// <exception cref="InvalidOperationException">A captured register or its saved scaler/unit descriptor is unavailable.</exception>
    public static async Task LoadScalersAsync(HttpClient http, GXDLMSProfileGeneric profile, Guid deviceId, CancellationToken token)
    {
        if (deviceId == Guid.Empty)
        {
            return;
        }
        var client = new GXDLMSClient();
        foreach (var group in profile.CaptureObjects
            .Where(capture => capture.Key is GXDLMSRegister or GXDLMSDemandRegister)
            .GroupBy(capture => (capture.Key.ObjectType, capture.Key.LogicalName)))
        {
            var target = group.First().Key;
            var listed = await http.PostAsJson<ListObjectsResponse>("api/Object/List", new ListObjects
            {
                Count = 1,
                Filter = new GXObject
                {
                    Device = new GXDevice { Id = deviceId },
                    Template = new GXObjectTemplate { LogicalName = target.LogicalName, ObjectType = (int)target.ObjectType }
                }
            }, token);
            var item = listed.Objects?.FirstOrDefault();
            if (item == null)
            {
                throw ScalerUnavailable(target);
            }
            var details = (await http.GetAsJsonAsync<GetObjectResponse>($"api/Object?id={item.Id}", token))?.Item;
            int index = target is GXDLMSDemandRegister ? 4 : 3;
            var attribute = details?.Attributes?.FirstOrDefault(value => value.Template?.Index == index);
            if (string.IsNullOrWhiteSpace(attribute?.Value))
            {
                throw ScalerUnavailable(target);
            }
            foreach (var capture in group)
            {
                client.UpdateValue(capture.Key, index, ReadXml(attribute.Value, false));
            }
        }
    }

    /// <summary>Creates a localized error instead of presenting unscaled values as scaled measurements.</summary>
    /// <param name="target">The captured register whose scaler/unit descriptor is unavailable.</param>
    /// <returns>An exception identifying the missing register metadata.</returns>
    private static InvalidOperationException ScalerUnavailable(GXDLMSObject target)
    {
        string message = Properties.Resources.ResourceManager.GetString(
            "ProfileScalerUnavailable", CultureInfo.CurrentUICulture)!;
        return new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, message, target.LogicalName));
    }

    /// <summary>Decodes raw DLMS XML through protocol bytes and applies the capture metadata using the library's profile parser.</summary>
    /// <param name="source">The capture layout, including register scaler/unit descriptors.</param>
    /// <param name="xml">A raw stored row structure or complete buffer array in translator XML.</param>
    /// <param name="useUtc2NormalTime">Whether the meter uses the reversed DLMS timezone deviation convention.</param>
    /// <returns>An independent profile containing the decoded and scaled buffer.</returns>
    public static GXDLMSProfileGeneric Decode(GXDLMSProfileGeneric source, string xml, bool useUtc2NormalTime = false)
    {
        var profile = CreateProfile(source);
        object value = ReadXml(xml, useUtc2NormalTime);
        if (value is GXStructure row)
        {
            value = new GXArray { row };
        }
        var client = new GXDLMSClient { UseUtc2NormalTime = useUtc2NormalTime };
        client.UpdateValue(profile, 2, value);
        return profile;
    }

    /// <summary>Converts translator XML to protocol bytes and decodes their typed DLMS value.</summary>
    /// <param name="xml">The translator XML containing one DLMS value.</param>
    /// <param name="useUtc2NormalTime">Whether to reverse the timezone deviation convention.</param>
    /// <returns>The typed wire value decoded from the generated protocol bytes.</returns>
    private static object ReadXml(string xml, bool useUtc2NormalTime)
    {
        var translator = new GXDLMSTranslator(TranslatorOutputType.SimpleXml) { Hex = false };
        // Stored ValueToXml uses decimal floats; the protocol XML translator
        // expects IEEE-754 hex for Float32/Float64 Value attributes.
        var document = XElement.Parse(xml);
        foreach (var element in document.DescendantsAndSelf())
        {
            var attribute = element.Attribute("Value");
            if (attribute == null)
            {
                continue;
            }
            var bytes = new GXByteBuffer();
            if (element.Name.LocalName == "Float32" &&
                float.TryParse(attribute.Value.Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out var single))
            {
                bytes.SetFloat(single);
                attribute.Value = Convert.ToHexString(bytes.Array());
            }
            else if (element.Name.LocalName == "Float64" &&
                double.TryParse(attribute.Value.Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out var number))
            {
                bytes.SetDouble(number);
                attribute.Value = Convert.ToHexString(bytes.Array());
            }
        }
        byte[] data = translator.XmlToData(document.ToString(SaveOptions.DisableFormatting));
        return GXDLMSClient.GetValue(new GXByteBuffer(data), useUtc2NormalTime);
    }
}
