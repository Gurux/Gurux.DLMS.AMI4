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

using Gurux.Common;
using Gurux.DLMS.Enums;
using Gurux.DLMS.Objects;
using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.DLMS.AMI.Shared.Rest;
using System.Text;
using Microsoft.Extensions.Logging;
using System.Globalization;

namespace Gurux.DLMS.AMI.Agent.Worker
{
    class Reader
    {
        private static int GetAttributeIndex(GXObject? obj, int index)
        {
            if (obj?.Attributes == null)
            {
                return -1;
            }
            int index2 = 0;
            foreach (GXAttribute it in obj.Attributes)
            {
                if (it.Template?.Index == index)
                {
                    return index2;
                }
                ++index2;
            }
            return -1;
        }

        private static GXAttribute GetRequiredAttribute(GXObject obj, int index)
        {
            var attributes = obj.Attributes
                ?? throw new ArgumentException("Object attributes are missing.");
            int position = GetAttributeIndex(obj, index);
            if (position == -1)
            {
                throw new ArgumentException($"Object attribute {index} is missing.");
            }
            return attributes[position];
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
            return Convert.ToInt32(obj.Attributes?[index].Value);
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
            return Convert.ToInt32(obj.Attributes?[index].Value);
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
            return Convert.ToInt32(obj.Attributes?[index].Value);
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
            return Convert.ToInt32(obj.Attributes?[index].Value);
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
            return obj.Attributes?[index].Read;
        }

        /// <summary>
        /// Set profile generic buffer last read time.
        /// </summary>
        private static void SetReadTime(GXObject obj, DateTime value)
        {
            int index = GetAttributeIndex(obj, 2);
            if (index == -1 || obj.Attributes == null)
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
                if (GXDLMSTranslator.XmlToValue(obj.Attributes?[index].Value) is GXArray tmp)
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

        internal static async System.Threading.Tasks.Task Read(HttpClient http, ILogger? logger, GXDLMSReader reader, GXTask task, GXDLMSObject obj, CancellationToken token = default)
        {
            GXObject taskObject = task.Object
                ?? throw new ArgumentException("Task object is missing.", nameof(task));
            if (obj is GXDLMSProfileGeneric profile && task.Index == 2)
            {
                await GXProfileHistoryReader.Read(http, reader, task, profile, token);
                return;
            }
            bool reaNextDay = false;
            AddValue v;
            if (logger?.IsEnabled(LogLevel.Information) == true)
            {
                logger.LogInformation("{Message}", "Reading: " + obj.ToString());
            }
            object? val;
            DateTime end = DateTime.MaxValue;
            DateTime lastEnd = DateTime.MaxValue;
            GXObject? tmp = null;
            do
            {
                if (obj.ObjectType == ObjectType.ProfileGeneric && task.Index == 2)
                {
                    //Get profile generic parameters if they are already read.
                    try
                    {
                        tmp = (await http.GetAsJsonAsync<GetObjectResponse>("api/Object?id=" + taskObject.Id, cancellationToken: token))?.Item;
                        tmp ??= taskObject;
                    }
                    catch (Exception)
                    {
                        tmp = taskObject;
                    }
                    //Add date time object so GXDLMSClient is not thrown an Capture objects not read-exception.
                    GXKeyValuePair<GXDLMSObject, GXDLMSCaptureObject> p = new(new GXDLMSClock(), new GXDLMSCaptureObject(2, 0));
                    ((GXDLMSProfileGeneric)obj).CaptureObjects.Add(p);
                    var start = GetBufferReadTime(tmp);
                    if (GetEntriesInUse(tmp) > 10 && IsSortedByDateTime(tmp))
                    {
                        if (start != null)
                        {
                            int period = GetCapturePeriod(tmp);
                            if (period == -1)
                            {
                                throw new Exception("Invalid capture period.");
                            }
                            //For event logs period is zero.
                            if (period == 0 && lastEnd != DateTime.MaxValue)
                            {
                                start = lastEnd;
                            }
                            else if (end != DateTime.MaxValue)
                            {
                                //Start is not increased for the first read.
                                //There might be new rows if meter is restarted.
                                start = start.Value.AddSeconds(period);
                            }
                            //Read profile generic using range.
                            if (reaNextDay && end != DateTime.MaxValue)
                            {
                                //Read next day. This hapens if there are caps on the data.
                                end = end.AddDays(1);
                            }
                            else
                            {
                                if (end - start > new TimeSpan(24, 0, 0))
                                {
                                    //Max 24 hours is read at once.
                                    end = start.Value.DateTime.AddDays(1);
                                }
                                else
                                {
                                    //Read next hour.
                                    end = DateTime.Now;
                                    end = end.AddSeconds(-end.Second);
                                    end = end.AddMinutes(-end.Minute);
                                    end = end.AddHours(1);
                                }
                            }
                            reaNextDay = false;
                            if (end < start)
                            {
                                end = end.AddSeconds(period);
                            }
                            //Read empty days until present time.
                            do
                            {
                                if (logger?.IsEnabled(LogLevel.Information) == true)
                                {
                                    logger.LogInformation("Reading {Object} {Start} - {End}", obj, start, end);
                                }
                                val = reader.ReadRowsByRange((GXDLMSProfileGeneric)obj, start.Value.DateTime, end);
                                if (val != null)
                                {
                                    if ((val is GXArray arr) && arr.Count == 1 && (arr[0] is GXStructure s) && s.Any() && s[0] is byte[] ba)
                                    {
                                        //If there is only one row in the given time period and it's returned.
                                        if (reader.Client.ChangeType(new GXByteBuffer(ba), DataType.DateTime) is not GXDateTime val2 || val2.Compare(start.Value.DateTime) != 0)
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
                                        Values = [
                                            new()
                                            {
                                                Attribute = new GXAttribute()
                                                {
                                                    Id = GetRequiredAttribute(taskObject, 2).Id
                                                },
                                                Read = start.Value.DateTime,
                                            }
                                        ]
                                    };
                                    await http.PostAsJson("api/value/Add", v, token);
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
                                if (logger?.IsEnabled(LogLevel.Error) == true)
                                {
                                    logger.LogError("Reading {Object} the first row.", obj);
                                }
                                int sm = GetSortMethod(taskObject);
                                //If FIFO
                                if (sm == 1)
                                {
                                    UInt32 cnt = (UInt32)(GetEntriesInUse(taskObject));
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
                        if (logger?.IsEnabled(LogLevel.Error) == true)
                        {
                            logger.LogError("Reading {Object} all rows.", obj);
                        }
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
                else if (val is byte[] bytes)
                {
                    DataType dt = (DataType)(GetRequiredAttribute(taskObject, task.Index.GetValueOrDefault(0)).Template?.UIDataType ?? 0);
                    if (dt == DataType.String)
                    {
                        val = ASCIIEncoding.ASCII.GetString(bytes);
                    }
                    else if (dt == DataType.StringUTF8)
                    {
                        val = ASCIIEncoding.UTF8.GetString(bytes);
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
                if (obj.ObjectType == ObjectType.ProfileGeneric && task.Index == 2 && tmp != null)
                {
                    //Make own value from each row.
                    if (val != null)
                    {
                        GXAttribute attribute = GetRequiredAttribute(taskObject, 2);
                        List<GXValue> list = [];
                        DateTime latest = GetBufferReadTime(tmp).GetValueOrDefault().DateTime;
                        DateTime first = latest;
                        int period = -1;
                        foreach (GXStructure row in ((GXArray)val).Cast<GXStructure>())
                        {
                            bool read = false;
                            DateTime dt = DateTime.MinValue;
                            task.Data = GXDLMSTranslator.ValueToXml(row);
                            for (int pos = 0; pos != row.Count; ++pos)
                            {
                                if (row[pos] is byte[] encodedDateTime)
                                {
                                    row[pos] = GXDLMSClient.ChangeType(encodedDateTime, DataType.DateTime);
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
                                            SetReadTime(taskObject, dt);
                                            latest = dt;
                                        }
                                    }
                                }
                                //Some meters are returning null as date time to save bytes...
                                if (pos == 0 && row[pos] == null)
                                {
                                    if (period == -1)
                                    {
                                        period = GetCapturePeriod(taskObject);
                                        if (period == -1)
                                        {
                                            throw new Exception("Invalid capture period.");
                                        }
                                    }
                                    row[pos] = latest.AddMinutes(period);
                                }
                            }
                            if (logger?.IsEnabled(LogLevel.Information) == true)
                            {
                                logger.LogInformation("{Message}", "Read: " + task.Data);
                            }
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
                            v = new AddValue() { Values = [.. list] };
                            await http.PostAsJson("/api/value/Add", v, token);
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
                    if (val is not string)
                    {
                        val = Convert.ToString(val, CultureInfo.InvariantCulture);
                    }
                    task.Data = (string?)val;
                    if (logger?.IsEnabled(LogLevel.Information) == true)
                    {
                        logger.LogInformation("{Message}", "Read: " + val);
                    }
                    int index = GetAttributeIndex(taskObject, task.Index.GetValueOrDefault(0));
                    if (index != -1)
                    {
                        v = new AddValue()
                        {
                            Values = [
                                new()
                                {
                                    Attribute = new GXAttribute()
                                    {
                                        Id = GetRequiredAttribute(taskObject, task.Index.GetValueOrDefault(0)).Id
                                    },
                                    Read = DateTime.Now,
                                    Value = (string?)val
                                }
                            ]
                        };
                        await http.PostAsJson("/api/value/Add", v, token);
                    }
                    else
                    {
                        if (logger?.IsEnabled(LogLevel.Information) == true)
                        {
                            logger.LogInformation("{Message}", "Invalid task index: " + task.Index);
                        }
                    }
                }
            }
            while (reaNextDay);
        }
    }
}

