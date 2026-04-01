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

using Microsoft.AspNetCore.Components;
using System.Globalization;

namespace Gurux.DLMS.AMI.Components
{
    public class GXTextChangedEventArgs
    {
        /// <summary>
        /// Text value before the change.
        /// </summary>
        public string? PreviousText { get; set; }

        /// <summary>
        /// Text value after the change.
        /// </summary>
        public string? CurrentText { get; set; }
    }

    /// <summary>
    /// Resource storage is used to retrieve data from local storage or the server.
    /// </summary>
    public interface IGXResourceStorage
    {
        /// <summary>
        /// Update localization settings.
        /// </summary>
        /// <returns></returns>
        Task UpdateLocalizationAsync();

        /// <summary>
        /// Get theme settings from the local storage or server.
        /// </summary>
        /// <returns></returns>
        Task<GXThemeInfo?> GetCurrentThemeAsync();

        /// <summary>
        /// Get installed imagepacks from the local storage or server.
        /// </summary>
        /// <returns></returns>
        Task<IEnumerable<string>?> GetCurrentIconPacksAsync();

        /// <summary>
        /// Get image from the local storage or server.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        Task<string?> GetImageAsync(string name);

        /// <summary>
        /// Get images from the local storage or server.
        /// </summary>
        /// <param name="names">Image names.</param>
        /// <returns>List of images.</returns>
        Task<List<string?>?> GetImagesAsync(IEnumerable<string> names);

        /// <summary>
        /// Refresh enum types from the server.
        /// </summary>
        /// <param name="type">Enum type name.</param>
        /// <returns>Return an array of { id, name } items for the enum.</returns>
        Task<IEnumerable<KeyValuePair<int, string>>?> RefreshEnumTypesAsync(string type);

        /// <summary>
        /// Get enum type from the local storage or server.
        /// </summary>
        /// <param name="type">Enum type name.</param>
        /// <param name="id">Enum ID.</param>
        /// <returns>Enum type name.</returns>
        Task<string?> GetEnumTypeAsync(string type, int id);

        /// <summary>
        /// Get enum types from the local storage or server.
        /// </summary>
        /// <param name="type">Enum type name.</param>
        /// <param name="ids">Enum IDs.</param>
        /// <returns>List of enum types.</returns>
        Task<List<string?>?> GetEnumTypesAsync(string type, IEnumerable<int> ids);

        /// <summary>
        /// Get enum type from the cache.
        /// </summary>
        /// <param name="type">Enum type name.</param>
        /// <param name="id">Enum ID.</param>
        /// <returns>Enum type name.</returns>
        string? GetEnumType(string type, int? id);

        /// <summary>
        /// Get enum type from the cache.
        /// </summary>
        /// <param name="type">Enum type name.</param>
        /// <param name="value">Enumerated value.</param>
        /// <returns>Enum type name.</returns>
        string? GetEnumType(string type, Enum value);

        /// <summary>
        /// Update enum types to the cache.
        /// </summary>
        /// <param name="type">Enum type name.</param>
        /// <param name="values">key value pairs.</param>
        /// <returns>Return an array of { id, name } items for the enum.</returns>
        void UpdateEnumTypes(string type, IEnumerable<KeyValuePair<int, string>> values);

        /// <summary>
        /// Clear enum type cache.
        /// </summary>
        /// <param name="type">Enum type name.</param>
        void ClearEnumTypes(string type);

        /// <summary>
        /// Get localized text from the local storage or server.
        /// </summary>
        /// <param name="cultureInfo">Used culture.</param>
        /// <param name="name">Localized name.</param>
        /// <returns>localized text</returns>
        Task<string> GetLocalizedTextAsync(CultureInfo cultureInfo, string name);

        /// <summary>
        /// Get localized text from the local storage or server.
        /// </summary>
        /// <param name="name">Localized name.</param>
        /// <returns>localized text</returns>
        Task<string> GetLocalizedTextAsync(string name);

        /// <summary>
        /// Get localized texts from the local storage or server.
        /// </summary>
        /// <param name="cultureInfo">Used culture.</param>
        /// <param name="names">Localized names.</param>
        /// <returns>List of localized texts.</returns>
        Task<List<string>> GetLocalizedTextsAsync(CultureInfo cultureInfo, IEnumerable<string> names);

        /// <summary>
        /// Get localized texts from the local storage or server.
        /// </summary>
        /// <param name="names">Localized names.</param>
        /// <returns>List of localized texts.</returns>
        Task<List<string>> GetLocalizedTextsAsync(IEnumerable<string> names);

        /// <summary>
        /// Notified when the theme is updated.
        /// </summary>
        public event EventHandler<GXThemeInfo> OnThemeChanged;

        /// <summary>
        /// Notified when the enum types is changed.
        /// </summary>
        public EventCallback OnEnumTypesChanged { get; set; }

        /// <summary>
        /// Notified when the localized text is changed.
        /// </summary>
        public event EventHandler<GXTextChangedEventArgs>? OnTextChanged;

        /// <summary>
        /// Notified when the image is changed.
        /// </summary>
        public event EventHandler<GXImageChangedArgs>? OnImageChanged;

        /// <summary>
        /// Converts the specified <see cref="DateTimeOffset"/> value to its string representation.
        /// </summary>
        /// <remarks>The format of the returned string depends on the default formatting conventions for
        /// <see cref="DateTimeOffset"/>.</remarks>
        /// <param name="value">The <see cref="DateTimeOffset"/> value to convert.</param>
        /// <returns>A string representation of the specified <see cref="DateTimeOffset"/> value.</returns>
        /// <seealso cref="HasLocalDateTimeAsync"/>
        public string? DateTimeOffsetToString(DateTimeOffset? value);

        /// <summary>
        /// Determines whether the current instance uses local or UTC time.
        /// </summary>
        /// <returns><see langword="true"/> if the current instance contains a local time; otherwise, UTC time is used.</returns>
        /// <seealso cref="DateTimeOffsetToString"/>
        /// <seealso cref="SetLocalDateTimeAsync"/>
        public Task<bool> HasLocalDateTimeAsync();

        /// <summary>
        /// Sets the local date and time asynchronously.
        /// </summary>
        /// <param name="value">
        /// A boolean value indicating whether the local ot UTC time is used.
        /// </param>
        /// <seealso cref="DateTimeOffsetToString"/>
        /// <seealso cref="HasLocalDateTimeAsync"/>
        public Task SetLocalDateTimeAsync(bool value);
    }
}