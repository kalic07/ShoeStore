# Build context is the `backend/` folder (see docker-compose.yml `context: .`).

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY Directory.Build.props .
COPY src/ApiGateway/ApiGateway.csproj src/ApiGateway/
COPY src/BuildingBlocks/Shared.Contracts/Shared.Contracts.csproj src/BuildingBlocks/Shared.Contracts/
COPY src/BuildingBlocks/Shared.Infrastructure/Shared.Infrastructure.csproj src/BuildingBlocks/Shared.Infrastructure/

RUN dotnet restore src/ApiGateway/ApiGateway.csproj

COPY src/ApiGateway/ src/ApiGateway/
COPY src/BuildingBlocks/ src/BuildingBlocks/

RUN dotnet publish src/ApiGateway/ApiGateway.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app .

EXPOSE 8080
ENTRYPOINT ["dotnet", "ApiGateway.dll"]
