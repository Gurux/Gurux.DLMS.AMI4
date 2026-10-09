# Gurux.DLMS.AMI4

Lightweight .NET library containing AMI (Advanced Metering Infrastructure) extensions for the Gurux DLMS/COSEM framework.

## Features

- AMI-specific module interfaces and helpers under the `Gurux.DLMS.AMI.Module` namespace
- Designed to integrate with the Gurux Device Framework
- Open source (GPLv2)

## Requirements

- .NET 9 SDK
- C# 13
- __Visual Studio 2022__ or later (or `dotnet` CLI)

## Quick start

1. Clone the repository:


   git clone https://github.com/Gurux/Gurux.DLMS.AMI4.git
   cd Gurux.DLMS.AMI4


2. Open the solution in __Visual Studio 2022__ or use the CLI:

- In Visual Studio: File > Open > Project/Solution, open the `.sln` file, then use __Build > Build Solution__.
- CLI: `dotnet restore` then `dotnet build`.

## Usage

Reference the assembly or project and use the types in the `Gurux.DLMS.AMI.Module` namespace. Example:


using Gurux.DLMS.AMI.Module;

// Implement a settings UI for AMI attributes
public class MyAttributeSettings : IAmiExtendedAttributeSettingsUI
{
    // Implement IAmiExtendedSettingsUI members
}


## Policy editor

The shared policy selection UI is `Gurux.UI.Components.PolicyEditor`, provided
by the `Gurux.UI.Components` project. It is no longer part of this module library.
Client and module pages bind `List<string>?` policy names with `@bind-Value`.
The host supplies `HttpClient`, `IGXNotificationService`, and `IGXProgress` through
DI. Loading uses `api/Policy/Available`; failures use the notification service
and loading can be cancelled through the progress service.

## Deployment packages

Module projects in `Gurux.DLMS.AMI.Modules` share `Directory.Build.targets`, which
excludes assemblies supplied by the AMI host from publish output and ZIP files.
This covers Microsoft/System references present in the host and the shared Gurux
contracts. Unknown Microsoft/System packages and module-private libraries remain
included, along with native dependencies and static resources. Development builds
keep CopyLocal dependencies. Reused publish folders are cleaned before ZIP creation.

Use `AmiModuleHostProject` to select another host project, or
`AmiModuleExcludeHostAssemblies=false` to publish without filtering.
`AmiModuleArchiveDirectory` selects the ZIP output directory. The verification
script `scripts/Test-AmiModulePublish.ps1` checks ZIP contents and loads the module
against host assemblies without initializing its application services.

## Contributing

Please follow the repository's coding guidelines and the project's `__CONTRIBUTING.md__` and `.editorconfig` files. Open issues and pull requests against the `main` branch. Keep changes small and focused, and include unit tests where appropriate.

## License

This code is licensed under the GNU General Public License v2 (GPL-2.0). See http://www.gnu.org/licenses/gpl-2.0.txt for details.

## Contact

Repository: [Gurux.DLMS.AMI4 GitHub](https://github.com/Gurux/Gurux.DLMS.AMI4)

For questions or support, open an issue on the repository.

## Module metadata

AmiModuleManifest is defined in Gurux.DLMS.AMI.Client and used by the Blazor client
and its host. AmiModuleTab and AmiModulePage are defined in Gurux.DLMS.AMI.Module
for module builders. AmiModuleDependency stays in Gurux.DLMS.AMI.Shared.DTOs.Module;
AmiModuleState and AmiModuleDependencyKind stay in Gurux.DLMS.AMI.Shared.Enums.
Module and Shared do not reference Client. Runtime exceptions and integration
interfaces in AmiModuleIntegrations.cs remain in Gurux.DLMS.AMI.Module.
