# littleShop 🛒

**littleShop** es una plataforma de comercio electrónico moderna y distribuida, diseñada bajo una arquitectura de **Microservicios**. Este proyecto sirve como una implementación de referencia utilizando **tecnologías de última generación** del ecosistema Microsoft, incluyendo **.NET 10** y **.NET Aspire**, junto con un frontend reactivo de alto rendimiento.

## 🚀 Características Principales

* **Arquitectura Desacoplada**: Servicios independientes para Identidad, Catálogo, Pedidos y Notificaciones.
* **Orquestación Nativa**: Uso de **.NET Aspire** para la gestión de recursos, configuración y ejecución del entorno local.
* **Frontend Moderno**: SPA construida con **React 18**, **TypeScript** y **Vite**.
* **Comunicación Asíncrona**: Implementación de mensajería basada en eventos usando **RabbitMQ** y **MassTransit**.
* **API Gateway**: Uso de **YARP** como punto de entrada único.
* **Documentación API**: Integración con **Scalar** para una experiencia de documentación superior a Swagger.
* **Pruebas de Rendimiento**: Scripts de carga incluidos con **k6**.
* **Infraestructura Local**: Contenedores gestionados automáticamente para Postgres, Redis y MailDev.

## 🛠 Stack Tecnológico

### Backend & Cloud Native
* **Framework**: .NET 10 (C#)
* **Orquestación**: .NET Aspire
* **Base de Datos**: PostgreSQL (con Entity Framework Core 10)
* **Caché**: Redis
* **Mensajería**: RabbitMQ (gestionado con MassTransit)
* **API Gateway**: YARP (Reverse Proxy)
* **Validación**: FluentValidation
* **Documentación**: Scalar / OpenAPI
* **Observabilidad**: OpenTelemetry

### Frontend
* **Framework**: React 18
* **Lenguaje**: TypeScript
* **Build Tool**: Vite
* **Estilos/UI**: React Bootstrap & Bootstrap 5
* **Routing**: React Router DOM

### Herramientas de Desarrollo y Testing
* **MailDev**: Servidor SMTP simulado para interceptar emails en desarrollo (Dashboard en puerto 1080).
* **PgAdmin**: Interfaz gráfica para gestión de PostgreSQL.
* **Redis Insight**: Interfaz gráfica para gestión de Redis.
* **k6**: Herramienta para pruebas de carga y rendimiento.

## 🏗 Arquitectura del Sistema

La solución se compone de los siguientes servicios orquestados por el proyecto `littleShop` (AppHost):

| Servicio | Responsabilidad | Dependencias |
| :--- | :--- | :--- |
| **littleshop.frontend** | Interfaz de usuario (SPA) | Consume API Gateway |
| **littleshop.apiGateway** | Enrutamiento y unificación de APIs | Redis |
| **littleshop.identity** | Autenticación (JWT), Usuarios y Roles | Postgres, RabbitMQ |
| **littleshop.catalog** | Gestión de productos e inventario | Postgres, RabbitMQ |
| **littleshop.orders** | Gestión y procesamiento de pedidos | Postgres, RabbitMQ, Catalog Service |
| **littleshop.notifications** | Envío de correos y alertas | RabbitMQ, MailDev |

## 📋 Prerrequisitos

Para ejecutar este proyecto, asegúrate de tener instalado:

1.  **[.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)**.
2.  **[Docker Desktop](https://www.docker.com/products/docker-desktop)** (Debe estar ejecutándose).
3.  **[Node.js](https://nodejs.org/)** (v18+ recomendado).
4.  **Visual Studio 2022** (Versión Preview/Latest recomendada) o **VS Code**.
5.  *(Opcional)* **[k6](https://k6.io/docs/get-started/installation/)** si deseas ejecutar las pruebas de carga.

## 🚀 Cómo empezar (Quick Start)

Gracias a .NET Aspire, no necesitas configurar cadenas de conexión manuales ni levantar `docker-compose` a mano.

1.  **Clonar el repositorio:**
    ```bash
    git clone [https://github.com/littlelauritt/littleShop.git](https://github.com/littlelauritt/littleShop.git)
    cd littleShop
    ```

2.  **Ejecutar la solución:**
    * Abre `littleShop.sln` en Visual Studio.
    * Establece el proyecto **`littleShop`** (el que contiene el `AppHost`) como *Startup Project*.
    * Presiona `F5`.

3.  **Aspire Dashboard:**
    * Se abrirá automáticamente el **Dashboard de .NET Aspire** en tu navegador.
    * Desde aquí podrás ver el estado de los servicios, logs, métricas y los endpoints.
    * **Frontend**: Busca el endpoint de `littleshop-frontend` para ver la web.
    * **Emails**: Busca el endpoint de `maildev` (dashboard) para ver los correos interceptados.

4.  **Acceso a Documentación de API:**
    * Cada microservicio expone su documentación en `/scalar/v1`.

## 📉 Pruebas de Carga (k6)

El proyecto incluye un script de pruebas de rendimiento (`load-test.js`) en la raíz para simular tráfico y verificar la estabilidad de los microservicios bajo carga.

Para ejecutar la prueba (asegúrate de que la aplicación esté corriendo primero):

```bash
# Si tienes k6 instalado:
k6 run load-test.js
```

## 📂 Estructura de Carpetas

```text
littleShop/
├── littleShop/                  # AppHost (Configuración de Aspire)
├── littleShop.Shared/           # DTOs y utilidades comunes
├── littleShop.identity/         # Auth Service (.NET 10, JWT)
├── littleShop.catalog/          # Product Service
├── littleShop.orders/           # Order Service
├── littleShop.notifications/    # Email Service Worker
├── littleshop.apiGateway/       # YARP Proxy
├── littleshop.frontend/         # React + Vite App
├── littleshop.serviceDefaults/  # Configuración base de OpenTelemetry/HealthChecks
├── load-test.js                 # Script de carga k6
└── Directory.Packages.props     # Gestión centralizada de versiones NuGet
```

## 🤝 Contribución

¡Las contribuciones son bienvenidas!

1.  Haz un Fork del proyecto.
2.  Crea una rama para tu funcionalidad (`git checkout -b feature/AmazingFeature`).
3.  Haz Commit de tus cambios (`git commit -m 'Add some AmazingFeature'`).
4.  Haz Push a la rama (`git push origin feature/AmazingFeature`).
5.  Abre un Pull Request.

## 📄 Licencia

Este proyecto está bajo la Licencia MIT.
