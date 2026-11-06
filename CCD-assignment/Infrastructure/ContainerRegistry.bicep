@description('Azure region for the Container Registry.')
param location string = 'belgiumcentral'

@description('Globally unique name for the Azure Container Registry.')
param registryName string

resource containerRegistry 'Microsoft.ContainerRegistry/registries@2023-07-01' = {
  name: registryName
  location: location
  sku: {
    name: 'Basic'
  }
  properties: {
    adminUserEnabled: false
  }
}

output loginServer string = containerRegistry.properties.loginServer