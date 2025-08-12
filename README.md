# Exchange Rate Comparison Microservices

A .NET microservices architecture for comparing exchange rates from multiple providers and finding the best offer. The system aggregates exchange rates from different APIs, compares them, and returns the optimal conversion rate.

## 🚀 Features

- **Multi-Provider Support**: Integrates with multiple exchange rate APIs (JSON and XML)
- **Best Rate Selection**: Automatically identifies the best exchange rate offer
- **Performance Tracking**: Measures response times for each provider
- **Error Handling**: Graceful handling of provider failures
- **Clean Architecture**: Domain-driven design with separation of concerns
- **Dockerized**: Full containerization with Docker Compose
- **Microservices**: Independent, scalable service architecture

## 🏗️ Architecture

```
┌─────────────────┐    ┌─────────────────┐    ┌─────────────────┐
│   MockApi1      │    │   MockApi2      │    │   MockApi3      │
│  (JSON Provider)│    │  (XML Provider) │    │  (JSON Provider)│
│    Port: 8082   │    │    Port: 8083   │    │    Port: 8084   │
└─────────────────┘    └─────────────────┘    └─────────────────┘
         │                       │                       │
         └───────────────────────┼───────────────────────┘
                                 │
                    ┌─────────────────┐
                    │ Exchange Rate   │
                    │    WebAPI       │
                    │  (Aggregator)   │
                    │   Port: 8081    │
                    └─────────────────┘
```

### Services

- **ExchangeRateComparison.WebApi**: Main aggregation service
- **ExchangeRateComparison.Application**: Business logic layer
- **ExchangeRateComparison.Domain**: Domain models and interfaces
- **ExchangeRateComparison.Infrastructure**: External service integrations
- **MockApi1.JsonProvider**: Mock JSON exchange rate provider
- **MockApi2.XmlProvider**: Mock XML exchange rate provider
- **MockApi3.JsonProvider**: Mock JSON exchange rate provider

## 🛠️ Technology Stack

- **.NET 8.0**: Framework
- **ASP.NET Core**: Web API
- **Docker**: Containerization
- **Docker Compose**: Service orchestration
- **Clean Architecture**: Design pattern
- **Domain-Driven Design**: Architecture approach

## 📦 Quick Start

### Prerequisites

- Docker Desktop
- .NET 8.0 SDK (for local development)

### 1. Clone and Build

```bash
git clone <repository-url>
cd ExchangeRateComparison
```

### 2. Run with Docker Compose

```bash
# Build and start all services
docker-compose up --build

# Or run in background
docker-compose up -d --build
```

### 3. Verify Services

Services will be available at:
- **Main API**: http://localhost:8081
- **MockApi1**: http://localhost:8082
- **MockApi2**: http://localhost:8083
- **MockApi3**: http://localhost:8084

## 🐳 Docker Configuration

### docker-compose.yml

```yaml
version: '3.8'

services:
  exchangerate-webapi:
    build:
      context: ./ExchangeRateComparison
      dockerfile: ExchangeRateComparison.WebApi/Dockerfile
    ports:
      - "8081:8080"
    environment:
      - ASPNETCORE_ENVIRONMENT=Development
      - ASPNETCORE_URLS=http://+:8080
      - ASPNETCORE_HTTP_PORTS=8080
      - MockApi1Url=http://mockapi1-json:8080
      - MockApi2Url=http://mockapi2-xml:8080
      - MockApi3Url=http://mockapi3-json:8080
    depends_on:
      - mockapi1-json
      - mockapi2-xml
      - mockapi3-json
    networks:
      - exchange-rate-network

  mockapi1-json:
    build:
      context: ./ExchangeRateComparison
      dockerfile: MockApi1.JsonProvider/Dockerfile
    ports:
      - "8082:8080"
    environment:
      - ASPNETCORE_ENVIRONMENT=Development
      - ASPNETCORE_URLS=http://+:8080
      - ASPNETCORE_HTTP_PORTS=8080
    networks:
      - exchange-rate-network

  mockapi2-xml:
    build:
      context: ./ExchangeRateComparison
      dockerfile: MockApi2.XmlProvider/Dockerfile
    ports:
      - "8083:8080"
    environment:
      - ASPNETCORE_ENVIRONMENT=Development
      - ASPNETCORE_URLS=http://+:8080
      - ASPNETCORE_HTTP_PORTS=8080
    networks:
      - exchange-rate-network

  mockapi3-json:
    build:
      context: ./ExchangeRateComparison
      dockerfile: MockApi3.JsonProvider/Dockerfile
    ports:
      - "8084:8080"
    environment:
      - ASPNETCORE_ENVIRONMENT=Development
      - ASPNETCORE_URLS=http://+:8080
      - ASPNETCORE_HTTP_PORTS=8080
    networks:
      - exchange-rate-network

networks:
  exchange-rate-network:
    driver: bridge

volumes:
  exchange-rate-data:
```

### Individual Dockerfiles

Each service has its own Dockerfile following the standard .NET multi-stage build pattern:

```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["ServiceName/ServiceName.csproj", "ServiceName/"]
RUN dotnet restore "./ServiceName/ServiceName.csproj"
COPY . .
WORKDIR "/src/ServiceName"
RUN dotnet build "./ServiceName.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./ServiceName.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ServiceName.dll"]
```

## 📚 API Documentation

### Exchange Rate Comparison Endpoint

**GET** `/api/exchange-rate/compare`

#### Parameters

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| sourceCurrency | string | Yes | Source currency code (e.g., "USD") |
| targetCurrency | string | Yes | Target currency code (e.g., "EUR") |
| amount | decimal | Yes | Amount to convert |

#### Example Request

```bash
curl "http://localhost:8081/api/exchange-rate/compare?sourceCurrency=MFQ&targetCurrency=YHN&amount=1000000000"
```

#### Example Response

```json
{
  "status": 1,
  "input": {
    "sourceCurrency": "MFQ",
    "targetCurrency": "YHN",
    "amount": 1000000000
  },
  "bestOffer": {
    "providerName": "API3",
    "convertedAmount": 1170146679.817767,
    "exchangeRate": 1.170146679817767,
    "responseTime": "2025-08-12T02:22:39.0100187Z",
    "isSuccessful": true,
    "errorMessage": null,
    "responseDuration": "00:00:00.5273589"
  },
  "allOffers": [
    {
      "providerName": "API1",
      "convertedAmount": 1026317810.7451265,
      "exchangeRate": 1.0263178107451265,
      "responseTime": "2025-08-12T02:22:38.7231981Z",
      "isSuccessful": true,
      "errorMessage": null,
      "responseDuration": "00:00:00.2805969"
    },
    {
      "providerName": "API2",
      "convertedAmount": 1083523743.87,
      "exchangeRate": 1.08352374387,
      "responseTime": "2025-08-12T02:22:38.7844452Z",
      "isSuccessful": true,
      "errorMessage": null,
      "responseDuration": "00:00:00.3157016"
    },
    {
      "providerName": "API3",
      "convertedAmount": 1170146679.817767,
      "exchangeRate": 1.170146679817767,
      "responseTime": "2025-08-12T02:22:39.0100187Z",
      "isSuccessful": true,
      "errorMessage": null,
      "responseDuration": "00:00:00.5273589"
    }
  ],
  "processedAt": "2025-08-12T02:22:39.0115406Z",
  "processingDuration": "00:00:00.5741232",
  "hasValidOffers": true,
  "successfulOffersCount": 3,
  "failedOffersCount": 0,
  "successfulOffers": [
    {
      "providerName": "API3",
      "convertedAmount": 1170146679.817767,
      "exchangeRate": 1.170146679817767,
      "responseTime": "2025-08-12T02:22:39.0100187Z",
      "isSuccessful": true,
      "errorMessage": null,
      "responseDuration": "00:00:00.5273589"
    },
    {
      "providerName": "API2",
      "convertedAmount": 1083523743.87,
      "exchangeRate": 1.08352374387,
      "responseTime": "2025-08-12T02:22:38.7844452Z",
      "isSuccessful": true,
      "errorMessage": null,
      "responseDuration": "00:00:00.3157016"
    },
    {
      "providerName": "API1",
      "convertedAmount": 1026317810.7451265,
      "exchangeRate": 1.0263178107451265,
      "responseTime": "2025-08-12T02:22:38.7231981Z",
      "isSuccessful": true,
      "errorMessage": null,
      "responseDuration": "00:00:00.2805969"
    }
  ],
  "failedOffers": []
}
```

#### Response Fields

| Field | Description |
|-------|-------------|
| status | Response status (1 = success, 0 = error) |
| input | Original request parameters |
| bestOffer | Provider with the highest exchange rate |
| allOffers | Complete list of all provider responses |
| successfulOffers | Providers that returned valid rates (sorted by rate desc) |
| failedOffers | Providers that failed to respond |
| hasValidOffers | Whether any providers returned valid rates |
| successfulOffersCount | Number of successful provider responses |
| failedOffersCount | Number of failed provider responses |
| processedAt | When the aggregation completed |
| processingDuration | Total time to process all providers |

## 🔧 Development

### Running Locally

```bash
# Start dependencies
docker-compose up mockapi1-json mockapi2-xml mockapi3-json

# Run main API locally
cd ExchangeRateComparison/ExchangeRateComparison.WebApi
dotnet run
```

### Environment Variables

| Variable | Default | Description |
|----------|---------|-------------|
| MockApi1Url | http://localhost:8082 | MockApi1 endpoint |
| MockApi2Url | http://localhost:8083 | MockApi2 endpoint |
| MockApi3Url | http://localhost:8084 | MockApi3 endpoint |
| ASPNETCORE_ENVIRONMENT | Development | Runtime environment |

## 🐛 Troubleshooting

### Port Conflicts

If you get "port already in use" errors:

```bash
# Stop all containers
docker-compose down --remove-orphans

# Clean docker system
docker system prune

# On macOS, disable AirPlay Receiver (uses port 5000)
# System Preferences > Sharing > AirPlay Receiver

# Restart services
docker-compose up --build
```

### Service Communication Issues

Check that services can communicate:

```bash
# Test individual services
curl http://localhost:8082/health
curl http://localhost:8083/health  
curl http://localhost:8084/health

# Check container networking
docker network ls
docker network inspect exchangeratecomparison_exchange-rate-network
```

### Logs

View service logs:

```bash
# All services
docker-compose logs -f

# Specific service
docker-compose logs -f exchangerate-webapi
docker-compose logs -f mockapi1-json
```

## 📈 Performance

The system includes built-in performance tracking:
- **Response Times**: Each provider's response time is measured
- **Processing Duration**: Total aggregation time
- **Success Rates**: Track provider reliability
- **Best Rate Selection**: Automatic optimization for best exchange rates

---

**Success Response Example**: The system successfully aggregated rates from all 3 providers, with API3 offering the best rate of 1.170146679817767 for converting 1,000,000,000 MFQ to YHN.