FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER app
WORKDIR /app
EXPOSE 8080
EXPOSE 8081


FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src


COPY ["Directory.Packages.props", "."]

COPY ["littleShop.identity/littleShop.identity.csproj", "littleShop.identity/"]
COPY ["littleShop.Shared/littleShop.Shared.csproj", "littleShop.Shared/"]
COPY ["littleshop.serviceDefaults/littleshop.serviceDefaults.csproj", "littleshop.serviceDefaults/"]

RUN dotnet restore "./littleShop.identity/littleShop.identity.csproj"

COPY . .
WORKDIR "/src/littleShop.identity"
RUN dotnet build "./littleShop.identity.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./littleShop.identity.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish ./
ENTRYPOINT ["dotnet", "littleShop.identity.dll"]