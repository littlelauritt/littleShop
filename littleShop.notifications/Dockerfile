# Usamos la imagen base de ASP.NET Core
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER app
WORKDIR /app

# Usamos la imagen del SDK para compilar
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# 1. COPIAR ARCHIVOS DE GESTIÓN DE PAQUETES (CRÍTICO PARA ERROR NU1015)
COPY ["Directory.Packages.props", "."]
COPY ["Directory.Build.props", "."]

# 2. COPIAR LOS CSPROJ
COPY ["littleShop.notifications/littleShop.notifications.csproj", "littleShop.notifications/"]
COPY ["littleShop.Shared/littleShop.Shared.csproj", "littleShop.Shared/"]

# ⚠️ FIX CRÍTICO: Copiamos a la carpeta 'littleShop.serviceDefaults' (con 'S' mayúscula)
# para que coincida con la referencia que hay dentro de tu .csproj
COPY ["littleshop.serviceDefaults/littleshop.serviceDefaults.csproj", "littleShop.serviceDefaults/"]

# 3. RESTAURAR DEPENDENCIAS
RUN dotnet restore "./littleShop.notifications/littleShop.notifications.csproj"

# 4. COPIAR EL RESTO DEL CÓDIGO
COPY . .

# ⚠️ FIX CRÍTICO 2: Copiamos el código fuente de serviceDefaults a la carpeta con mayúsculas
# Esto asegura que el compilador encuentre los archivos .cs en la ruta que espera Linux
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