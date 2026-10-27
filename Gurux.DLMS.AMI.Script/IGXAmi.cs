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

using Gurux.DLMS.AMI.Shared.DTOs.Authentication;
using Gurux.DLMS.AMI.Shared.DTOs.Device;
using System.Threading;

namespace Gurux.DLMS.AMI.Script
{
    /// <summary>
    /// Provides scripts with their execution context, supported repository operations and application services.
    /// </summary>
    /// <remarks>
    /// Supported entity types depend on the operation. The default implementation dispatches to
    /// repositories resolved from dependency injection and provides synchronous wrappers for asynchronous calls.
    /// </remarks>
    public interface IGXAmi
    {
        /// <summary>
        /// Gets or sets the object that triggered execution, or null when there is no sender.
        /// Assigning a user as the sender also sets User in the default implementation.
        /// </summary>
        object? Sender { get; set; }

        /// <summary>
        /// Gets or sets the user associated with script execution, or null when no user is assigned.
        /// </summary>
        GXUser? User { get; set; }

        /// <summary>
        /// Gets or sets the template used for an unknown connecting device, or null when none is configured.
        /// </summary>
        /// <remarks>
        /// The agent can use this template to create a device when an unknown device connects.
        /// </remarks>
        GXDeviceTemplate? DefaultDeviceTemplate { get; set; }

        /// <summary>
        /// Returns supported entities that match the supplied filter.
        /// </summary>
        /// <typeparam name="T">Supported AMI entity type, such as a device, task or log entry.</typeparam>
        /// <param name="filter">Entity containing the filter fields or identifier used by the repository.</param>
        /// <returns>The matching entities.</returns>
        IEnumerable<T> Select<T>(T filter);

        /// <summary>
        /// Returns supported entities that match the supplied filter.
        /// </summary>
        /// <typeparam name="T">Supported AMI entity type, such as a device, task or log entry.</typeparam>
        /// <param name="filter">Entity containing the filter fields or identifier used by the repository.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task whose result contains the matching entities.</returns>
        Task<IEnumerable<T>> SelectAsync<T>(T filter, CancellationToken cancellationToken = default);

        /// <summary>
        /// Reads a supported entity using its identifier or, for devices and gateways, its filter.
        /// </summary>
        /// <typeparam name="T">Supported AMI entity type, such as a device, task or log entry.</typeparam>
        /// <param name="filter">Entity containing the filter fields or identifier used by the repository.</param>
        /// <returns>The entity, or null when no entity is found. Device and gateway filters must match exactly one entity.</returns>
        T? SingleOrDefault<T>(T filter);

        /// <summary>
        /// Reads a supported entity using its identifier or, for devices and gateways, its filter.
        /// </summary>
        /// <typeparam name="T">Supported AMI entity type, such as a device, task or log entry.</typeparam>
        /// <param name="filter">Entity containing the filter fields or identifier used by the repository.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task whose result is the entity, or null when no entity is found. Device and gateway filters must match exactly one entity.</returns>
        Task<T?> SingleOrDefaultAsync<T>(T filter, CancellationToken cancellationToken = default);

        /// <summary>
        /// Persists a supported entity or collection through its repository.
        /// </summary>
        /// <param name="value">Entity or collection to persist.</param>
        void Add(object value);

        /// <summary>
        /// Persists a supported entity or collection through its repository.
        /// </summary>
        /// <param name="value">Entity or collection to persist.</param>
        /// <param name="cancellationToken">Token used to request cancellation of the repository operation.</param>
        /// <returns>A task representing the repository operation.</returns>
        Task AddAsync(object value, CancellationToken cancellationToken = default);

        /// <summary>
        /// Persists a device and updates its identifier from the repository result.
        /// </summary>
        /// <param name="value">Added device.</param>
        /// <param name="lateBinding">True to defer creating device objects until they are read from the meter.</param>
        void AddDevice(GXDevice value, bool lateBinding);

        /// <summary>
        /// Persists a device and updates its identifier from the repository result.
        /// </summary>
        /// <param name="value">Added device.</param>
        /// <param name="lateBinding">True to defer creating device objects until they are read from the meter.</param>
        /// <param name="cancellationToken">Token used to request cancellation of the repository operation.</param>
        /// <returns>A task representing the repository operation.</returns>
        Task AddDeviceAsync(GXDevice value, bool lateBinding, CancellationToken cancellationToken = default);

        /// <summary>
        /// Removes supported entities, or closes log entries, through their repository.
        /// </summary>
        /// <param name="value">Supported entity or collection to remove, or log entries to close.</param>
        /// <param name="delete">True to permanently delete entities where supported by the repository; false to mark them as removed. Ignored for log entries and tasks.</param>
        /// <param name="cancellationToken">Token used to request cancellation of the repository operation.</param>
        /// <returns>A task representing the repository operation.</returns>
        Task RemoveAsync(object value, bool delete, CancellationToken cancellationToken = default);

        /// <summary>
        /// Removes supported entities, or closes log entries, through their repository.
        /// </summary>
        /// <param name="value">Supported entity or collection to remove, or log entries to close.</param>
        /// <param name="delete">True to permanently delete entities where supported by the repository; false to mark them as removed. Ignored for log entries and tasks.</param>
        void Remove(object value, bool delete);

        /// <summary>
        /// Clears log records through the log repository.
        /// </summary>
        /// <remarks>
        /// The default implementation supports GXLog only and clears all log records;
        /// it does not use the supplied items to restrict the operation.
        /// </remarks>
        /// <typeparam name="T">Log entity type.</typeparam>
        /// <param name="items">Optional entries; ignored by the default implementation.</param>
        /// <returns>A task representing the log clearing operation.</returns>
        Task ClearAsync<T>(IEnumerable<T>? items);

        /// <summary>
        /// Clears log records through the log repository.
        /// </summary>
        /// <remarks>
        /// The default implementation supports GXLog only and clears all log records;
        /// it does not use the supplied items to restrict the operation.
        /// </remarks>
        /// <typeparam name="T">Log entity type.</typeparam>
        /// <param name="items">Optional entries; ignored by the default implementation.</param>
        void Clear<T>(IEnumerable<T>? items);

        /// <summary>
        /// Persists a supported entity or collection using the same repository dispatch as Add.
        /// </summary>
        /// <param name="value">Entity or collection to persist.</param>
        void Update(object value);

        /// <summary>
        /// Persists a supported entity or collection using the same repository dispatch as Add.
        /// </summary>
        /// <param name="value">Entity or collection to persist.</param>
        /// <param name="cancellationToken">Token used to request cancellation of the repository operation.</param>
        /// <returns>A task representing the repository operation.</returns>
        Task UpdateAsync(object value, CancellationToken cancellationToken = default);

        /// <summary>
        /// Resolves an optional application service by its runtime type.
        /// </summary>
        /// <param name="type">Required service type.</param>
        /// <returns>The registered service, or null if the service is not registered.</returns>
        object? GetService(Type type);

        /// <summary>
        /// Resolves an optional application service by its generic type.
        /// </summary>
        /// <typeparam name="T">Service type.</typeparam>
        /// <returns>The registered service, or null if the service is not registered.</returns>
        T? GetService<T>();
    }
}
