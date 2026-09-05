# Build context is the `backend/` folder (see docker-compose.yml `context: .`).

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY Directory.Build.props .
COPY src/Services/Payment/Payment.Api/Payment.Api.csproj src/Services/Payment/Payment.Api/
COPY src/Services/Payment/Payment.Application/Payment.Application.csproj src/Services/Payment/Payment.Application/
COPY src/Services/Payment/Payment.Domain/Payment.Domain.csproj src/Services/Payment/Payment.Domain/
COPY src/Services/Payment/Payment.Infrastructure/Payment.Infrastructure.csproj src/Services/Payment/Payment.Infrastructure/
COPY src/BuildingBlocks/Shared.Contracts/Shared.Contracts.csproj src/BuildingBlocks/Shared.Contracts/
COPY src/BuildingBlocks/Shared.Infrastructure/Shared.Infrastructure.csproj src/BuildingBlocks/Shared.Infrastructure/

RUN dotnet restore src/Services/Payment/Payment.Api/Payment.Api.csproj

COPY src/Services/Payment/ src/Services/Payment/
COPY src/BuildingBlocks/ src/BuildingBlocks/

RUN dotnet publish src/Services/Payment/Payment.Api/Payment.Api.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app .

EXPOSE 8080
ENTRYPOINT ["dotnet", "Payment.Api.dll"]
