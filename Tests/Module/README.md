# Module tests

Run `dotnet test Tests/Module/Module.Tests.slnx -m:1` from the repository root to run the Module, runtime and health suites. Run `dotnet test Tests/Module/Gurux.DLMS.AMI.Module.Tests.csproj` for Module contract, event, settings, test-host and legacy compatibility tests.

`Support` contains the source-only, non-packable test host library shared with Diagnostics and UserActivity tests. Its assembly and namespaces retain the existing Module.Testing names. See Support/README.md for usage.

`Compatibility/LegacyModule` deliberately compiles against the published 4.x Module package. The tests verify that Module 5 rejects this old contract before loading removed interface names. All Test sources and fixtures are excluded from the production Module package.
