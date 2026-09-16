# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /app

# Copy csproj files and restore dependencies
COPY ["src/Domain/Spot.Domain.csproj", "src/Domain/"]
COPY ["src/Infrastructure/Spot.Infrastructure.csproj", "src/Infrastructure/"]
COPY ["src/Api/Spot.Api.csproj", "src/Api/"]

RUN dotnet restore "src/Api/Spot.Api.csproj"

# Copy source code and build Release
COPY src/ src/
WORKDIR "/app/src/Api"
RUN dotnet publish "Spot.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Stage 2: Production Runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENV DOTNET_EnableDiagnostics=0

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Spot.Api.dll"]
