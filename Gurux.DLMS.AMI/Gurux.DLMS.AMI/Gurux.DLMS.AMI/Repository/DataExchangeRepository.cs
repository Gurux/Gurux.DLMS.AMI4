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

using Gurux.DLMS.AMI.Module;
using Gurux.DLMS.AMI.Shared.DIs;
using Gurux.DLMS.AMI.Shared.Rest;

namespace Gurux.DLMS.AMI.Server.Repository;

/// <summary>Forwards the existing data exchange API to the active DataExchange module.</summary>
internal sealed class DataExchangeRepository(IGXAmiModuleServices modules) : IDataExchange
{
    public Task<GXDataExchangeTypeResponse> AvailableTypes(GXDataExchangeType? request, CancellationToken cancellationToken = default)
        => InvokeAsync((service, token) => service.AvailableTypes(request, token), cancellationToken);

    public Task<GXImportDataResponse> ImportDataAsync(GXImportData data, CancellationToken cancellationToken = default)
        => InvokeAsync((service, token) => service.ImportDataAsync(data, token), cancellationToken);

    public Task<GXExportDataResponse> ExportDataAsync(GXExportData data, CancellationToken cancellationToken = default)
        => InvokeAsync((service, token) => service.ExportDataAsync(data, token), cancellationToken);

    private async Task<TResult> InvokeAsync<TResult>(Func<IDataExchange, CancellationToken, Task<TResult>> operation, CancellationToken cancellationToken)
    {
        try
        {
            return await modules.InvokeAsync(operation, cancellationToken);
        }
        catch (AmiModuleUnavailableException)
        {
            throw new AmiModuleUnavailableException("The Gurux.AMI.DataExchange module is not active.");
        }
    }
}
