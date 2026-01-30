#!/bin/bash

# Script de despliegue para Azure Container Apps POC
# Asegúrate de tener Azure CLI instalado y estar autenticado

set -e

# Variables
RESOURCE_GROUP="poc-containerapp-cus-rg"
LOCATION="centralus"
APP_NAME="pocapp"

echo "========================================="
echo "Azure Container Apps POC - Deployment"
echo "========================================="

# 1. Crear grupo de recursos
echo "📦 Creando grupo de recursos..."
az group create --name $RESOURCE_GROUP --location $LOCATION

# 2. Desplegar infraestructura base (ACR, SQL, Storage, etc)
echo "🏗️  Desplegando infraestructura base con Bicep..."
az deployment group create \
  --resource-group $RESOURCE_GROUP \
  --template-file srcs/infrastructure/main-infra.bicep \
  --parameters appName=$APP_NAME

# 3. Obtener ACR login server

echo "🔑 Obteniendo información del Container Registry..."
ACR_NAME=$(az deployment group show \
  --resource-group $RESOURCE_GROUP \
  --name main-infra \
  --query properties.outputs.acrLoginServer.value -o tsv)
echo "ACR: $ACR_NAME"

# 4. Login en ACR
echo "🔐 Autenticando en Azure Container Registry..."
ACR_SHORT_NAME=$(echo $ACR_NAME | cut -d'.' -f1)
az acr login --name $ACR_SHORT_NAME

# 5. Construir y publicar imágenes
echo "🐳 Construyendo y publicando imágenes Docker..."

cd srcs

echo "  → Construyendo Tasks API..."
docker build --platform linux/amd64 -f backends/TasksApi/Dockerfile -t ${ACR_NAME}/tasks-api:latest .
docker push ${ACR_NAME}/tasks-api:latest

echo "  → Construyendo Files API..."
docker build --platform linux/amd64 -f backends/FilesApi/Dockerfile -t ${ACR_NAME}/files-api:latest .
docker push ${ACR_NAME}/files-api:latest

echo "  → Construyendo Tasks Web..."
docker build --platform linux/amd64 -f frontends/TasksWeb/Dockerfile -t ${ACR_NAME}/tasks-web:latest .
docker push ${ACR_NAME}/tasks-web:latest

echo "  → Construyendo Files Web..."
docker build --platform linux/amd64 -f frontends/FilesWeb/Dockerfile -t ${ACR_NAME}/files-web:latest .
docker push ${ACR_NAME}/files-web:latest

echo "  → Construyendo File Processor Job..."
docker build --platform linux/amd64 -f backends/FileProcessorJob/Dockerfile -t ${ACR_NAME}/file-processor-job:latest .
docker push ${ACR_NAME}/file-processor-job:latest

cd ..

# 6. Desplegar Container Apps ahora que las imágenes existen
echo "🚀 Desplegando Container Apps..."
az deployment group create \
  --resource-group $RESOURCE_GROUP \
  --template-file srcs/infrastructure/main-apps.bicep \
  --parameters appName=$APP_NAME

# 7. Mostrar URLs
echo ""
echo "========================================="
echo "✅ Despliegue completado exitosamente!"
echo "========================================="
echo ""

TASKS_WEB_URL=$(az deployment group show \
  --resource-group $RESOURCE_GROUP \
  --name main-apps \
  --query properties.outputs.tasksWebUrl.value -o tsv)

FILES_WEB_URL=$(az deployment group show \
  --resource-group $RESOURCE_GROUP \
  --name main-apps \
  --query properties.outputs.filesWebUrl.value -o tsv)

SQL_SERVER=$(az deployment group show \
  --resource-group $RESOURCE_GROUP \
  --name main-infra \
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
echo "   admin / Preimitation{7{!3# (Admin)"
echo "   user / Chronoscopically\$0/(8. (User)"
echo ""