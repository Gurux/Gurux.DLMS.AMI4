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
using Gurux.Common;
using Gurux.DLMS.Enums;
using Gurux.DLMS.Objects;
using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.DLMS.AMI.Shared.Rest;
using System.Text;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System;
using System.Globalization;

namespace Gurux.DLMS.AMI.Agent.Worker
{
    class Reader
    {
        private static int GetAttributeIndex(GXObject obj, int index)
        {
            int index2 = 0;
            foreach (GXAttribute it in obj.Attributes)
            {
                if (it.Template.Index == index)
                {
                    return index2;
                }
                ++index2;
            }
            return -1;
        }

        private static int GetBufferIndex(GXObject obj)
        {
            return GetAttributeIndex(obj, 2);
        }

        /// <summary>
        /// Get capture period of the profile generic.
        /// </summary>
        private static int GetCapturePeriod(GXObject obj)
        {
            int index = GetAttributeIndex(obj, 4);
            if (index == -1)
            {
                return -1;
            }
            return Convert.ToInt32(obj.Attributes[index].Value);
        }

        /// <summary>
        /// Get entries in use from the profile generic.
        /// </summary>
        private static int GetEntriesInUse(GXObject obj)
        {
            int index = GetAttributeIndex(obj, 7);
            if (index == -1)
            {
                return 1;
            }
            return Convert.ToInt32(obj.Attributes[index].Value);
        }


        /// <summary>
        /// Get sort method for the profile generic.
        /// </summary>
        private static int GetSortMethod(GXObject obj)
        {
            int index = GetAttributeIndex(obj, 5);
            if (index == -1)
            {
                return 1;
            }
            return Convert.ToInt32(obj.Attributes[index].Value);
        }


        /// <summary>
        /// Get total entries from the profile generic.
        /// </summary>
        private static int GetTotalEntries(GXObject obj)
        {
            int index = GetAttributeIndex(obj, 8);
            if (index == -1)
            {
                return 1;
            }
            return Convert.ToInt32(obj.Attributes[index].Value);
        }


        /// <summary>
        /// Get profile generic buffer last read time.
        /// </summary>
        private static DateTimeOffset? GetBufferReadTime(GXObject obj)
        {
            int index = GetAttributeIndex(obj, 2);
            if (index == -1)
            {
                return null;
            }
            return obj.Attributes[index].Read;
        }

        /// <summary>
        /// Set profile generic buffer last read time.
        /// </summary>
        private static void SetReadTime(GXObject obj, DateTime value)
        {
            int index = GetAttributeIndex(obj, 2);
            if (index == -1)
            {
                return;
            }
            obj.Attributes[index].Read = value;
        }

        /// <summary>
        /// Is profile generic object sorted using date time.
        /// </summary>
        private static bool IsSortedByDateTime(GXObject obj)
        {
            int index = GetAttributeIndex(obj, 3);
            if (index == -1)
            {
                return false;
            }
            try
            {
                GXArray? tmp = GXDLMSTranslator.XmlToValue(obj.Attributes[index].Value) as GXArray;
                if (tmp != null)
                {
                    //If interface type is clock.
                    return (UInt16)((GXStructure)tmp[0])[0] == 8;
                }
            }
            catch (Exception)
            {
                //It's OK if this fails.
            }
            return false;
        }

        internal static async System.Threading.Tasks.Task Read(ILogger? logger, GXDLMSReader reader, GXTask task, GXDLMSObject obj)
        {
            bool reaNextDay;
            AddValue v;
            logger?.LogInformation("Reading: " + obj.ToString());
            object? val;
            DateTime end = DateTime.MaxValue;
            DateTime lastEnd = DateTime.MaxValue;
            do
            {
                reaNextDay = false;
                if (task.Object != null && obj.ObjectType == ObjectType.ProfileGeneric && task.Index == 2)
                {
                    //Add date time object so GXDLMSClient is not thrown an Capture objects not read-exception.
                    GXKeyValuePair<GXDLMSObject, GXDLMSCaptureObject> p = new GXKeyValuePair<GXDLMSObject, GXDLMSCaptureObject>(new GXDLMSClock(), new GXDLMSCaptureObject(2, 0));
                    ((GXDLMSProfileGeneric)obj).CaptureObjects.Add(p);
                    var start = GetBufferReadTime(task.Object);
                    if (GetEntriesInUse(task.Object) > 1 && IsSortedByDateTime(task.Object))
                    {
                        if (start != null)
                        {
                            int period = GetCapturePeriod(task.Object);
                            if (period == -1)
                            {
                                throw new Exception("Invalid capture period.");
                            }
                            //For event logs period is zero.
                            if (period == 0 && lastEnd != DateTime.MaxValue)
                            {
                                start = lastEnd;
                            }
                            else
                            {
                                start = start.Value.AddSeconds(period);
                            }
                            //Read profile generic using range.
                            end = DateTime.Now;
                            end = end.AddSeconds(-end.Second);
                            end = end.AddMinutes(-end.Minute);
                            end = end.AddHours(1);
                            if (end - start > new TimeSpan(24, 0, 0))
                            {
                                //Max 24 hours is read at once.
                                end = start.Value.DateTime.AddDays(1);
                            }
                            //Read empty days until present time.
                            do
                            {
                                logger?.LogError($"Reading {obj} {start} - {end}", obj, start, end);
                                val = reader.ReadRowsByRange((GXDLMSProfileGeneric)obj, start.Value.DateTime, end);
                                if (val != null)
                                {
                                    if ((val is GXArray arr) && arr.Count == 1 && (arr[0] is GXStructure s) && s.Any() && s[0] is byte[] ba)
                                    {
                                        GXDateTime? val2 = reader.Client.ChangeType(new GXByteBuffer(ba), DataType.DateTime) as GXDateTime;
                                        //If there is only one row in the given time period and it's returned.
                                        if (val2 == null || val2.Compare(start.Value.DateTime) != 0)
                                        {
                                            break;
                                        }
                                    }
                                    else
                                    {
                                        break;
                                    }
                                }
                                else
                                {
                                    //Save last read start time. 
                                    //This is needed if the last row was added a long time ago.
                                    //There is no need to read values from long time of period if there aren't any values.
                                    //This is done for every read because connection might drop and don't want to re-start reading.
                                    v = new AddValue()
                                    {
                                        Values = new GXValue[] {
                                            new GXValue()
                                            {
                                                Attribute = new GXAttribute()
                                                {
                                                    Id = task.Object.Attributes[GetAttributeIndex(task.Object, 2)].Id
                                                },
                                                Read = start.Value.DateTime,
                                            }
                                        }
                                    };
                                    await GXAgentWorker.client.PostAsJson("/api/value/Add", v);
                                }
                                start = end;
                                end = end.AddDays(1);
                            }
                            while (end < DateTime.Now);
                            if (end < DateTime.Now)
                            {
                                //Read the next day.
                                reaNextDay = true;
                            }
                        }
                        else
                        {
                            //Read for the first time.
                            //Try to read the first item.
                            try
                            {
                                logger?.LogError($"Reading {obj} the first row.", obj);
                                int sm = GetSortMethod(task.Object);
                                //If FIFO
                                if (sm == 1)
                                {
                                    UInt32 cnt = (UInt32) (GetEntriesInUse(task.Object));
                                    val = null;
                                    while (cnt != 0)
                                    {
                                        //Read the last row. Some meters might return invalid count. 
                                        //For that reason previous row is try to read.
                                        val = reader.ReadRowsByEntry((GXDLMSProfileGeneric)obj, cnt, 1);
                                        if (val != null)
                                        {
                                            break;
                                        }
                                        --cnt;
                                    }
                                }
                                else
                                {
                                    val = reader.ReadRowsByEntry((GXDLMSProfileGeneric)obj, 1, 1);
                                }

                                //Read the next day.
                                reaNextDay = true;
                            }
                            catch (Exception)
                            {
                                //If meter doesn't support read by entry.
                                //Read the next day.
                                reaNextDay = true;
                                continue;
                            }
                        }
                    }
                    else
                    {
                        //If only one row.
                        logger?.LogError($"Reading {obj} all rows.", obj);
                        val = await reader.Read(obj, task.Index.GetValueOrDefault(0));
                    }
                }
                else
                {
                    val = await reader.Read(obj, task.Index.GetValueOrDefault(0));
                }
                if (val is Enum)
                {
                    //Enum values are saved as interger.
                    val = Convert.ToString(Convert.ToInt32(val));
                }
                else if (val is byte[])
                {
                    DataType dt = (DataType)task.Object.Attributes[GetAttributeIndex(task.Object, task.Index.GetValueOrDefault(0))].Template.UIDataType;
                    if (dt == DataType.String)
                    {
                        val = ASCIIEncoding.ASCII.GetString((byte[])val);
                    }
                    else if (dt == DataType.StringUTF8)
                    {
                        val = ASCIIEncoding.UTF8.GetString((byte[])val);
                    }
                    else
                    {
                        val = GXCommon.ToHex((byte[])val);
                    }
                }
                else if (val is GXDate d)
                {
                    //Date is always saved using invariant culture.
                    val = d.ToFormatString(CultureInfo.InvariantCulture);
                }
                else if (val is GXTime t)
                {
                    //Time is always saved using invariant culture.
                    val = t.ToFormatString(CultureInfo.InvariantCulture);
                }
                else if (val is GXDateTime dt)
                {
                    //Date time is always saved using invariant culture.
                    val = dt.ToFormatString(CultureInfo.InvariantCulture);
                }
                else if (val is string str)
                {
                    val = str.Replace("\0", "");
                }
                if (obj.ObjectType == ObjectType.ProfileGeneric && task.Index == 2)
                {
                    //Make own value from each row.
                    if (val != null)
                    {
                        GXAttribute attribute = task.Object.Attributes[GetBufferIndex(task.Object)];
                        List<GXValue> list = new List<GXValue>();
                        int index = GetAttributeIndex(task.Object, task.Index.GetValueOrDefault(0));
                        DateTime latest = task.Object.Attributes[index].Read.GetValueOrDefault().DateTime;
                        DateTime first = latest;
                        int period = -1;
                        foreach (GXStructure row in (GXArray)val)
                        {
                            bool read = false;
                            DateTime dt = DateTime.MinValue;
                            task.Data = GXDLMSTranslator.ValueToXml(row);
                            for (int pos = 0; pos != row.Count; ++pos)
                            {
                                if (row[pos] is byte[])
                                {
                                    row[pos] = GXDLMSClient.ChangeType((byte[])row[pos], DataType.DateTime);
                                    if (pos == 0)
                                    {
                                        dt = ((GXDateTime)row[pos]).Value.LocalDateTime;
                                        //If we have already read this row.
                                        if (dt <= first)
                                        {
                                            read = true;
                                            break;
                                        }
                                        if (dt > latest)
                                        {
                                            SetReadTime(task.Object, dt);
                                            latest = dt;
                                        }
                                    }
                                }
                                //Some meters are returning null as date time to save bytes...
                                if (pos == 0 && row[pos] == null)
                                {
                                    if (period == -1)
                                    {
                                        period = GetCapturePeriod(task.Object);
                                        if (period == -1)
                                        {
                                            throw new Exception("Invalid capture period.");
                                        }
                                    }
                                    row[pos] = latest.AddMinutes(period);
                                }
                            }
                            logger?.LogInformation("Read: " + task.Data);
                            if (!read)
                            {
                                list.Add(new GXValue()
                                {
                                    Read = dt,
                                    Value = task.Data,
                                    Attribute = new GXAttribute() { Id = attribute.Id }
                                });
                            }
                        }
                        if (list.Count != 0)
                        {
                            v = new AddValue() { Values = list.ToArray() };
                            await GXAgentWorker.client.PostAsJson("/api/value/Add", v);
                            reaNextDay = true;
                        }
                        else if (end != DateTime.MaxValue)
                        {
                            //Set read start time to end time if there hasn't been any new rows in the given period.
                            lastEnd = end;
                        }
                    }
                }
                else
                {
                    if (!(val is string))
                    {
                        val = Convert.ToString(val);
                    }
                    task.Data = (string?)val;
                    logger?.LogInformation("Read: " + val);
                    int index = GetAttributeIndex(task.Object, task.Index.GetValueOrDefault(0));
                    if (index != -1)
                    {
                        v = new AddValue()
                        {
                            Values = new GXValue[] {
                                new GXValue()
                                {
                                    Attribute = new GXAttribute()
                                    {
                                        Id = task.Object.Attributes[index].Id
                                    },
                                    Read = DateTime.Now,
                                    Value = (string?)val
                                }
                            }
                        };
                        await GXAgentWorker.client.PostAsJson("/api/value/Add", v);
                    }
                    else
                    {
                        logger?.LogInformation("Invalid task index: " + task.Index);
                    }
                }
            }
            while (reaNextDay);
        }
    }
}
