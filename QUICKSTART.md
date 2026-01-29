# 🚀 Inicio Rápido - Desarrollo Local

## Método 1: Script Automático (Recomendado)

```bash
bash setup-local.sh
```

Este script automáticamente:
1. Instala `dotnet-ef` si no existe
2. Levanta SQL Server y Azurite
3. Crea las migraciones de base de datos
4. Levanta todos los servicios
5. Muestra las URLs de acceso

## Método 2: Paso a Paso

### 1. Instalar Entity Framework Core Tools

```bash
dotnet tool install --global dotnet-ef
export PATH="$PATH:$HOME/.dotnet/tools"
```

### 2. Levantar solo SQL Server y Azurite

```bash
docker-compose up -d sqlserver azurite
```

Espera ~15 segundos para que SQL Server esté listo.

### 3. Crear migraciones para TasksApi

```bash
cd srcs/backends/TasksApi
dotnet ef migrations add InitialCreate
dotnet ef database update
```

### 4. Crear migraciones para FilesApi

```bash
cd ../FilesApi
dotnet ef migrations add InitialCreate
dotnet ef database update
```

### 5. Levantar todos los servicios

```bash
cd ../../..
docker-compose up -d
```

### 6. Verificar estado

```bash
docker-compose ps
```

## 🌐 URLs de Acceso

- **Tasks API Swagger**: http://localhost:5001/swagger
- **Files API Swagger**: http://localhost:5002/swagger
- **Tasks Web**: http://localhost:5003
- **Files Web**: http://localhost:5004

## 🔐 Usuarios de Prueba

| Usuario | Contraseña | Rol   |
|---------|------------|-------|
| admin   | admin123   | Admin |
| user    | user123    | User  |

## 🧪 Probar con cURL

### 1. Login

```bash
curl -X POST http://localhost:5001/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"username":"admin","password":"admin123"}'
```

Guarda el `token` del response.

### 2. Crear tarea

```bash
TOKEN="<tu-token-jwt>"
curl -X POST http://localhost:5001/api/tasks \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"title":"Mi primera tarea","description":"Descripción de prueba","isCompleted":false}'
```

### 3. Listar tareas

```bash
curl -X GET http://localhost:5001/api/tasks \
  -H "Authorization: Bearer $TOKEN"
```

## 📊 Ver Logs

Todos los servicios:
```bash
docker-compose logs -f
```

Un servicio específico:
```bash
docker-compose logs -f tasks-api
```

## 🛑 Detener Servicios

```bash
docker-compose down
```

Para eliminar también los datos (bases de datos):
```bash
docker-compose down -v
```

## ⚠️ Troubleshooting

### Error: "no such file or directory: srcs/backends/TasksApi"

Asegúrate de estar en el directorio raíz del proyecto:
```bash
cd /Users/xiillii/Projects/POCs/AzureContainerApp
```

### Error: "dotnet-ef does not exist"

Instala las herramientas EF Core:
```bash
dotnet tool install --global dotnet-ef
export PATH="$PATH:$HOME/.dotnet/tools"
```

O reinicia tu terminal después de instalarlo.

### SQL Server no arranca

Verifica logs:
```bash
docker logs poc-sqlserver
```

Asegúrate de tener al menos 2GB de RAM disponible para Docker.

### Imágenes Docker no se construyen

Desde el directorio raíz:
```bash
docker-compose build --no-cache
docker-compose up -d
```

## 🔄 Recargar Cambios

Después de modificar código:

```bash
docker-compose build [service-name]
docker-compose up -d [service-name]
```

Por ejemplo:
```bash
docker-compose build tasks-api
docker-compose up -d tasks-api
```

## 📝 Agregar Nueva Migración

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

## 🗄️ Conectar a SQL Server

Usar Azure Data Studio o SQL Server Management Studio:

- **Server**: localhost,1433
- **Authentication**: SQL Server Authentication
- **Username**: sa
- **Password**: P@ssw0rd123!
- **Databases**: TasksDb, FilesDb

## 📦 Conectar a Azurite (Storage Emulator)

- **Connection String**: `UseDevelopmentStorage=true`
- **Account Name**: devstoreaccount1
- **Blob Endpoint**: http://localhost:10000/devstoreaccount1

Usar Azure Storage Explorer para visualizar blobs.
