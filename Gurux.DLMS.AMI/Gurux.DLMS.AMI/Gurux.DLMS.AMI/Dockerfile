# --------------------------------------------------------------------------
# Gurux Ltd
#
# Copyright (c) Gurux Ltd.
#
# This file is part of Gurux Device Framework.
#
# Commit: $Format:%H$
# Date:   $Format:%cI$
#
# This software is available under either of the following licenses:
#
# 1. GNU General Public License, version 2 only (GPL-2.0-only).
#    See the LICENSE file included with this distribution.
#
# 2. A commercial license granted by Gurux Ltd under a separate written
#    agreement. Use under this option requires a valid commercial license
#    and is governed by the terms of that agreement.
#
# If you have not obtained a commercial license from Gurux Ltd,
# use of this software is governed by GPL-2.0-only.
#
# Commercial licensing: https://www.gurux.fi/
#
# SPDX-License-Identifier: GPL-2.0-only OR LicenseRef-Gurux-Commercial
# --------------------------------------------------------------------------

# Build with the repository root as context.
# The base stage is also used by Visual Studio fast-mode debugging.
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
RUN mkdir -p /data /app/Modules && chown app:app /app /data /app/Modules
ENV ASPNETCORE_ENVIRONMENT=Production \
    ASPNETCORE_HTTP_PORTS=8080 \
    Database__Type=SQLite \
    Database__Settings="Data Source=/data/ami.db;Foreign Keys=True" \
    HOME=/data \
    XDG_DATA_HOME=/data \
    ImageStorage__RootPath=/data/images
EXPOSE 8080
EXPOSE 8081
USER $APP_UID

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Gurux.DLMS.AMI/Gurux.DLMS.AMI/Gurux.DLMS.AMI/Gurux.DLMS.AMI.csproj", "Gurux.DLMS.AMI/Gurux.DLMS.AMI/Gurux.DLMS.AMI/"]
COPY ["Gurux.DLMS.AMI/Gurux.DLMS.AMI/Gurux.DLMS.AMI.Client/Gurux.DLMS.AMI.Client.csproj", "Gurux.DLMS.AMI/Gurux.DLMS.AMI/Gurux.DLMS.AMI.Client/"]
RUN --mount=type=cache,id=ami-nuget,target=/root/.nuget/packages,sharing=locked \
    dotnet restore "Gurux.DLMS.AMI/Gurux.DLMS.AMI/Gurux.DLMS.AMI/Gurux.DLMS.AMI.csproj" -p:Configuration=$BUILD_CONFIGURATION -p:AmiReleaseBuild=true
COPY Gurux.DLMS.AMI/Gurux.DLMS.AMI/Gurux.DLMS.AMI/ Gurux.DLMS.AMI/Gurux.DLMS.AMI/Gurux.DLMS.AMI/
COPY Gurux.DLMS.AMI/Gurux.DLMS.AMI/Gurux.DLMS.AMI.Client/ Gurux.DLMS.AMI/Gurux.DLMS.AMI/Gurux.DLMS.AMI.Client/
COPY README.md README.md

FROM build AS publish
RUN --mount=type=cache,id=ami-nuget,target=/root/.nuget/packages,sharing=locked \
    dotnet publish "Gurux.DLMS.AMI/Gurux.DLMS.AMI/Gurux.DLMS.AMI/Gurux.DLMS.AMI.csproj" -c $BUILD_CONFIGURATION --no-restore -o /app/publish -p:UseAppHost=false -p:AmiReleaseBuild=true

FROM base AS final
COPY --from=publish --chown=app:app /app/publish/ ./
HEALTHCHECK --interval=10s --timeout=8s --start-period=60s --retries=3 CMD ["dotnet", "Gurux.DLMS.AMI.dll", "--health-check"]
ENTRYPOINT ["dotnet", "Gurux.DLMS.AMI.dll"]
