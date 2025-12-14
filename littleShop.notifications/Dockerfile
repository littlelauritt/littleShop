# Usamos la imagen base de ASP.NET Core
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER app
WORKDIR /app

# Usamos la imagen del SDK para compilar
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# 1. COPIAR LOS CSPROJ (¡Aquí faltaba el serviceDefaults!)
COPY ["littleShop.notifications/littleShop.notifications.csproj", "littleShop.notifications/"]
COPY ["littleShop.Shared/littleShop.Shared.csproj", "littleShop.Shared/"]
# 👇 ESTA ES LA LÍNEA QUE TE FALTABA 👇
COPY ["littleshop.serviceDefaults/littleshop.serviceDefaults.csproj", "littleshop.serviceDefaults/"]

# 2. RESTAURAR DEPENDENCIAS
RUN dotnet restore "./littleShop.notifications/littleShop.notifications.csproj"

# 3. COPIAR EL RESTO DEL CÓDIGO
COPY . .
WORKDIR "/src/littleShop.notifications"

# 4. COMPILAR
RUN dotnet build "./littleShop.notifications.csproj" -c $BUILD_CONFIGURATION -o /app/build

# 5. PUBLICAR
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./littleShop.notifications.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# 6. IMAGEN FINAL
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "littleShop.notifications.dll"]