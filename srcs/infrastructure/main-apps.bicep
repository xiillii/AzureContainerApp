// Bicep template for Container Apps only
// Deploy this after infrastructure is ready and Docker images are pushed

param location string = resourceGroup().location
param appName string = 'pocapp'
param resourcePostfix string = 'jaromero' // Change in production

// Get existing resources
var uniqueSuffix = resourcePostfix
var acrName = toLower('${appName}acr${uniqueSuffix}')
var sqlServerName = toLower('${appName}-sql-${uniqueSuffix}')
var storageAccountName = toLower('${appName}st${uniqueSuffix}')
var appInsightsName = '${appName}-ai-${uniqueSuffix}'
var environmentName = '${appName}-env-${uniqueSuffix}'

resource acr 'Microsoft.ContainerRegistry/registries@2023-01-01-preview' existing = {
  name: acrName
}

resource sqlServer 'Microsoft.Sql/servers@2023-02-01-preview' existing = {
  name: sqlServerName
}

resource storageAccount 'Microsoft.Storage/storageAccounts@2023-01-01' existing = {
  name: storageAccountName
}

resource appInsights 'Microsoft.Insights/components@2020-02-02' existing = {
  name: appInsightsName
}

resource containerAppEnv 'Microsoft.App/managedEnvironments@2023-05-01' existing = {
  name: environmentName
}

// JWT Secret
var jwtSecret = 'your-secret-key-min-32-chars-long!-change-in-production'

// Tasks API Container App
resource tasksApiApp 'Microsoft.App/containerApps@2023-05-01' = {
  name: 'tasks-api'
  location: location
  properties: {
    managedEnvironmentId: containerAppEnv.id
    configuration: {
      ingress: {
        external: false
        targetPort: 8080
        transport: 'http'
      }
      registries: [
        {
          server: acr.properties.loginServer
          username: acr.listCredentials().username
          passwordSecretRef: 'acr-password'
        }
      ]
      secrets: [
        {
          name: 'acr-password'
          value: acr.listCredentials().passwords[0].value
        }
        {
          name: 'sql-connection-string'
          value: 'Server=tcp:${sqlServer.properties.fullyQualifiedDomainName},1433;Initial Catalog=TasksDb;Persist Security Info=False;User ID=${sqlServer.properties.administratorLogin};Password=P@ssw0rd123!;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;'
        }
        {
          name: 'jwt-secret'
          value: jwtSecret
        }
        {
          name: 'appinsights-connection-string'
          value: appInsights.properties.ConnectionString
        }
      ]
    }
    template: {
      containers: [
        {
          name: 'tasks-api'
          image: '${acr.properties.loginServer}/tasks-api:latest'
          resources: {
            cpu: json('0.5')
            memory: '1Gi'
          }
          env: [
            {
              name: 'ConnectionStrings__DefaultConnection'
              secretRef: 'sql-connection-string'
            }
            {
              name: 'Jwt__Secret'
              secretRef: 'jwt-secret'
            }
            {
              name: 'Jwt__Issuer'
              value: 'TasksApi'
            }
            {
              name: 'Jwt__Audience'
              value: 'TasksWebApp'
            }
            {
              name: 'ApplicationInsights__ConnectionString'
              secretRef: 'appinsights-connection-string'
            }
          ]
        }
      ]
      scale: {
        minReplicas: 1
        maxReplicas: 3
      }
    }
  }
}

// Files API Container App
resource filesApiApp 'Microsoft.App/containerApps@2023-05-01' = {
  name: 'files-api'
  location: location
  properties: {
    managedEnvironmentId: containerAppEnv.id
    configuration: {
      ingress: {
        external: false
        targetPort: 8080
        transport: 'http'
      }
      registries: [
        {
          server: acr.properties.loginServer
          username: acr.listCredentials().username
          passwordSecretRef: 'acr-password'
        }
      ]
      secrets: [
        {
          name: 'acr-password'
          value: acr.listCredentials().passwords[0].value
        }
        {
          name: 'sql-connection-string'
          value: 'Server=tcp:${sqlServer.properties.fullyQualifiedDomainName},1433;Initial Catalog=FilesDb;Persist Security Info=False;User ID=${sqlServer.properties.administratorLogin};Password=P@ssw0rd123!;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;'
        }
        {
          name: 'storage-connection-string'
          value: 'DefaultEndpointsProtocol=https;AccountName=${storageAccount.name};AccountKey=${storageAccount.listKeys().keys[0].value};EndpointSuffix=${environment().suffixes.storage}'
        }
        {
          name: 'jwt-secret'
          value: jwtSecret
        }
        {
          name: 'appinsights-connection-string'
          value: appInsights.properties.ConnectionString
        }
      ]
    }
    template: {
      containers: [
        {
          name: 'files-api'
          image: '${acr.properties.loginServer}/files-api:latest'
          resources: {
            cpu: json('0.5')
            memory: '1Gi'
          }
          env: [
            {
              name: 'ConnectionStrings__DefaultConnection'
              secretRef: 'sql-connection-string'
            }
            {
              name: 'AzureStorage__ConnectionString'
              secretRef: 'storage-connection-string'
            }
            {
              name: 'AzureStorage__ContainerName'
              value: 'files'
            }
            {
              name: 'Jwt__Secret'
              secretRef: 'jwt-secret'
            }
            {
              name: 'Jwt__Issuer'
              value: 'FilesApi'
            }
            {
              name: 'Jwt__Audience'
              value: 'FilesWebApp'
            }
            {
              name: 'ApplicationInsights__ConnectionString'
              secretRef: 'appinsights-connection-string'
            }
          ]
        }
      ]
      scale: {
        minReplicas: 1
        maxReplicas: 3
      }
    }
  }
}

// Tasks Web Container App (Public)
resource tasksWebApp 'Microsoft.App/containerApps@2023-05-01' = {
  name: 'tasks-web'
  location: location
  properties: {
    managedEnvironmentId: containerAppEnv.id
    configuration: {
      ingress: {
        external: true
        targetPort: 8080
        transport: 'http'
      }
      registries: [
        {
          server: acr.properties.loginServer
          username: acr.listCredentials().username
          passwordSecretRef: 'acr-password'
        }
      ]
      secrets: [
        {
          name: 'acr-password'
          value: acr.listCredentials().passwords[0].value
        }
        {
          name: 'jwt-secret'
          value: jwtSecret
        }
      ]
    }
    template: {
      containers: [
        {
          name: 'tasks-web'
          image: '${acr.properties.loginServer}/tasks-web:latest'
          resources: {
            cpu: json('0.5')
            memory: '1Gi'
          }
          env: [
            {
              name: 'ApiBaseUrl'
              value: 'https://${tasksApiApp.properties.configuration.ingress.fqdn}'
            }
            {
              name: 'Jwt__Secret'
              secretRef: 'jwt-secret'
            }
          ]
        }
      ]
      scale: {
        minReplicas: 1
        maxReplicas: 5
      }
    }
  }
}

// Files Web Container App (Public)
resource filesWebApp 'Microsoft.App/containerApps@2023-05-01' = {
  name: 'files-web'
  location: location
  properties: {
    managedEnvironmentId: containerAppEnv.id
    configuration: {
      ingress: {
        external: true
        targetPort: 8080
        transport: 'http'
      }
      registries: [
        {
          server: acr.properties.loginServer
          username: acr.listCredentials().username
          passwordSecretRef: 'acr-password'
        }
      ]
      secrets: [
        {
          name: 'acr-password'
          value: acr.listCredentials().passwords[0].value
        }
        {
          name: 'jwt-secret'
          value: jwtSecret
        }
      ]
    }
    template: {
      containers: [
        {
          name: 'files-web'
          image: '${acr.properties.loginServer}/files-web:latest'
          resources: {
            cpu: json('0.5')
            memory: '1Gi'
          }
          env: [
            {
              name: 'ApiBaseUrl'
              value: 'https://${filesApiApp.properties.configuration.ingress.fqdn}'
            }
            {
              name: 'Jwt__Secret'
              secretRef: 'jwt-secret'
            }
          ]
        }
      ]
      scale: {
        minReplicas: 1
        maxReplicas: 5
      }
    }
  }
}

// Outputs
output tasksWebUrl string = 'https://${tasksWebApp.properties.configuration.ingress.fqdn}'
output filesWebUrl string = 'https://${filesWebApp.properties.configuration.ingress.fqdn}'
output tasksApiUrl string = 'https://${tasksApiApp.properties.configuration.ingress.fqdn}'
output filesApiUrl string = 'https://${filesApiApp.properties.configuration.ingress.fqdn}'
