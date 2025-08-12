FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["MockApi2.XmlProvider/MockApi2.XmlProvider.csproj", "MockApi2.XmlProvider/"]
RUN dotnet restore "./MockApi2.XmlProvider/MockApi2.XmlProvider.csproj"
COPY . .
WORKDIR "/src/MockApi2.XmlProvider"
RUN dotnet build "./MockApi2.XmlProvider.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./MockApi2.XmlProvider.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "MockApi2.XmlProvider.dll"]