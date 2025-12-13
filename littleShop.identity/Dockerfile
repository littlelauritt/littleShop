# 1. Usamos la imagen de .NET 10 Preview
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER app
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

# 2. Fase de compilación
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# 3. COPIAMOS EL GESTOR DE PAQUETES
COPY ["Directory.Packages.props", "."]

# 4. COPIAMOS LOS PROYECTOS
# Fíjate que ahora serviceDefaults está todo en minúscula, tal como me has confirmado
COPY ["littleShop.identity/littleShop.identity.csproj", "littleShop.identity/"]
COPY ["littleShop.Shared/littleShop.Shared.csproj", "littleShop.Shared/"]
COPY ["littleshop.serviceDefaults/littleshop.serviceDefaults.csproj", "littleshop.serviceDefaults/"]

# 5. Restauramos dependencias
RUN dotnet restore "./littleShop.identity/littleShop.identity.csproj"

# 6. Copiamos el resto del código
COPY . .
WORKDIR "/src/littleShop.identity"
RUN dotnet build "./littleShop.identity.csproj" -c $BUILD_CONFIGURATION -o /app/build

# 7. Publicamos
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./littleShop.identity.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# 8. Imagen final
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish ./
ENTRYPOINT ["dotnet", "littleShop.identity.dll"]