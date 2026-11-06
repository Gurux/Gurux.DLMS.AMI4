using Microsoft.Extensions.Configuration;

namespace Gurux.DLMS.AMI.Agent.Shared;

/// <summary>Limits how long the agent waits for each script invocation.</summary>
internal sealed class GXScriptExecutionOptions
{
    /// <summary>Gets or sets the positive execution deadline, up to one day. Defaults to 30 seconds.</summary>
    public TimeSpan ExecutionTimeout { get; set; } = TimeSpan.FromSeconds(30);

    internal void Validate()
    {
        if (ExecutionTimeout <= TimeSpan.Zero || ExecutionTimeout > TimeSpan.FromDays(1))
        {
            throw new ArgumentException("Scripts:ExecutionTimeout must be positive and at most one day.");
        }
    }

    internal static GXScriptExecutionOptions Read(IConfiguration configuration)
    {
        var options = new GXScriptExecutionOptions
        {
            ExecutionTimeout = configuration.GetValue("Scripts:ExecutionTimeout", TimeSpan.FromSeconds(30))
        };
        options.Validate();
        return options;
    }
}
