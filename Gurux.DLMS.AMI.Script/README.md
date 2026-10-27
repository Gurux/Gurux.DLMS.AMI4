# Gurux.DLMS.AMI.Script

A .NET library that adds scripting to Gurux DLMS AMI v4 (Advanced Metering Infrastructure).

Use it to customize AMI workflows with C# code: query devices, store meter values, create tasks,
manage schedules and write logs through the application's repositories.

## How it works

- `GXAmiScript.Generate` compiles source code into an in-memory assembly using Roslyn and collects compiler diagnostics.
- `GXAmiScript.RunAsync` creates a script instance and calls its public entry method (`Run` by default).
- `IGXAmi` gives scripts access to the execution context, supported repository operations and registered application services.

Scripts must define `Gurux.DLMS.AMI.GeneratedScript.GXAmiScript` with a public constructor
accepting `IGXAmi`. For example:

```csharp
using Gurux.DLMS.AMI.Script;

namespace Gurux.DLMS.AMI.GeneratedScript
{
    public class GXAmiScript
    {
        private readonly IGXAmi ami;

        public GXAmiScript(IGXAmi ami) => this.ami = ami;

        public string Run() => ami.Sender?.ToString() ?? "No sender";
    }
}
```

The hosting AMI application supplies the service provider and registers the repositories used by scripts.
This project is a library integrated into AMI, rather than a standalone application.

## Build

Requires the .NET 10 SDK and the sibling `Gurux.DLMS.AMI.Shared` project from the AMI repository.

```sh
dotnet build Gurux.DLMS.AMI.Script.csproj
```

See the [Gurux DLMS AMI project](https://www.gurux.fi/Gurux.DLMS.AMI4) for more information.

## License

Licensed under GPL-2.0-only.
