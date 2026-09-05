# Build context is the `backend/` folder (see docker-compose.yml `context: .`)
# so all paths below are relative to backend/.

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY Directory.Build.props .
COPY src/Services/Identity/Identity.Api/Identity.Api.csproj src/Services/Identity/Identity.Api/
COPY src/BuildingBlocks/Shared.Contracts/Shared.Contracts.csproj src/BuildingBlocks/Shared.Contracts/
COPY src/BuildingBlocks/Shared.Infrastructure/Shared.Infrastructure.csproj src/BuildingBlocks/Shared.Infrastructure/

RUN dotnet restore src/Services/Identity/Identity.Api/Identity.Api.csproj

COPY src/Services/Identity/Identity.Api/ src/Services/Identity/Identity.Api/
COPY src/BuildingBlocks/ src/BuildingBlocks/

RUN dotnet publish src/Services/Identity/Identity.Api/Identity.Api.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app .

EXPOSE 8080
ENTRYPOINT ["dotnet", "Identity.Api.dll"]
