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

# Build with repository root as context: docker build -f Gurux.DLMS.AMI.Agent/Dockerfile .
FROM mcr.microsoft.com/dotnet/runtime:10.0 AS base
WORKDIR /app
# Registration and local script compilation write beneath the application directory.
RUN mkdir -p /data && chown app:app /app /data
USER $APP_UID

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Gurux.DLMS.AMI.Agent/Gurux.DLMS.AMI.Agent.csproj", "Gurux.DLMS.AMI.Agent/"]
COPY ["Gurux.DLMS.AMI.Shared/Gurux.DLMS.AMI.Shared.csproj", "Gurux.DLMS.AMI.Shared/"]
COPY ["Gurux.DLMS.AMI.Script/Gurux.DLMS.AMI.Script.csproj", "Gurux.DLMS.AMI.Script/"]
COPY ["Gurux.DLMS.AMI.Script/Gurux.DLMS.AMI.Script.csproj", "Gurux.DLMS.AMI.Script/"]
RUN --mount=type=cache,id=ami-nuget,target=/root/.nuget/packages,sharing=locked \
    dotnet restore "Gurux.DLMS.AMI.Agent/Gurux.DLMS.AMI.Agent.csproj" -p:Configuration=$BUILD_CONFIGURATION
COPY Gurux.DLMS.AMI.Agent/ Gurux.DLMS.AMI.Agent/
COPY Gurux.DLMS.AMI.Shared/ Gurux.DLMS.AMI.Shared/
COPY Gurux.DLMS.AMI.Script/ Gurux.DLMS.AMI.Script/
RUN --mount=type=cache,id=ami-nuget,target=/root/.nuget/packages,sharing=locked \
    dotnet publish "Gurux.DLMS.AMI.Agent/Gurux.DLMS.AMI.Agent.csproj" -c $BUILD_CONFIGURATION --no-restore -o /app/publish -p:UseAppHost=false

FROM base AS final
COPY --from=build --chown=app:app /app/publish/ ./
ENTRYPOINT ["dotnet", "Gurux.DLMS.AMI.Agent.dll"]
