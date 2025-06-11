ARG BUILD_ENV=prod

# Base Stage for general settings that never change
FROM mcr.microsoft.com/dotnet/aspnet:9.0@sha256:1e12c265e1e1b3714c5805ab0cab63380eb687b0a04f3b3ef3392494a6122614 AS aspnet
WORKDIR /app
EXPOSE 8080

# Build Stage with Dotnet SDK
FROM mcr.microsoft.com/dotnet/sdk:9.0@sha256:c5b188baf837b0180a14f988815b9cc7a55b836dd6cbe1b6e6523cf3098faaa8 AS sdk
WORKDIR /src

# Install dev tools
FROM sdk AS sdk_dev
ONBUILD RUN apt-get --no-install-recommends update \
    && apt-get --no-install-recommends install -y unzip  \
    && curl --proto "=https" -sSL https://aka.ms/getvsdbgsh | bash /dev/stdin -v latest -l /remote_debugger \
    && apt-get clean;

FROM sdk AS sdk_prod

# Build Stage with Dotnet SDK
FROM sdk_${BUILD_ENV} AS build
WORKDIR /src

# copy csproj and restore as distinct layers
COPY Directory.Packages.props ./Directory.Packages.props
COPY Core/*.csproj ./Core/
COPY Api/*.csproj ./Api/

# Restore dependencies
RUN dotnet restore Api/Api.csproj

# Copy all source code
COPY Core/ Core/
COPY Api/ Api/

# Publish the app
WORKDIR /src/Api/
RUN dotnet publish -c Release -o /app

# Install dev tools
FROM aspnet AS aspnet_dev
ONBUILD COPY --from=build /remote_debugger /remote_debugger
ONBUILD RUN apt-get --no-install-recommends update \
    && apt-get --no-install-recommends install -y curl  \
    && apt-get clean;

FROM aspnet AS aspnet_prod

# final stage
FROM aspnet_${BUILD_ENV} AS final
WORKDIR /app

COPY --from=build /app .
ENTRYPOINT ["dotnet", "Api.dll"]
USER 1000