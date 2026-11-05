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

using System.Security.AccessControl;
using System.Security.Principal;

namespace Gurux.DLMS.AMI.Agent;

/// <summary>Stores registration JSON using private permissions and temporary-file replacement.</summary>
internal static class GXAgentSettingsStore
{
    /// <summary>Gets the registration path in the directory from which this agent is run.</summary>
    internal static string DefaultPath => Path.Combine(Environment.CurrentDirectory, "settings.json");

    /// <summary>Restricts access to an existing registration without importing another agent's settings.</summary>
    /// <param name="path">Selected registration path.</param>
    internal static void ProtectExisting(string path)
    {
        if (File.Exists(path))
        {
            RestrictFile(path);
        }
    }

    private static void RestrictFile(string path)
    {
        if (OperatingSystem.IsWindows())
        {
            var security = new FileSecurity();
            security.SetAccessRuleProtection(true, false);
            security.AddAccessRule(new FileSystemAccessRule(WindowsIdentity.GetCurrent().User!, FileSystemRights.FullControl, AccessControlType.Allow));
            new FileInfo(path).SetAccessControl(security);
        }
        else
            {
            File.SetUnixFileMode(path, UnixFileMode.UserRead | UnixFileMode.UserWrite);
        }
    }

    /// <summary>Creates a directory with access restricted to the current account if it does not already exist.</summary>
    /// <param name="directory">Directory to create.</param>
    /// <remarks>Permissions on an existing directory are not changed.</remarks>
    internal static void EnsurePrivateDirectory(string directory)
    {
        if (!Directory.Exists(directory))
        {
            if (OperatingSystem.IsWindows())
            {
                Directory.CreateDirectory(directory);
                var security = new DirectorySecurity();
                security.SetAccessRuleProtection(true, false);
                security.AddAccessRule(new FileSystemAccessRule(WindowsIdentity.GetCurrent().User!, FileSystemRights.FullControl,
                    InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit, PropagationFlags.None, AccessControlType.Allow));
                new DirectoryInfo(directory).SetAccessControl(security);
            }
            else
                {
                Directory.CreateDirectory(directory, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
            }
        }
    }

    /// <summary>Writes private JSON to a temporary sibling file and replaces the destination after the write completes.</summary>
    /// <param name="path">Destination file path; its parent directory is created when missing.</param>
    /// <param name="json">Serialized registration or completion data, which may contain secrets.</param>
    /// <param name="token">Cancels asynchronous writing before replacement.</param>
    /// <returns>A task that completes once the temporary file has replaced the destination.</returns>
    /// <remarks>Temporary files are removed on failure. This method does not validate JSON or explicitly flush it to physical media.</remarks>
    /// <exception cref="OperationCanceledException">The write is canceled.</exception>
    internal static async Task SaveAsync(string path, string json, CancellationToken token)
    {
        path = Path.GetFullPath(path);
        EnsurePrivateDirectory(Path.GetDirectoryName(path)!);
        string temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            var fileOptions = new FileStreamOptions { Mode = FileMode.CreateNew, Access = FileAccess.Write, Share = FileShare.None, Options = FileOptions.Asynchronous };
            if (!OperatingSystem.IsWindows())
                {
                fileOptions.UnixCreateMode = UnixFileMode.UserRead | UnixFileMode.UserWrite;
            }
            FileStream stream;
            if (OperatingSystem.IsWindows())
            {
                var security = new FileSecurity();
                security.SetAccessRuleProtection(true, false);
                security.AddAccessRule(new FileSystemAccessRule(WindowsIdentity.GetCurrent().User!, FileSystemRights.FullControl, AccessControlType.Allow));
                stream = new FileInfo(temporary).Create(FileMode.CreateNew, FileSystemRights.Write | FileSystemRights.ReadPermissions,
                    FileShare.None, 4096, FileOptions.Asynchronous, security);
            }
            else
                {
                stream = new FileStream(temporary, fileOptions);
            }
            await using (stream)
            {
                await using var writer = new StreamWriter(stream);
                await writer.WriteAsync(json.AsMemory(), token);
            }
            File.Move(temporary, path, overwrite: true);
        }
        finally { if (File.Exists(temporary)) {
            File.Delete(temporary);
        } }
    }
}
