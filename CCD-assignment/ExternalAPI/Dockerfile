FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

WORKDIR /src

# Copy project file
COPY ["ExternalAPI.csproj", "./"]

# Restore dependencies
RUN dotnet restore "ExternalAPI.csproj"

# Copy remaining source files
COPY . .

# Publish application
RUN dotnet publish "ExternalAPI.csproj" \
    --configuration Release \
    --output /app/publish \
    /p:UseAppHost=false

# Runtime image
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final

WORKDIR /app

COPY --from=build /app/publish .

EXPOSE 8080

ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["dotnet", "ExternalAPI.dll"]