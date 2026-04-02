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

using Gurux.DLMS.AMI.Module;
using Gurux.DLMS.AMI.Module.Enums;
using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.DLMS.AMI.Shared.DTOs.Device;
using System.Reflection;

namespace Gurux.DLMS.AMI.Client
{
    /// <summary>
    /// Extended UI helper.
    /// </summary>
    public class AmiExtendedUIHelper
    {
        private static IAmiExtendedObjectUI[] GetExtendedUIs(object target,
            bool settings,
            out ExtendedlUIType extendedlUI)
        {
            extendedlUI = ExtendedlUIType.None;
            List<IAmiExtendedObjectUI> list = new List<IAmiExtendedObjectUI>();

            //Find module from loaded assemblies.
            foreach (Assembly asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    foreach (Type it in asm.GetExportedTypes())
                    {
                        if (it.IsAbstract || !it.IsClass || it.FullName == null)
                        {
                            continue;
                        }
                        if (typeof(IAmiExtendedUI).IsAssignableFrom(it))
                        {
                            if (settings)
                            {
                                if ((target is GXDeviceGroup && !typeof(IAmiExtendedDeviceGroupSettingsUI).IsAssignableFrom(it)) ||
                                    (target is GXDevice && !typeof(IAmiExtendedDeviceSettingsUI).IsAssignableFrom(it)) ||
                                    (target is GXObject && !typeof(IAmiExtendedObjectSettingsUI).IsAssignableFrom(it)) ||
                                    (target is GXAttribute && !typeof(IAmiExtendedAttributeSettingsUI).IsAssignableFrom(it)) ||
                                    (target is GXDeviceGroup && !typeof(IAmiExtendedDeviceGroupSettingsUI).IsAssignableFrom(it)))
                                {
                                    continue;
                                }
                            }
                            else
                            {
                                if ((target is GXDeviceGroup && !typeof(IAmiExtendedDeviceGroupUI).IsAssignableFrom(it)) ||
                                    (target is GXDevice && !typeof(IAmiExtendedDeviceUI).IsAssignableFrom(it)) ||
                                    (target is GXObject && !typeof(IAmiExtendedObjectUI).IsAssignableFrom(it)) ||
                                    (target is GXAttribute && !typeof(IAmiExtendedAttributeUI).IsAssignableFrom(it)) ||
                                    (target is GXDeviceGroup && !typeof(IAmiExtendedDeviceGroupUI).IsAssignableFrom(it)))
                                {
                                    continue;
                                }
                            }
                            //Create assembly and ask if it wants to handle this object.
                            IAmiExtendedObjectUI? ext = (IAmiExtendedObjectUI?)Activator.CreateInstance(it);
                            if (ext != null)
                            {
                                ExtendedlUIType type = ext.ExtendedUI(target);
                                if (type != ExtendedlUIType.None)
                                {
                                    list.Add(ext);
                                    //If one ui is apped, then the all are.
                                    if (extendedlUI == ExtendedlUIType.None)
                                    {
                                        extendedlUI = type;
                                    }
                                    else if (extendedlUI == ExtendedlUIType.Replace &&
                                        type == ExtendedlUIType.Append)
                                    {
                                        extendedlUI = ExtendedlUIType.Append;
                                    }
                                }
                            }
                        }
                    }
                }
                catch (Exception)
                {
                    //It's OK if this fails. Assembly is skipped.
                }
            }
            return list.ToArray();
        }
        public static IAmiExtendedObjectUI[] GetExtendedObjectUIs(GXObject target,
            bool settings,
            out ExtendedlUIType extendedlUI)
        {
            return GetExtendedUIs(target, settings, out extendedlUI);
        }
    }
}
