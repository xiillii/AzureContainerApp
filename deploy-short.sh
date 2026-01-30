#!/bin/bash

# Script de despliegue para Azure Container Apps POC
# Asegúrate de tener Azure CLI instalado y estar autenticado

set -e

# Variables
RESOURCE_GROUP="rg-containerapp-cus-poc"
LOCATION="centralus"
APP_NAME="pocapp"

echo "========================================="
echo "Azure Container Apps POC - Deployment. Short Version"
echo "========================================="

# 1. Crear grupo de recursos
echo "📦 Creando grupo de recursos..."
az group create --name $RESOURCE_GROUP --location $LOCATION

# 2. Desplegar infraestructura
echo "🏗️  Desplegando infraestructura con Bicep..."
az deployment group create \
  --resource-group $RESOURCE_GROUP \
  --template-file srcs/infrastructure/main-short.bicep \
  --parameters appName=$APP_NAME

# 3. Obtener ACR login server
echo "🔑 Obteniendo información del Container Registry..."
ACR_NAME=$(az deployment group show \
  --resource-group $RESOURCE_GROUP \
  --name main-short \
  --query properties.outputs.acrLoginServer.value -o tsv)

echo "ACR: $ACR_NAME"

