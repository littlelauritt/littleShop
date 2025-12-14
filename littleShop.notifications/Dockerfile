# Usamos la imagen base de ASP.NET Core
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER app
WORKDIR /app

# Usamos la imagen del SDK para compilar
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# 1. COPIAR SOLO EL ARCHIVO DE PAQUETES QUE SÍ TIENES
COPY ["Directory.Packages.props", "."]

# 2. COPIAR LOS CSPROJ
COPY ["littleShop.notifications/littleShop.notifications.csproj", "littleShop.notifications/"]
COPY ["littleShop.Shared/littleShop.Shared.csproj", "littleShop.Shared/"]

# COPIAR EL SERVICE DEFAULTS EN LA CARPETA CORRECTA (Con Mayúsculas)
COPY ["littleshop.serviceDefaults/littleshop.serviceDefaults.csproj", "littleShop.serviceDefaults/"]

# 3. RESTAURAR DEPENDENCIAS
RUN dotnet restore "./littleShop.notifications/littleShop.notifications.csproj"

# 4. COPIAR EL RESTO DEL CÓDIGO
COPY . .

# COPIAR EL CÓDIGO FUENTE DE SERVICE DEFAULTS A LA CARPETA CON MAYÚSCULAS
COPY ["littleshop.serviceDefaults/", "littleShop.serviceDefaults/"]

WORKDIR "/src/littleShop.notifications"

# 5. COMPILAR
RUN dotnet build "./littleShop.notifications.csproj" -c $BUILD_CONFIGURATION -o /app/build

# 6. PUBLICAR
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./littleShop.notifications.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# 7. IMAGEN FINAL
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "littleShop.notifications.dll"]