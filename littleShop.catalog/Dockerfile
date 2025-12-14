FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER app
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

COPY ["Directory.Packages.props", "."]

COPY ["littleShop.catalog/littleShop.catalog.csproj", "littleShop.catalog/"]
COPY ["littleShop.Shared/littleShop.Shared.csproj", "littleShop.Shared/"]
COPY ["littleshop.serviceDefaults/littleshop.serviceDefaults.csproj", "littleshop.serviceDefaults/"]

RUN dotnet restore "./littleShop.catalog/littleShop.catalog.csproj"

COPY . .
WORKDIR "/src/littleShop.catalog"
RUN dotnet build "./littleShop.catalog.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./littleShop.catalog.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "littleShop.catalog.dll"]