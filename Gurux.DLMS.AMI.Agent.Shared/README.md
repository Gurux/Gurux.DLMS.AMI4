# Gurux.DLMS.AMI.Agent.Shared

Shared settings and worker interfaces for agents in Gurux DLMS AMI v4.
This .NET class library provides contracts used by the agent host and worker implementation.

## Contents

- `AgentOptions`: server connection, authentication, version, serial port, and worker settings.
- `IGXAgentWorker`: worker initialization, agent registration, start/stop operations, and task polling.

Worker settings cover meter reading, incoming meter connections, notifications,
stored raw notification processing, and gateway connections.
The settings types are provided by `Gurux.DLMS.AMI.Shared`.

## Build

Requires the .NET 10 SDK and the sibling `Gurux.DLMS.AMI.Shared` project
from the Gurux DLMS AMI v4 repository. From this directory, run:

```sh
dotnet build Gurux.DLMS.AMI.Agent.Shared.csproj
```

XML API documentation is generated during the build.
This library is used by other projects; it is not a standalone application.

## Configuration

Use `AgentOptions` to supply the AMI server address and a personal access token,
then configure the workers needed by the agent. Keep access tokens out of source control.
The agent host and worker implementation handle startup and communication.

## License

GNU General Public License v2.0 only (`GPL-2.0-only`).
See the license in the repository and the source file headers.
