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

using Gurux.DLMS.AMI.Shared.DTOs.ContentType;
using System.Globalization;

namespace Gurux.DLMS.AMI.Client.Helpers
{

    public class GXContentTypeFieldConverter<T>
    {
        private readonly GXContentTypeField _value;

        private T? _value2;

        public T? Value
        {
            get
            {
                if (_value2 != null)
                {
                    return _value2;
                }
                if (string.IsNullOrEmpty(_value.Default))
                {
                    return default;
                }
                _value2 = (T?)Activator.CreateInstance(typeof(T));
                typeof(T)?.GetProperty("Id")?.SetValue(_value2, Guid.Parse(_value.Default));
                return _value2;
            }
            set
            {
                _value2 = value;
                if (value == null)
                {
                    _value.Default = null;
                }
                else
                {
                    _value.Default = Convert.ToString(typeof(T)?.GetProperty("Id")?.GetValue(value));
                }
            }
        }

        /// <summary>
        /// User Value as date time.
        /// </summary>
        /// <remarks>
        /// This value is used when user can give any value.
        /// </remarks>
        /// <seealso cref="Values"/>
        public DateTime? ValueAsDateTime
        {
            get
            {
                if (string.IsNullOrEmpty(_value.Default))
                {
                    return null;
                }
                return DateTimeOffset.Parse(_value.Default, CultureInfo.InvariantCulture).LocalDateTime;
            }
            set
            {
                if (value == null)
                {
                    _value.Default = null;
                }
                else
                {
                    DateTimeOffset tmp = new DateTimeOffset(value.Value);
                    _value.Default = value?.ToString(CultureInfo.InvariantCulture);
                }
            }
        }

        /// <summary>
        /// User Value as date.
        /// </summary>
        /// <remarks>
        /// This value is used when user can give any value.
        /// </remarks>
        public DateTime? ValueAsDate
        {
            get
            {
                if (string.IsNullOrEmpty(_value.Default))
                {
                    return null;
                }
                return DateTimeOffset.Parse(_value.Default, CultureInfo.InvariantCulture).LocalDateTime;
            }
            set
            {
                if (value == null)
                {
                    _value.Default = null;
                }
                else
                {
                    DateTimeOffset tmp = new DateTimeOffset(value.Value);
                    _value.Default = value?.ToString(CultureInfo.InvariantCulture);
                }
            }
        }

        /// <summary>
        /// User Value as time.
        /// </summary>
        /// <remarks>
        /// This value is used when user can give any value.
        /// </remarks>
        public TimeOnly? ValueAsTime
        {
            get
            {
                if (string.IsNullOrEmpty(_value.Default))
                {
                    return null;
                }
                return TimeOnly.Parse(_value.Default, CultureInfo.InvariantCulture);
            }
            set
            {
                if (value == null)
                {
                    _value.Default = null;
                }
                else
                {
                    _value.Default = value.Value.ToString(CultureInfo.InvariantCulture);
                }
            }
        }


        /// <summary>
        /// User Value as date time.
        /// </summary>
        /// <remarks>
        /// This value is used when user can give any value.
        /// </remarks>
        public TimeSpan? ValueAsTimeSpan
        {
            get
            {
                if (string.IsNullOrEmpty(_value.Default))
                {
                    return null;
                }
                return TimeSpan.Parse(_value.Default, CultureInfo.InvariantCulture);
            }
            set
            {
                if (value == null)
                {
                    _value.Default = null;
                }
                else
                {
                    _value.Default = value.Value.ToString("HH:mm:ss", CultureInfo.InvariantCulture);
                }
            }
        }

        /// <summary>
        /// User Value as boolean.
        /// </summary>
        /// <remarks>
        /// This value is used when user can give any value.
        /// </remarks>
        public bool ValueAsBoolean
        {
            get
            {
                return _value.Default == "1";
            }
            set
            {
                _value.Default = value ? "1" : "0";
            }
        }

        /// <summary>
        /// User Value as byte array.
        /// </summary>
        /// <remarks>
        /// This value is used when user can give any value.
        /// </remarks>
        private byte[]? ValueAsByteArray
        {
            get
            {
                if (string.IsNullOrEmpty(_value.Default))
                {
                    return null;
                }
                return Convert.FromHexString(_value.Default);
            }
            set
            {
                if (value == null)
                {
                    _value.Default = null;
                }
                else
                {
                    _value.Default = Convert.ToHexString(value);
                }
            }
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="value"></param>
        public GXContentTypeFieldConverter(GXContentTypeField value)
        {
            _value = value;
        }
    }
}
