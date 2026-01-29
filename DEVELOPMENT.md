# Azure Container Apps POC - Guía de Desarrollo Local

## 🚀 Inicio Rápido con Docker Compose

### 1. Construir y levantar servicios

```bash
docker-compose up -d
```

Esto levantará:
- SQL Server (puerto 1433)
- Azurite (Storage Emulator) (puertos 10000-10002)
- Tasks API (puerto 5001)
- Files API (puerto 5002)
- Tasks Web (puerto 5003)
- Files Web (puerto 5004)

### 2. Ejecutar migraciones de base de datos

Espera a que SQL Server esté listo (~30 segundos), luego:

```bash
cd srcs/backends/TasksApi
dotnet ef database update

cd ../FilesApi
dotnet ef database update
```

### 3. Acceder a las aplicaciones

- **Tasks Web**: http://localhost:5003
- **Files Web**: http://localhost:5004
- **Tasks API Swagger**: http://localhost:5001/swagger
- **Files API Swagger**: http://localhost:5002/swagger

### Credenciales de prueba

| Usuario | Contraseña | Rol   |
|---------|------------|-------|
| admin   | admin123   | Admin |
| user    | user123    | User  |

## 🛠️ Desarrollo sin Docker

### Prerrequisitos

- .NET 8 SDK
- SQL Server LocalDB o SQL Server 2022
- Azurite (Storage Emulator)

### 1. Instalar Azurite

```bash
npm install -g azurite
```

### 2. Iniciar Azurite

```bash
azurite --location ./azurite-data
```

### 3. Actualizar cadenas de conexión

En `appsettings.json` de cada API, ajustar:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=TasksDb;Trusted_Connection=True;TrustServerCertificate=True"
  },
  "AzureStorage": {
    "ConnectionString": "UseDevelopmentStorage=true",
    "ContainerName": "files"
  }
}
```

### 4. Ejecutar migraciones

```bash
cd srcs/backends/TasksApi
dotnet ef migrations add InitialCreate
dotnet ef database update

cd ../FilesApi
dotnet ef migrations add InitialCreate
dotnet ef database update
```

### 5. Ejecutar servicios

Terminal 1 - Tasks API:
```bash
cd srcs/backends/TasksApi
dotnet run
```

Terminal 2 - Files API:
```bash
cd srcs/backends/FilesApi
dotnet run
```

Terminal 3 - Tasks Web:
```bash
cd srcs/frontends/TasksWeb
dotnet run
```

Terminal 4 - Files Web:
```bash
cd srcs/frontends/FilesWeb
dotnet run
```

## 🧪 Testing

### Probar con cURL

Login:
```bash
curl -X POST http://localhost:5001/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"username":"admin","password":"admin123"}'
```

Listar tareas (con token):
```bash
TOKEN="<tu-token-jwt>"
curl -X GET http://localhost:5001/api/tasks \
  -H "Authorization: Bearer $TOKEN"
```

### Probar con Swagger

1. Navega a http://localhost:5001/swagger
2. Haz login en `/api/auth/login`
3. Copia el token del response
4. Click en "Authorize" (🔒)
5. Ingresa: `Bearer <tu-token>`
6. Prueba los endpoints protegidos

## 📝 Crear nueva migración

Para TasksApi:
```bash
cd srcs/backends/TasksApi
dotnet ef migrations add <NombreMigracion>
dotnet ef database update
```

Para FilesApi:
```bash
cd srcs/backends/FilesApi
dotnet ef migrations add <NombreMigracion>
dotnet ef database update
```

## 🐛 Troubleshooting

### SQL Server no arranca en Docker

```bash
docker logs poc-sqlserver
```

Verificar que el contenedor tenga suficiente memoria (mínimo 2GB).

### Azurite no se conecta

Verificar que los puertos 10000-10002 no estén en uso:
```bash
lsof -i :10000
```

### Entity Framework no encuentra el DbContext

Asegúrate de estar en el directorio correcto del proyecto:
```bash
cd srcs/backends/TasksApi  # o FilesApi
```

## 🔄 Reconstruir imágenes Docker

Después de cambios en el código:

```bash
docker-compose build
docker-compose up -d
```

O reconstruir un servicio específico:

```bash
docker-compose build tasks-api
docker-compose up -d tasks-api
```

## 🧹 Limpieza

Detener y eliminar contenedores:
```bash
docker-compose down
```

Eliminar volúmenes (⚠️ esto borrará los datos):
```bash
docker-compose down -v
```

## 📊 Ver logs

Todos los servicios:
```bash
docker-compose logs -f
```

Un servicio específico:
```bash
docker-compose logs -f tasks-api
```

## 🔍 Inspeccionar base de datos

Conectar con Azure Data Studio o SQL Server Management Studio:

- **Server**: localhost,1433
- **Authentication**: SQL Server Authentication
- **Username**: sa
- **Password**: P@ssw0rd123!
- **Databases**: TasksDb, FilesDb

## 📦 Agregar paquetes NuGet

```bash
cd srcs/backends/TasksApi
dotnet add package NombrePaquete
```

Reconstruir imagen Docker después.

## ⚙️ Variables de entorno

Crear archivo `.env` en la raíz:

```env
SQL_PASSWORD=P@ssw0rd123!
JWT_SECRET=your-secret-key-min-32-chars-long!-change-in-production
```

Luego actualizar `docker-compose.yml` para usar:
```yaml
environment:
  - SA_PASSWORD=${SQL_PASSWORD}
```

## 🎯 Próximos pasos

- Implementar UI en Blazor para Tasks y Files
- Agregar paginación en APIs
- Implementar refresh tokens
- Agregar tests unitarios
- Configurar CI/CD con GitHub Actions
