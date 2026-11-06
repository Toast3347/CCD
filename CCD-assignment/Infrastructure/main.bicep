@description('Azure region for all resources.')
param location string = 'belgiumcentral'

@description('Lowercase application prefix used in resource names.')
param prefix string = 'ccdweatherapp'

@description('Name of the existing Azure Container Registry.')
param registryName string

@description('Full ExternalAPI image name, including registry, repository and tag.')
param apiImage string

@description('Full WeatherFunctions image name, including registry, repository and tag.')
param functionsImage string


// Naming settings

var normalizedPrefix = toLower(replace(prefix, '-', ''))

var storageAccountName = take(
  '${normalizedPrefix}${uniqueString(resourceGroup().id)}',
  24
)

var containerEnvironmentName = '${normalizedPrefix}-env'
var apiContainerAppName = '${normalizedPrefix}-api'
var functionsContainerAppName = '${normalizedPrefix}-functions'
var identityName = '${normalizedPrefix}-container-identity'


// Existing Azure Container Registry

resource containerRegistry 'Microsoft.ContainerRegistry/registries@2023-07-01' existing = {
  name: registryName
}


// Storage Account

resource storageAccount 'Microsoft.Storage/storageAccounts@2023-05-01' = {
  name: storageAccountName
  location: location
  sku: {
    name: 'Standard_LRS'
  }
  kind: 'StorageV2'
  properties: {
    accessTier: 'Hot'
    allowBlobPublicAccess: false
    minimumTlsVersion: 'TLS1_2'
    supportsHttpsTrafficOnly: true
  }
}

var storageKeys = listKeys(storageAccount.id, '2023-05-01')

var storageConnectionString = 'DefaultEndpointsProtocol=https;AccountName=${storageAccount.name};AccountKey=${storageKeys.keys[0].value};EndpointSuffix=${environment().suffixes.storage}'


// Blob, queue and table Storage

resource blobService 'Microsoft.Storage/storageAccounts/blobServices@2023-05-01' = {
  parent: storageAccount
  name: 'default'
}

resource blobContainer 'Microsoft.Storage/storageAccounts/blobServices/containers@2023-05-01' = {
  parent: blobService
  name: 'images'
  properties: {
    publicAccess: 'None'
  }
}

resource queueService 'Microsoft.Storage/storageAccounts/queueServices@2023-05-01' = {
  parent: storageAccount
  name: 'default'
}

resource weatherJobsQueue 'Microsoft.Storage/storageAccounts/queueServices/queues@2023-05-01' = {
  parent: queueService
  name: 'weather-jobs'
}

resource imageJobsQueue 'Microsoft.Storage/storageAccounts/queueServices/queues@2023-05-01' = {
  parent: queueService
  name: 'image-jobs'
}

resource tableService 'Microsoft.Storage/storageAccounts/tableServices@2023-05-01' = {
  parent: storageAccount
  name: 'default'
}

resource jobStatusTable 'Microsoft.Storage/storageAccounts/tableServices/tables@2023-05-01' = {
  parent: tableService
  name: 'ImageJobs'
}


// Managed Identity for ACR image pulls

resource containerAppIdentity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' = {
  name: identityName
  location: location
}

var acrPullRoleDefinitionId = '7f951dda-4ed3-4680-a7ca-43fe172d538d'

resource acrPullRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(
    containerRegistry.id,
    containerAppIdentity.id,
    acrPullRoleDefinitionId
  )
  scope: containerRegistry
  properties: {
    roleDefinitionId: subscriptionResourceId(
      'Microsoft.Authorization/roleDefinitions',
      acrPullRoleDefinitionId
    )
    principalId: containerAppIdentity.properties.principalId
    principalType: 'ServicePrincipal'
  }
}


// Azure Container Apps Environment

resource containerAppEnv 'Microsoft.App/managedEnvironments@2024-03-01' = {
  name: containerEnvironmentName
  location: location
  properties: {}
}


// ExternalAPI Container

resource apiContainerApp 'Microsoft.App/containerApps@2024-03-01' = {
  name: apiContainerAppName
  location: location
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: {
      '${containerAppIdentity.id}': {}
    }
  }
  properties: {
    managedEnvironmentId: containerAppEnv.id

    configuration: {
      activeRevisionsMode: 'Single'

      registries: [
        {
          server: containerRegistry.properties.loginServer
          identity: containerAppIdentity.id
        }
      ]

      secrets: [
        {
          name: 'storage-connection'
          value: storageConnectionString
        }
      ]

      ingress: {
        external: true
        targetPort: 8080
        transport: 'auto'
        allowInsecure: false
      }
    }

    template: {
      containers: [
        {
          name: 'weather-api'
          image: apiImage

          env: [
              {
                name: 'AzureWebJobsStorage'
                secretRef: 'storage-connection'
              }
              {
                name: 'SECRET_VERSION'
                value: 'initial'
              }
            ]

          resources: {
            cpu: json('0.5')
            memory: '1Gi'
          }
        }
      ]

      scale: {
        minReplicas: 1
        maxReplicas: 5
      }
    }
  }

  dependsOn: [
    acrPullRoleAssignment
  ]
}


// WeatherFunctions Container

resource functionsContainerApp 'Microsoft.App/containerApps@2024-03-01' = {
  name: functionsContainerAppName
  location: location

  kind: 'functionapp,linux,container,azurecontainerapps'

  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: {
      '${containerAppIdentity.id}': {}
    }
  }

  properties: {
    managedEnvironmentId: containerAppEnv.id

    configuration: {
      activeRevisionsMode: 'Single'

      registries: [
        {
          server: containerRegistry.properties.loginServer
          identity: containerAppIdentity.id
        }
      ]

      secrets: [
        {
          name: 'storage-connection'
          value: storageConnectionString
        }
      ]

      ingress: {
        external: true
        targetPort: 80
        transport: 'auto'
        allowInsecure: false
      }
    }

    template: {
      containers: [
        {
          name: 'weather-functions'
          image: functionsImage

          env: [
            {
              name: 'AzureWebJobsStorage'
              secretRef: 'storage-connection'
            }
            {
              name: 'FUNCTIONS_WORKER_RUNTIME'
              value: 'dotnet-isolated'
            }
            {
              name: 'FUNCTIONS_EXTENSION_VERSION'
              value: '~4'
            }
            {
              name: 'AzureFunctionsJobHost__Logging__Console__IsEnabled'
              value: 'true'
            }
            {
              name: 'SECRET_VERSION'
              value: 'initial'
            }
          ]

          resources: {
            cpu: json('0.5')
            memory: '1Gi'
          }
        }
      ]

      scale: {
        minReplicas: 1
        maxReplicas: 10
      }
    }
  }

  dependsOn: [
    acrPullRoleAssignment
  ]
}


// Outputs

output storageAccountName string = storageAccount.name

output containerAppsEnvironmentName string = containerAppEnv.name

output apiContainerAppName string = apiContainerApp.name

output functionsContainerAppName string = functionsContainerApp.name

output containerAppIdentityPrincipalId string = containerAppIdentity.properties.principalId

output apiFqdn string = apiContainerApp.properties.configuration.ingress.fqdn

output functionsFqdn string = functionsContainerApp.properties.configuration.ingress.fqdn