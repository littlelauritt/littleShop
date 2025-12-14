# Usamos la imagen base de ASP.NET Core
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER app
WORKDIR /app

# Usamos la imagen del SDK para compilar
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# 1. COPIAR PAQUETES
COPY ["Directory.Packages.props", "."]

# 2. COPIAR PROYECTOS (AQUÍ ESTÁ EL TRUCO)
# Copiamos el archivo .csproj de notifications
COPY ["littleShop.notifications/littleShop.notifications.csproj", "littleShop.notifications/"]
COPY ["littleShop.Shared/littleShop.Shared.csproj", "littleShop.Shared/"]

# ⚠️ CORRECCIÓN CRÍTICA:
# Cogemos el archivo de tu disco (littleshop... minúscula) 
# Y lo guardamos en Docker COMO littleShop... (MAYÚSCULA) para que coincida con la referencia.
COPY ["littleshop.serviceDefaults/littleshop.serviceDefaults.csproj", "littleShop.serviceDefaults/littleShop.serviceDefaults.csproj"]

# 3. RESTAURAR DEPENDENCIAS
RUN dotnet restore "./littleShop.notifications/littleShop.notifications.csproj"

# 4. COPIAR EL CÓDIGO FUENTE
# Copiamos el código de notificaciones y shared
COPY ["littleShop.notifications/", "littleShop.notifications/"]
COPY ["littleShop.Shared/", "littleShop.Shared/"]

# ⚠️ CORRECCIÓN CRÍTICA 2:
# Copiamos todo el código de serviceDefaults en la carpeta CON MAYÚSCULAS
COPY ["littleshop.serviceDefaults/", "littleShop.serviceDefaults/"]

# 5. COMPILAR
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