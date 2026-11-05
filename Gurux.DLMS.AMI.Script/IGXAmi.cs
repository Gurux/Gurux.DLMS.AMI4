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
    /// repositories resolved from dependency injection. Synchronous wrappers block until the asynchronous
    /// operation completes and can wrap failures in AggregateException. Unsupported entity types cause
    /// ArgumentException; missing repository registrations cause InvalidOperationException.
    /// Asynchronous repository operations combine their supplied token with the current invocation token.
    /// </remarks>
    public interface IGXAmi
    {
        /// <summary>Gets the cooperative cancellation token for the current invocation.</summary>
        /// <remarks>
        /// Scripts should observe this token during long-running work. The interface default returns
        /// a token that cannot be canceled; the runner exposes the linked invocation tokens.
        /// </remarks>
        CancellationToken CancellationToken => default;

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
        /// <param name="cancellationToken">Token combined with the current invocation token to request cancellation.</param>
        /// <returns>A task whose result contains the matching entities.</returns>
        /// <exception cref="OperationCanceledException">The operation or current invocation token is canceled.</exception>
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
        /// <param name="cancellationToken">Token combined with the current invocation token to request cancellation.</param>
        /// <returns>A task whose result is the entity, or null when no entity is found. Device and gateway filters must match exactly one entity.</returns>
        /// <exception cref="OperationCanceledException">The operation or current invocation token is canceled.</exception>
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
        /// <exception cref="OperationCanceledException">The operation or current invocation token is canceled.</exception>
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
        /// <exception cref="OperationCanceledException">The operation or current invocation token is canceled.</exception>
        Task AddDeviceAsync(GXDevice value, bool lateBinding, CancellationToken cancellationToken = default);

        /// <summary>
        /// Removes supported entities, or closes log entries, through their repository.
        /// </summary>
        /// <param name="value">Supported entity or collection to remove, or log entries to close.</param>
        /// <param name="delete">True to permanently delete entities where supported by the repository; false to mark them as removed. Ignored for log entries and tasks.</param>
        /// <param name="cancellationToken">Token used to request cancellation of the repository operation.</param>
        /// <returns>A task representing the repository operation.</returns>
        /// <exception cref="OperationCanceledException">The operation or current invocation token is canceled.</exception>
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
        /// <exception cref="OperationCanceledException">The operation or current invocation token is canceled.</exception>
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
        /// <exception cref="OperationCanceledException">The operation or current invocation token is canceled.</exception>
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
