# Usamos la imagen base de ASP.NET Core
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER app
WORKDIR /app

# Usamos la imagen del SDK para compilar
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# 1. COPIAR PAQUETES GLOBALES
COPY ["Directory.Packages.props", "."]

# 2. COPIAR PROYECTOS (MANTENIENDO ESTRUCTURA DE CARPETAS)
# Copiamos cada csproj a su carpeta correspondiente
COPY ["littleShop.notifications/littleShop.notifications.csproj", "littleShop.notifications/"]
COPY ["littleShop.Shared/littleShop.Shared.csproj", "littleShop.Shared/"]
# OJO: Copiamos serviceDefaults a la carpeta con mayúsculas que espera el csproj
COPY ["littleshop.serviceDefaults/littleshop.serviceDefaults.csproj", "littleShop.serviceDefaults/"]

# 3. RESTAURAR DEPENDENCIAS (Desde la carpeta del proyecto principal)
RUN dotnet restore "./littleShop.notifications/littleShop.notifications.csproj"

# 4. COPIAR TODO EL CÓDIGO FUENTE
# Copiamos todo lo de cada proyecto a su sitio
COPY ["littleShop.notifications/", "littleShop.notifications/"]
COPY ["littleShop.Shared/", "littleShop.Shared/"]
COPY ["littleshop.serviceDefaults/", "littleShop.serviceDefaults/"]

# 5. COMPILAR
# Nos movemos a la carpeta del proyecto para que las rutas relativas (../) funcionen
WORKDIR "/src/littleShop.notifications"
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