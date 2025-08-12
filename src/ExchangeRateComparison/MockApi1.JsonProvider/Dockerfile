FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["MockApi1.JsonProvider/MockApi1.JsonProvider.csproj", "MockApi1.JsonProvider/"]
RUN dotnet restore "./MockApi1.JsonProvider/MockApi1.JsonProvider.csproj"
COPY . .
WORKDIR "/src/MockApi1.JsonProvider"
RUN dotnet build "./MockApi1.JsonProvider.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./MockApi1.JsonProvider.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "MockApi1.JsonProvider.dll"]