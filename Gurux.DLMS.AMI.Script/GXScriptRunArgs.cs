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

using System.Runtime.Loader;

namespace Gurux.DLMS.AMI.Script
{
    /// <summary>
    /// Specifies the compiled script, entry method and arguments used for one invocation.
    /// </summary>
    public class GXScriptRunArgs
    {
        /// <summary>Gets or sets the cooperative cancellation token for this invocation.</summary>
        /// <remarks>
        /// The runner links this token with the token passed to RunAsync and exposes the result through
        /// <see cref="IGXAmi.CancellationToken"/>. Canceling it does not forcibly terminate script code.
        /// </remarks>
        public CancellationToken CancellationToken { get; set; }

        /// <summary>
        /// Gets or sets the emitted assembly image used when no assembly is already loaded. Defaults to an empty array.
        /// </summary>
        public byte[] ByteAssembly { get; set; } = [];

        /// <summary>
        /// Gets or sets the public script method to invoke. Defaults to "Run".
        /// </summary>
        /// <remarks>
        /// The method can return a synchronous value, Task or ValueTask, including generic forms.
        /// The runner rejects async void methods because their completion cannot be awaited.
        /// </remarks>
        public string MethodName { get; set; } = "Run";

        /// <summary>
        /// Gets or sets whether reflection invokes the method on a thread pool thread. Defaults to false.
        /// </summary>
        /// <remarks>
        /// Asynchronous results are always awaited. Cancellable invocations also use a thread pool thread.
        /// </remarks>
        public bool Asyncronous { get; set; }
        /// <summary>
        /// Gets or sets the context whose first loaded assembly is reused for execution.
        /// </summary>
        /// <remarks>
        /// If null or empty, the runner replaces this value with a new collectible context and loads
        /// <see cref="ByteAssembly"/> into it. The caller is responsible for requesting unloading when finished.
        /// </remarks>
        public AssemblyLoadContext? AssemblyLoadContext { get; set; }

        /// <summary>
        /// Gets or sets the positional arguments passed to the method. Null invokes a parameterless method.
        /// </summary>
        /// <remarks>The number, order and types must match the selected method's parameters.</remarks>
        public object?[]? Parameters { get; set; }
    }
}
