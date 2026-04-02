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


## Contributing

Please follow the repository's coding guidelines and the project's `__CONTRIBUTING.md__` and `.editorconfig` files. Open issues and pull requests against the `main` branch. Keep changes small and focused, and include unit tests where appropriate.

## License

This code is licensed under the GNU General Public License v2 (GPL-2.0). See http://www.gnu.org/licenses/gpl-2.0.txt for details.

## Contact

Repository: [Gurux.DLMS.AMI4 GitHub](https://github.com/Gurux/Gurux.DLMS.AMI4)

For questions or support, open an issue on the repository.
