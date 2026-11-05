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
USER $APP_UID

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Gurux.DLMS.AMI.Agent/Gurux.DLMS.AMI.Agent.csproj", "Gurux.DLMS.AMI.Agent/"]
COPY ["Gurux.DLMS.AMI.Http/Gurux.DLMS.AMI.Http.csproj", "Gurux.DLMS.AMI.Http/"]
COPY ["Gurux.DLMS.AMI.Shared/Gurux.DLMS.AMI.Shared.csproj", "Gurux.DLMS.AMI.Shared/"]
RUN dotnet restore Gurux.DLMS.AMI.Agent/Gurux.DLMS.AMI.Agent.csproj
COPY Gurux.DLMS.AMI.Agent/ Gurux.DLMS.AMI.Agent/
COPY Gurux.DLMS.AMI.Http/ Gurux.DLMS.AMI.Http/
COPY Gurux.DLMS.AMI.Shared/ Gurux.DLMS.AMI.Shared/
RUN dotnet publish Gurux.DLMS.AMI.Agent/Gurux.DLMS.AMI.Agent.csproj -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
COPY --from=build --chown=app:app /app/publish .
USER root
RUN chown app:app /app
USER $APP_UID
ENTRYPOINT ["dotnet", "Gurux.DLMS.AMI.Agent.dll"]
