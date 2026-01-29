#!/bin/bash

# Script de configuración para desarrollo local

set -e

echo "========================================="
echo "Configuración de Desarrollo Local"
echo "========================================="

# Directorio base
BASE_DIR="/Users/xiillii/Projects/POCs/AzureContainerApp"
cd "$BASE_DIR"

# 1. Verificar que dotnet ef está instalado
echo "1️⃣  Verificando dotnet-ef..."
if ! command -v dotnet-ef &> /dev/null; then
    echo "   Instalando dotnet-ef..."
    dotnet tool install --global dotnet-ef
    export PATH="$PATH:$HOME/.dotnet/tools"
else
    echo "   ✓ dotnet-ef ya instalado"
fi

# 2. Levantar contenedores
echo ""
echo "2️⃣  Levantando contenedores (SQL Server + Azurite)..."
docker-compose up -d sqlserver azurite

# 3. Esperar a que SQL Server esté listo
echo ""
echo "3️⃣  Esperando a que SQL Server esté listo..."
sleep 15
echo "   ✓ SQL Server debería estar listo"

# 4. Crear migraciones y actualizar base de datos para TasksApi
echo ""
echo "4️⃣  Configurando TasksApi..."
cd "$BASE_DIR/srcs/backends/TasksApi"

# Verificar si ya existe la carpeta Migrations
if [ -d "Migrations" ]; then
    echo "   ⚠️  Migrations ya existe, actualizando base de datos..."
    dotnet ef database update
else
    echo "   Creando migración inicial..."
    dotnet ef migrations add InitialCreate
    echo "   Actualizando base de datos..."
    dotnet ef database update
fi

# 5. Crear migraciones y actualizar base de datos para FilesApi
echo ""
echo "5️⃣  Configurando FilesApi..."
cd "$BASE_DIR/srcs/backends/FilesApi"

if [ -d "Migrations" ]; then
    echo "   ⚠️  Migrations ya existe, actualizando base de datos..."
    dotnet ef database update
else
    echo "   Creando migración inicial..."
    dotnet ef migrations add InitialCreate
    echo "   Actualizando base de datos..."
    dotnet ef database update
fi

# 6. Levantar todos los servicios
echo ""
echo "6️⃣  Levantando todos los servicios..."
cd "$BASE_DIR"
docker-compose up -d

# 7. Mostrar estado
echo ""
echo "========================================="
echo "✅ Configuración completada!"
echo "========================================="
echo ""
docker-compose ps
echo ""
echo "📊 URLs de acceso:"
echo "  Tasks API:    http://localhost:5001/swagger"
echo "  Files API:    http://localhost:5002/swagger"
echo "  Tasks Web:    http://localhost:5003"
echo "  Files Web:    http://localhost:5004"
echo ""
echo "🔐 Usuarios de prueba:"
echo "  admin / admin123 (Admin)"
echo "  user / user123 (User)"
echo ""
echo "📝 Logs: docker-compose logs -f [service-name]"
echo "🛑 Detener: docker-compose down"
echo ""
