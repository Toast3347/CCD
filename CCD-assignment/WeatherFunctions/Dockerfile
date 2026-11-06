# Azure Functions .NET 10 isolated runtime
FROM mcr.microsoft.com/azure-functions/dotnet-isolated:4-dotnet-isolated10.0 AS base

WORKDIR /home/site/wwwroot
EXPOSE 80

# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

ARG BUILD_CONFIGURATION=Release

WORKDIR /src

COPY ["WeatherFunctions/WeatherFunctions.csproj", "WeatherFunctions/"]

RUN dotnet restore "WeatherFunctions/WeatherFunctions.csproj"

COPY . .

WORKDIR /src/WeatherFunctions

RUN dotnet publish "WeatherFunctions.csproj" \
    --configuration $BUILD_CONFIGURATION \
    --output /app/publish \
    /p:UseAppHost=false

# Final stage
FROM base AS final

WORKDIR /home/site/wwwroot

COPY --from=build /app/publish .

ENV AzureWebJobsScriptRoot=/home/site/wwwroot
ENV AzureFunctionsJobHost__Logging__Console__IsEnabled=true