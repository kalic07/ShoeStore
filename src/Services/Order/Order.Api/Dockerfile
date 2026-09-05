# Build context is the `backend/` folder (see docker-compose.yml `context: .`).

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY Directory.Build.props .
COPY src/Services/Order/Order.Api/Order.Api.csproj src/Services/Order/Order.Api/
COPY src/Services/Order/Order.Application/Order.Application.csproj src/Services/Order/Order.Application/
COPY src/Services/Order/Order.Domain/Order.Domain.csproj src/Services/Order/Order.Domain/
COPY src/Services/Order/Order.Infrastructure/Order.Infrastructure.csproj src/Services/Order/Order.Infrastructure/
COPY src/BuildingBlocks/Shared.Contracts/Shared.Contracts.csproj src/BuildingBlocks/Shared.Contracts/
COPY src/BuildingBlocks/Shared.Infrastructure/Shared.Infrastructure.csproj src/BuildingBlocks/Shared.Infrastructure/

RUN dotnet restore src/Services/Order/Order.Api/Order.Api.csproj

COPY src/Services/Order/ src/Services/Order/
COPY src/BuildingBlocks/ src/BuildingBlocks/

RUN dotnet publish src/Services/Order/Order.Api/Order.Api.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app .

EXPOSE 8080
ENTRYPOINT ["dotnet", "Order.Api.dll"]
