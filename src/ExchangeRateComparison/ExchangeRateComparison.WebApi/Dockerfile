FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["ExchangeRateComparison.WebApi/ExchangeRateComparison.WebApi.csproj", "ExchangeRateComparison.WebApi/"]
COPY ["ExchangeRateComparison.Application/ExchangeRateComparison.Application.csproj", "ExchangeRateComparison.Application/"]
COPY ["ExchangeRateComparison.Domain/ExchangeRateComparison.Domain.csproj", "ExchangeRateComparison.Domain/"]
COPY ["ExchangeRateComparison.Infrastructure/ExchangeRateComparison.Infrastructure.csproj", "ExchangeRateComparison.Infrastructure/"]
RUN dotnet restore "./ExchangeRateComparison.WebApi/ExchangeRateComparison.WebApi.csproj"
COPY . .
WORKDIR "/src/ExchangeRateComparison.WebApi"
RUN dotnet build "./ExchangeRateComparison.WebApi.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./ExchangeRateComparison.WebApi.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ExchangeRateComparison.WebApi.dll"]