using System.Collections.Frozen;
using Gurux.DLMS.AMI.Module.Enums;

namespace Gurux.DLMS.AMI.Server.Module;

public sealed partial class GXModuleRuntime
{
    // Immutable contract-only snapshot: never retain an exported service instance.
    private FrozenSet<Type> availableServices = FrozenSet<Type>.Empty;

    private void RefreshServiceAvailability()
    {
        lock (sync)
        {
            var snapshot = exports.Where(pair => entries.TryGetValue(pair.Value, out var entry) &&
                    entry.State == AmiModuleState.Active && !entry.UninstallRequested)
                .Select(pair => pair.Key).ToFrozenSet();
            Volatile.Write(ref availableServices, snapshot);
        }
    }
}
