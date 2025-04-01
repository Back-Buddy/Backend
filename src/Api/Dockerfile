ARG BUILD_ENV=prod

# Base Stage for general settings that never change
FROM mcr.microsoft.com/dotnet/aspnet:9.0@sha256:4f0ad314f83e6abeb6906e69d0f9c81a0d2ee51d362e035c7d3e6ac5743f5399 AS aspnet
WORKDIR /app
EXPOSE 8080

# Build Stage with Dotnet SDK
FROM mcr.microsoft.com/dotnet/sdk:9.0@sha256:d7f4691d11f610d9b94bb75517c9e78ac5799447b5b3e82af9e4625d8c8d1d53 AS sdk
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