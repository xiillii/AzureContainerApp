#!/bin/bash

# Script de despliegue para Azure Container Apps POC
# Asegúrate de tener Azure CLI instalado y estar autenticado

set -e

# Variables
RESOURCE_GROUP="rg-containerapp-poc"
LOCATION="eastus"
APP_NAME="pocapp"

echo "========================================="
echo "Azure Container Apps POC - Deployment"
echo "========================================="

# 1. Crear grupo de recursos
echo "📦 Creando grupo de recursos..."
az group create --name $RESOURCE_GROUP --location $LOCATION

# 2. Desplegar infraestructura
echo "🏗️  Desplegando infraestructura con Bicep..."
az deployment group create \
  --resource-group $RESOURCE_GROUP \
  --template-file srcs/infrastructure/main.bicep \
  --parameters appName=$APP_NAME

# 3. Obtener ACR login server
echo "🔑 Obteniendo información del Container Registry..."
ACR_NAME=$(az deployment group show \
  --resource-group $RESOURCE_GROUP \
  --name main \
  --query properties.outputs.acrLoginServer.value -o tsv)

echo "ACR: $ACR_NAME"

# 4. Login en ACR
echo "🔐 Autenticando en Azure Container Registry..."
az acr login --name $(echo $ACR_NAME | cut -d'.' -f1)

# 5. Construir y publicar imágenes
echo "🐳 Construyendo y publicando imágenes Docker..."

cd srcs

echo "  → Construyendo Tasks API..."
docker build -f backends/TasksApi/Dockerfile -t ${ACR_NAME}/tasks-api:latest .
docker push ${ACR_NAME}/tasks-api:latest

echo "  → Construyendo Files API..."
docker build -f backends/FilesApi/Dockerfile -t ${ACR_NAME}/files-api:latest .
docker push ${ACR_NAME}/files-api:latest

echo "  → Construyendo Tasks Web..."
docker build -f frontends/TasksWeb/Dockerfile -t ${ACR_NAME}/tasks-web:latest .
docker push ${ACR_NAME}/tasks-web:latest

echo "  → Construyendo Files Web..."
docker build -f frontends/FilesWeb/Dockerfile -t ${ACR_NAME}/files-web:latest .
docker push ${ACR_NAME}/files-web:latest

cd ..

# 6. Reiniciar Container Apps
echo "🔄 Reiniciando Container Apps..."
az containerapp restart --name tasks-api --resource-group $RESOURCE_GROUP
az containerapp restart --name files-api --resource-group $RESOURCE_GROUP
az containerapp restart --name tasks-web --resource-group $RESOURCE_GROUP
az containerapp restart --name files-web --resource-group $RESOURCE_GROUP

# 7. Mostrar URLs
echo ""
echo "========================================="
echo "✅ Despliegue completado exitosamente!"
echo "========================================="
echo ""

TASKS_WEB_URL=$(az deployment group show \
  --resource-group $RESOURCE_GROUP \
  --name main \
  --query properties.outputs.tasksWebUrl.value -o tsv)

FILES_WEB_URL=$(az deployment group show \
  --resource-group $RESOURCE_GROUP \
  --name main \
  --query properties.outputs.filesWebUrl.value -o tsv)

SQL_SERVER=$(az deployment group show \
  --resource-group $RESOURCE_GROUP \
  --name main \
  --query properties.outputs.sqlServerName.value -o tsv)

echo "📊 URLs de acceso:"
echo "  Tasks Web: $TASKS_WEB_URL"
echo "  Files Web: $FILES_WEB_URL"
echo ""
echo "🗄️  SQL Server: $SQL_SERVER"
echo ""
echo "⚠️  Nota: Ejecuta las migraciones de base de datos manualmente:"
echo "   cd srcs/backends/TasksApi"
echo "   dotnet ef database update --connection \"Server=tcp:${SQL_SERVER},1433;Initial Catalog=TasksDb;User ID=sqladmin;Password=P@ssw0rd123!;Encrypt=True;\""
echo ""
echo "   cd ../FilesApi"
echo "   dotnet ef database update --connection \"Server=tcp:${SQL_SERVER},1433;Initial Catalog=FilesDb;User ID=sqladmin;Password=P@ssw0rd123!;Encrypt=True;\""
echo ""
echo "🔐 Usuarios de prueba:"
echo "   admin / admin123 (Admin)"
echo "   user / user123 (User)"
echo ""
