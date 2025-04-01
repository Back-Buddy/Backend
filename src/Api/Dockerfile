ARG BUILD_ENV=prod

# Base Stage for general settings that never change
FROM mcr.microsoft.com/dotnet/aspnet:9.0@sha256:c013be6e8c5219fa56002ad96aac9d99afcca23a185aadd0a30e9b4f3d6efd8c AS aspnet
WORKDIR /app
EXPOSE 8080

# Build Stage with Dotnet SDK
FROM mcr.microsoft.com/dotnet/sdk:9.0@sha256:4845ef954a33b55c1a1f5db1ac24ba6cedb1dafb7f0b6a64ebce2fabe611f0c0 AS sdk
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
COPY Api/*.csproj ./Api/

# Restore dependencies
RUN dotnet restore Api/Api.csproj

# Copy all source code
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