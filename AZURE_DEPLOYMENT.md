# Azure Deployment Guide

## Prerequisites

1. **Install Azure CLI**
   ```bash
   # macOS
   brew install azure-cli
   
   # Or download from: https://aka.ms/InstallAzureCLIDeb
   ```

2. **Login to Azure**
   ```bash
   az login
   ```

3. **Set your subscription** (if you have multiple)
   ```bash
   az account list --output table
   az account set --subscription "Your-Subscription-Name-or-ID"
   ```

## Deployment Steps

### 1. Create Resource Group

```bash
# Set variables
RESOURCE_GROUP="rg-containerapp-poc"
LOCATION="eastus"  # or your preferred region

# Create resource group
az group create \
  --name $RESOURCE_GROUP \
  --location $LOCATION
```

### 2. Deploy Infrastructure

```bash
# Deploy Bicep template
az deployment group create \
  --resource-group $RESOURCE_GROUP \
  --template-file srcs/infrastructure/main.bicep \
  --parameters location=$LOCATION \
  --parameters environmentName="poc-env" \
  --parameters sqlAdminPassword="YourStrongPassword123!" \
  --parameters jwtSecret="your-super-secret-jwt-key-min-32-chars-long!"
```

### 3. Build and Push Docker Images

After infrastructure is deployed, you need to push your Docker images to Azure Container Registry (ACR):

```bash
# Get ACR name from deployment output
ACR_NAME=$(az deployment group show \
  --resource-group $RESOURCE_GROUP \
  --name main \
  --query properties.outputs.acrLoginServer.value \
  --output tsv | cut -d'.' -f1)

# Login to ACR
az acr login --name $ACR_NAME

# Build and push images
ACR_SERVER="${ACR_NAME}.azurecr.io"

# Tasks API
docker build -t ${ACR_SERVER}/tasks-api:latest -f srcs/backends/TasksApi/Dockerfile ./srcs
docker push ${ACR_SERVER}/tasks-api:latest

# Files API
docker build -t ${ACR_SERVER}/files-api:latest -f srcs/backends/FilesApi/Dockerfile ./srcs
docker push ${ACR_SERVER}/files-api:latest

# Tasks Web
docker build -t ${ACR_SERVER}/tasks-web:latest -f srcs/frontends/TasksWeb/Dockerfile ./srcs
docker push ${ACR_SERVER}/tasks-web:latest

# Files Web
docker build -t ${ACR_SERVER}/files-web:latest -f srcs/frontends/FilesWeb/Dockerfile ./srcs
docker push ${ACR_SERVER}/files-web:latest
```

### 4. Update Container Apps with Images

```bash
# Update container apps to use the pushed images
az containerapp update \
  --name tasks-api \
  --resource-group $RESOURCE_GROUP \
  --image ${ACR_SERVER}/tasks-api:latest

az containerapp update \
  --name files-api \
  --resource-group $RESOURCE_GROUP \
  --image ${ACR_SERVER}/files-api:latest

az containerapp update \
  --name tasks-web \
  --resource-group $RESOURCE_GROUP \
  --image ${ACR_SERVER}/tasks-web:latest

az containerapp update \
  --name files-web \
  --resource-group $RESOURCE_GROUP \
  --image ${ACR_SERVER}/files-web:latest
```

### 5. Get Application URLs

```bash
# Get Tasks Web URL
az containerapp show \
  --name tasks-web \
  --resource-group $RESOURCE_GROUP \
  --query properties.configuration.ingress.fqdn \
  --output tsv

# Get Files Web URL
az containerapp show \
  --name files-web \
  --resource-group $RESOURCE_GROUP \
  --query properties.configuration.ingress.fqdn \
  --output tsv
```

## Quick Deployment Script

Or use this all-in-one script:

```bash
#!/bin/bash
set -e

# Configuration
RESOURCE_GROUP="rg-containerapp-poc"
LOCATION="eastus"
SQL_PASSWORD="YourStrongPassword123!"
JWT_SECRET="your-super-secret-jwt-key-min-32-chars-long!"

echo "🚀 Starting Azure deployment..."

# 1. Create resource group
echo "📦 Creating resource group..."
az group create --name $RESOURCE_GROUP --location $LOCATION

# 2. Deploy infrastructure
echo "🏗️  Deploying infrastructure..."
az deployment group create \
  --resource-group $RESOURCE_GROUP \
  --template-file srcs/infrastructure/main.bicep \
  --parameters location=$LOCATION \
  --parameters sqlAdminPassword=$SQL_PASSWORD \
  --parameters jwtSecret=$JWT_SECRET

# 3. Get ACR info
echo "🔍 Getting ACR information..."
ACR_NAME=$(az deployment group show \
  --resource-group $RESOURCE_GROUP \
  --name main \
  --query properties.outputs.acrLoginServer.value \
  --output tsv | cut -d'.' -f1)

ACR_SERVER="${ACR_NAME}.azurecr.io"

# 4. Login to ACR
echo "🔐 Logging in to ACR..."
az acr login --name $ACR_NAME

# 5. Build and push images
echo "🐳 Building and pushing Docker images..."
docker build -t ${ACR_SERVER}/tasks-api:latest -f srcs/backends/TasksApi/Dockerfile ./srcs
docker push ${ACR_SERVER}/tasks-api:latest

docker build -t ${ACR_SERVER}/files-api:latest -f srcs/backends/FilesApi/Dockerfile ./srcs
docker push ${ACR_SERVER}/files-api:latest

docker build -t ${ACR_SERVER}/tasks-web:latest -f srcs/frontends/TasksWeb/Dockerfile ./srcs
docker push ${ACR_SERVER}/tasks-web:latest

docker build -t ${ACR_SERVER}/files-web:latest -f srcs/frontends/FilesWeb/Dockerfile ./srcs
docker push ${ACR_SERVER}/files-web:latest

# 6. Update container apps
echo "🔄 Updating container apps..."
az containerapp update --name tasks-api --resource-group $RESOURCE_GROUP --image ${ACR_SERVER}/tasks-api:latest
az containerapp update --name files-api --resource-group $RESOURCE_GROUP --image ${ACR_SERVER}/files-api:latest
az containerapp update --name tasks-web --resource-group $RESOURCE_GROUP --image ${ACR_SERVER}/tasks-web:latest
az containerapp update --name files-web --resource-group $RESOURCE_GROUP --image ${ACR_SERVER}/files-web:latest

# 7. Get URLs
echo ""
echo "✅ Deployment complete!"
echo ""
echo "📱 Application URLs:"
echo "Tasks Web: https://$(az containerapp show --name tasks-web --resource-group $RESOURCE_GROUP --query properties.configuration.ingress.fqdn -o tsv)"
echo "Files Web: https://$(az containerapp show --name files-web --resource-group $RESOURCE_GROUP --query properties.configuration.ingress.fqdn -o tsv)"
echo ""
echo "🔍 To view resources:"
echo "az resource list --resource-group $RESOURCE_GROUP --output table"
```

Save this as `deploy-azure.sh`, make it executable, and run:

```bash
chmod +x deploy-azure.sh
./deploy-azure.sh
```

## What Gets Created

The Bicep deployment will create:

1. **Container Apps Environment** - Hosts all container apps
2. **Azure Container Registry** - Stores Docker images
3. **Azure SQL Server** - Database server
4. **Azure SQL Databases** - TasksDb and FilesDb
5. **Storage Account** - Blob storage for files
6. **Application Insights** - Monitoring and diagnostics
7. **Log Analytics Workspace** - Centralized logging
8. **4 Container Apps** - tasks-api, files-api, tasks-web, files-web

## Estimated Cost

- Container Apps: ~$0.10/hour per app (4 apps = $0.40/hour)
- Azure SQL: ~$5-15/day (depending on tier)
- Storage: ~$0.02/GB/month
- Container Registry: ~$5/month (Basic tier)

**Total: ~$100-200/month** for this POC

## Cleanup

To delete all resources:

```bash
az group delete --name $RESOURCE_GROUP --yes --no-wait
```

## Troubleshooting

### View Container Logs
```bash
az containerapp logs show \
  --name tasks-api \
  --resource-group $RESOURCE_GROUP \
  --follow
```

### View Container App Details
```bash
az containerapp show \
  --name tasks-api \
  --resource-group $RESOURCE_GROUP
```

### Test Database Connection
```bash
# Get SQL server name
SQL_SERVER=$(az sql server list --resource-group $RESOURCE_GROUP --query [0].fullyQualifiedDomainName -o tsv)

# Test connection (requires sqlcmd or Azure Data Studio)
sqlcmd -S $SQL_SERVER -U sqladmin -P YourPassword -d TasksDb -Q "SELECT @@VERSION"
```
