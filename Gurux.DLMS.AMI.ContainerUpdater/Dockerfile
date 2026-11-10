# Build from the AMI repository root.
FROM docker:cli AS docker-cli
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY Gurux.DLMS.AMI.ContainerUpdater/Gurux.DLMS.AMI.ContainerUpdater.csproj Gurux.DLMS.AMI.ContainerUpdater/
RUN --mount=type=cache,id=ami-nuget,target=/root/.nuget/packages,sharing=locked dotnet restore Gurux.DLMS.AMI.ContainerUpdater/Gurux.DLMS.AMI.ContainerUpdater.csproj
COPY Gurux.DLMS.AMI.ContainerUpdater/ Gurux.DLMS.AMI.ContainerUpdater/
RUN --mount=type=cache,id=ami-nuget,target=/root/.nuget/packages,sharing=locked dotnet publish Gurux.DLMS.AMI.ContainerUpdater/Gurux.DLMS.AMI.ContainerUpdater.csproj -c Release --no-restore -o /out -p:UseAppHost=false
FROM mcr.microsoft.com/dotnet/runtime:10.0
COPY --from=docker-cli /usr/local/bin/docker /usr/local/bin/docker
COPY --from=docker-cli /usr/local/libexec/docker/cli-plugins/ /usr/local/libexec/docker/cli-plugins/
COPY --from=build /out/ /updater/
WORKDIR /project
HEALTHCHECK --interval=5s --timeout=5s --start-period=30s --retries=3 CMD ["dotnet", "/updater/Gurux.DLMS.AMI.ContainerUpdater.dll", "--health-check"]
ENTRYPOINT ["dotnet", "/updater/Gurux.DLMS.AMI.ContainerUpdater.dll"]
