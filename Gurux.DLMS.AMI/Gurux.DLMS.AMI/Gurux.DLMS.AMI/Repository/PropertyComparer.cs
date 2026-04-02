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
using Gurux.Service.Orm;
using Gurux.DLMS.AMI.Client.Shared;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Threading;
using Gurux.DLMS.AMI.Client.Shared.Enums;

namespace Gurux.DLMS.AMI.Server.Repository
{
    /// <summary>
    /// JsonProperty is used to sort JsonProperty.
    /// </summary>
    internal class PropertyComparer : IComparer<JsonProperty>
    {
        public int Compare(JsonProperty x, JsonProperty y)
        {
            int ret = 0;
            if (x.Name == y.Name)
            {
                ret = 0;
            }
            else if (x.Name == TargetType.DeviceTemplate)
            {
                ret = -1;
            }
            else if (y.Name == TargetType.DeviceTemplate)
            {
                ret = 1;
            }
            else if (x.Name == TargetType.Script)
            {
                ret = -1;
            }
            else if (y.Name == TargetType.Script)
            {
                ret = 1;
            }
            else if (x.Name == TargetType.ContentType)
            {
                ret = -1;
            }
            else if (y.Name == TargetType.ContentType)
            {
                ret = 1;
            }
            else if (x.Name == TargetType.Content)
            {
                ret = -1;
            }
            else if (y.Name == TargetType.Content)
            {
                ret = 1;
            }
            else if (x.Name == TargetType.Device)
            {
                ret = -1;
            }
            else if (y.Name == TargetType.Device)
            {
                ret = 1;
            }
            else
            {
                ret = 1;
            }
            return ret;           
        }
    }
}
