# POC Azure Container Apps - Microservices Architecture

## 📋 Descripción

Este proyecto es un POC (Proof of Concept) que implementa un sistema de microservicios en Azure Container Apps, cumpliendo con los siguientes requisitos:

- ✅ Sistema de microservicios en Azure Container Apps
- ✅ 2 frontends en .NET 8.x con acceso desde Internet
- ✅ 2 backends en .NET 8.x (uno para cada frontend)
- ✅ Base de datos Azure SQL
- ✅ Sistema de autenticación y autorización (JWT)
- ✅ Azure Storage Account (Blob Storage)
- ✅ Sistema de monitoreo y logging (Application Insights)

## 🏗️ Arquitectura

```
Internet
   ↓
┌──────────────────────────────────────────────────┐
│     Azure Container Apps Environment             │
│                                                   │
│  ┌─────────────┐              ┌─────────────┐   │
│  │  Tasks Web  │              │  Files Web  │   │
│  │  (Público)  │              │  (Público)  │   │
│  └──────┬──────┘              └──────┬──────┘   │
│         │                            │           │
│         ↓                            ↓           │
│  ┌─────────────┐              ┌─────────────┐   │
│  │  Tasks API  │              │  Files API  │   │
│  │  (Interno)  │              │  (Interno)  │   │
│  └──────┬──────┘              └──────┬──────┘   │
│         │                            │           │
└─────────┼────────────────────────────┼───────────┘
          │                            │
          ↓                            ↓
    ┌──────────┐                ┌──────────────┐
    │ Azure SQL│                │Azure Storage │
    │  TasksDb │                │   FilesDb    │
    │  FilesDb │                │ Blob Storage │
    └──────────┘                └──────────────┘
              ↓
        ┌─────────────────────┐
        │ Application Insights│
        └─────────────────────┘
```

## 📁 Estructura del Proyecto

```
srcs/
├── frontends/
│   ├── TasksWeb/           # Blazor Web App para gestión de tareas
│   └── FilesWeb/           # Blazor Web App para gestión de archivos
├── backends/
│   ├── TasksApi/           # API REST para tareas (CRUD + Auth)
│   └── FilesApi/           # API REST para archivos (Upload/Download + Auth)
├── shared/
│   └── Shared.Models/      # Modelos compartidos (TaskItem, FileMetadata, User)
└── infrastructure/
    └── main.bicep          # Infraestructura como código (IaC)
```

## 🚀 Despliegue

### Prerrequisitos

- Azure CLI instalado
- Docker instalado
- .NET 8 SDK instalado
- Suscripción de Azure activa

### Paso 1: Crear Grupo de Recursos

```bash
az group create --name rg-containerapp-poc --location eastus
```

### Paso 2: Desplegar Infraestructura

```bash
cd srcs/infrastructure
az deployment group create \
  --resource-group rg-containerapp-poc \
  --template-file main.bicep \
  --parameters appName=pocapp
```

Este comando creará:
- Azure Container Registry (ACR)
- Container Apps Environment
- Azure SQL Server con 2 bases de datos (TasksDb, FilesDb)
- Storage Account con contenedor 'files'
- Application Insights
- Log Analytics Workspace
- 4 Container Apps (tasks-api, files-api, tasks-web, files-web)

### Paso 3: Construir y Publicar Imágenes Docker

```bash
# Obtener login server del ACR
ACR_NAME=$(az deployment group show \
  --resource-group rg-containerapp-poc \
  --name main \
  --query properties.outputs.acrLoginServer.value -o tsv)

# Login en ACR
az acr login --name ${ACR_NAME}

# Navegar al directorio srcs
cd ..

# Build y push Tasks API
docker build -f backends/TasksApi/Dockerfile -t ${ACR_NAME}/tasks-api:latest .
docker push ${ACR_NAME}/tasks-api:latest

# Build y push Files API
docker build -f backends/FilesApi/Dockerfile -t ${ACR_NAME}/files-api:latest .
docker push ${ACR_NAME}/files-api:latest

# Build y push Tasks Web
docker build -f frontends/TasksWeb/Dockerfile -t ${ACR_NAME}/tasks-web:latest .
docker push ${ACR_NAME}/tasks-web:latest

# Build y push Files Web
docker build -f frontends/FilesWeb/Dockerfile -t ${ACR_NAME}/files-web:latest .
docker push ${ACR_NAME}/files-web:latest
```

### Paso 4: Ejecutar Migraciones de Base de Datos

```bash
# Obtener SQL Server name
SQL_SERVER=$(az deployment group show \
  --resource-group rg-containerapp-poc \
  --name main \
  --query properties.outputs.sqlServerName.value -o tsv)

# Conectar y ejecutar migraciones para TasksDb
cd backends/TasksApi
dotnet ef migrations add InitialCreate
dotnet ef database update --connection "Server=tcp:${SQL_SERVER},1433;Initial Catalog=TasksDb;User ID=sqladmin;Password=P@ssw0rd123!;Encrypt=True;TrustServerCertificate=False;"

# Ejecutar migraciones para FilesDb
cd ../FilesApi
dotnet ef migrations add InitialCreate
dotnet ef database update --connection "Server=tcp:${SQL_SERVER},1433;Initial Catalog=FilesDb;User ID=sqladmin;Password=P@ssw0rd123!;Encrypt=True;TrustServerCertificate=False;"
```

### Paso 5: Reiniciar Container Apps

```bash
az containerapp restart --name tasks-api --resource-group rg-containerapp-poc
az containerapp restart --name files-api --resource-group rg-containerapp-poc
az containerapp restart --name tasks-web --resource-group rg-containerapp-poc
az containerapp restart --name files-web --resource-group rg-containerapp-poc
```

### Paso 6: Obtener URLs

```bash
# URL de Tasks Web
az deployment group show \
  --resource-group rg-containerapp-poc \
  --name main \
  --query properties.outputs.tasksWebUrl.value -o tsv

# URL de Files Web
az deployment group show \
  --resource-group rg-containerapp-poc \
  --name main \
  --query properties.outputs.filesWebUrl.value -o tsv
```

## 🔐 Autenticación

El sistema usa **JWT (JSON Web Tokens)** para autenticación. Hay 2 usuarios precargados:

| Usuario | Contraseña | Rol   |
|---------|------------|-------|
| admin   | Preimitation{7{!3#   | Admin |
| user    | Chronoscopically$0/(8.    | User  |

## 🧪 Desarrollo Local

### Ejecutar con Docker Compose (Opcional)

Crea un `docker-compose.yml` en la raíz:

```yaml
version: '3.8'
services:
  sqlserver:
    image: mcr.microsoft.com/mssql/server:2022-latest
    environment:
      - ACCEPT_EULA=Y
      - SA_PASSWORD=P@ssw0rd123!
    ports:
      - "1433:1433"

  azurite:
    image: mcr.microsoft.com/azure-storage/azurite
    ports:
      - "10000:10000"
      - "10001:10001"

  tasks-api:
    build:
      context: ./srcs
      dockerfile: backends/TasksApi/Dockerfile
    ports:
      - "5001:8080"
    environment:
      - ConnectionStrings__DefaultConnection=Server=sqlserver;Database=TasksDb;User=sa;Password=P@ssw0rd123!;TrustServerCertificate=True
    depends_on:
      - sqlserver

  files-api:
    build:
      context: ./srcs
      dockerfile: backends/FilesApi/Dockerfile
    ports:
      - "5002:8080"
    environment:
      - ConnectionStrings__DefaultConnection=Server=sqlserver;Database=FilesDb;User=sa;Password=P@ssw0rd123!;TrustServerCertificate=True
      - AzureStorage__ConnectionString=DefaultEndpointsProtocol=http;AccountName=devstoreaccount1;AccountKey=Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==;BlobEndpoint=http://azurite:10000/devstoreaccount1;
    depends_on:
      - sqlserver
      - azurite
```

Ejecutar:
```bash
docker-compose up -d
```

## 📊 Monitoreo

El sistema incluye:
- **Application Insights**: Telemetría, logs y métricas
- **Log Analytics**: Agregación de logs
- **Container Apps Metrics**: CPU, memoria, peticiones HTTP

Acceder al portal de Azure → Application Insights para ver dashboards.

## 🔧 Configuración

### Variables de Entorno (Container Apps)

Todas las variables están configuradas en `main.bicep`:

- `ConnectionStrings__DefaultConnection`: Cadena de conexión a SQL
- `Jwt__Secret`: Clave secreta para JWT
- `AzureStorage__ConnectionString`: Conexión a Storage Account
- `ApplicationInsights__ConnectionString`: Conexión a Application Insights

## 📝 Endpoints de API

### TasksApi (Interno)

- `POST /api/auth/login` - Login
- `GET /api/auth/health` - Health check
- `GET /api/tasks` - Listar tareas (requiere auth)
- `GET /api/tasks/{id}` - Obtener tarea (requiere auth)
- `POST /api/tasks` - Crear tarea (requiere auth)
- `PUT /api/tasks/{id}` - Actualizar tarea (requiere auth)
- `DELETE /api/tasks/{id}` - Eliminar tarea (requiere auth)

### FilesApi (Interno)

- `POST /api/auth/login` - Login
- `GET /api/auth/health` - Health check
- `GET /api/files` - Listar archivos (requiere auth)
- `GET /api/files/{id}` - Obtener metadata (requiere auth)
- `POST /api/files/upload` - Subir archivo (requiere auth)
- `GET /api/files/{id}/download` - Descargar archivo (requiere auth)
- `DELETE /api/files/{id}` - Eliminar archivo (requiere auth)

## 🛡️ Seguridad

- ✅ Backends con **ingress interno** (no accesibles desde Internet)
- ✅ Frontends con **ingress público**
- ✅ Autenticación JWT en todos los endpoints protegidos
- ✅ HTTPS en Container Apps
- ✅ Secrets almacenados en Container Apps secrets
- ✅ Storage Account sin acceso público a blobs
- ✅ SQL Server con firewall configurado

## 🧹 Limpieza

Para eliminar todos los recursos:

```bash
az group delete --name rg-containerapp-poc --yes --no-wait
```

## 📚 Tecnologías Utilizadas

- **.NET 8.0**: Runtime y SDK
- **Blazor Web App**: Frontends interactivos
- **ASP.NET Core Web API**: Backends RESTful
- **Entity Framework Core**: ORM para Azure SQL
- **Azure Container Apps**: Hosting de microservicios
- **Azure SQL Database**: Base de datos relacional
- **Azure Blob Storage**: Almacenamiento de archivos
- **Application Insights**: Telemetría y monitoreo
- **JWT Bearer Authentication**: Seguridad
- **Bicep**: Infraestructura como código

## 📄 Licencia

Este es un proyecto de demostración para propósitos educativos.

## ⚠️ Notas de Producción

**NO usar en producción sin:**
- Cambiar todas las contraseñas y secrets
- Implementar Azure Key Vault para secrets
- Configurar SSL/TLS personalizado
- Implementar políticas de red más restrictivas
- Configurar backup de bases de datos
- Implementar CI/CD pipeline
- Agregar health checks robustos
- Configurar auto-scaling apropiado
