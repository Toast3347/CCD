$ErrorActionPreference = "Stop"

# Config

$ScriptDir = $PSScriptRoot
$RepositoryRoot = Split-Path $ScriptDir -Parent

$ResourceGroup = "WeatherApp_RG"
$Location = "belgiumcentral"
$Prefix = "ccdweatherapp"
$RegistryName = "ccdweatherapp"

$ExternalApiPath = Join-Path $RepositoryRoot "ExternalAPI"
$FunctionsPath = Join-Path $RepositoryRoot "WeatherFunctions"

$AcrTemplate = Join-Path $ScriptDir "ContainerRegistry.bicep"
$MainTemplate = Join-Path $ScriptDir "main.bicep"

$ApiDockerfile = Join-Path $ExternalApiPath "Dockerfile"
$FunctionsDockerfile = Join-Path $FunctionsPath "Dockerfile"


# Helper

function Assert-NativeCommand {
    param(
        [string]$Message
    )

    if ($LASTEXITCODE -ne 0) {
        throw "$Message (exit code: $LASTEXITCODE)"
    }
}

function Invoke-AzJson {
    param(
        [string[]]$Arguments,
        [string]$FailureMessage
    )

    $output = & az @Arguments

    Assert-NativeCommand $FailureMessage

    if (-not $output) {
        throw "$FailureMessage (Azure CLI returned no output)"
    }

    return ($output | ConvertFrom-Json)
}


# Checks

Write-Host "`nChecking project files..." -ForegroundColor Cyan

foreach ($path in @(
    $ExternalApiPath,
    $FunctionsPath,
    $ApiDockerfile,
    $FunctionsDockerfile,
    $AcrTemplate,
    $MainTemplate
)) {
    if (-not (Test-Path $path)) {
        throw "Required file or directory not found: $path"
    }
}

foreach ($tool in @("az", "docker")) {
    if (-not (Get-Command $tool -ErrorAction SilentlyContinue)) {
        throw "Required command '$tool' is not installed."
    }
}

docker info --format '{{.ServerVersion}}' | Out-Null
Assert-NativeCommand "Docker Desktop is not running."


# Azure CLI login

Write-Host "`nLogging in to Azure..." -ForegroundColor Cyan

az login
Assert-NativeCommand "Azure login failed."

az account show --output none
Assert-NativeCommand "No active Azure subscription."


# Register resource providers

Write-Host "`nRegistering Azure resource providers..." -ForegroundColor Cyan

az provider register --namespace Microsoft.App --wait
Assert-NativeCommand "Microsoft.App registration failed."

az provider register --namespace Microsoft.ContainerRegistry --wait
Assert-NativeCommand "Microsoft.ContainerRegistry registration failed."

az provider register --namespace Microsoft.Storage --wait
Assert-NativeCommand "Microsoft.Storage registration failed."


# Create resource group

Write-Host "`nCreating resource group..." -ForegroundColor Cyan

az group create `
    --name $ResourceGroup `
    --location $Location `
    --output none

Assert-NativeCommand "Resource group creation failed."


# Deploy azure container registry

Write-Host "`nDeploying Azure Container Registry..." -ForegroundColor Cyan

$acrDeployment = Invoke-AzJson -Arguments @(
    "deployment", "group", "create",
    "--name", "weatherapp-acr",
    "--resource-group", $ResourceGroup,
    "--template-file", $AcrTemplate,
    "--parameters",
    "location=$Location",
    "registryName=$RegistryName",
    "--output", "json"
) -FailureMessage "ACR deployment failed."

if ($null -eq $acrDeployment.properties.outputs.loginServer) {
    throw "ACR deployment did not return loginServer."
}

$RegistryServer = [string]$acrDeployment.properties.outputs.loginServer.value
$RegistryServer = $RegistryServer.Trim()

if ([string]::IsNullOrWhiteSpace($RegistryServer)) {
    throw "Registry login server is empty."
}

if ($RegistryServer -notmatch '^[a-zA-Z0-9.-]+\.azurecr\.io$') {
    throw "Invalid registry hostname: $RegistryServer"
}

Write-Host "Registry: $RegistryServer" -ForegroundColor Green


# Image name vars

$ApiImage = "${RegistryServer}/weather-api:latest"
$FunctionsImage = "${RegistryServer}/weather-functions:latest"

Write-Host "`nAPI image: $ApiImage"
Write-Host "Functions image: $FunctionsImage"


# Login to ACR

Write-Host "`nLogging Docker in to ACR..." -ForegroundColor Cyan

az acr login --name $RegistryName
Assert-NativeCommand "ACR login failed."


# Build and push Containers

Write-Host "`nBuilding Weather API container..." -ForegroundColor Cyan

docker build `
    --file $ApiDockerfile `
    --tag $ApiImage `
    $ExternalApiPath

Assert-NativeCommand "Weather API Docker build failed."

Write-Host "`nBuilding Weather Functions container..." -ForegroundColor Cyan

docker build `
    --file $FunctionsDockerfile `
    --tag $FunctionsImage `
    $RepositoryRoot

Assert-NativeCommand "Weather Functions Docker build failed."

Write-Host "`nPushing Weather API image..." -ForegroundColor Cyan

docker push $ApiImage
Assert-NativeCommand "Weather API image push failed."

Write-Host "`nPushing Weather Functions image..." -ForegroundColor Cyan

docker push $FunctionsImage
Assert-NativeCommand "Weather Functions image push failed."


# Deploy Azure infrastructure

Write-Host "`nDeploying Azure infrastructure..." -ForegroundColor Cyan

$mainDeployment = Invoke-AzJson -Arguments @(
    "deployment", "group", "create",
    "--name", "weatherapp-containers",
    "--resource-group", $ResourceGroup,
    "--template-file", $MainTemplate,
    "--parameters",
    "location=$Location",
    "prefix=$Prefix",
    "registryName=$RegistryName",
    "apiImage=$ApiImage",
    "functionsImage=$FunctionsImage",
    "--output", "json"
) -FailureMessage "Main infrastructure deployment failed."


# Extract outputs

$outputs = $mainDeployment.properties.outputs

$ApiAppName = [string]$outputs.apiContainerAppName.value
$FunctionsAppName = [string]$outputs.functionsContainerAppName.value
$StorageAccountName = [string]$outputs.storageAccountName.value
$EnvironmentName = [string]$outputs.containerAppsEnvironmentName.value

$ApiFqdn = [string]$outputs.apiFqdn.value
$FunctionsFqdn = [string]$outputs.functionsFqdn.value

if ([string]::IsNullOrWhiteSpace($ApiAppName)) {
    throw "API Container App name is missing."
}

if ([string]::IsNullOrWhiteSpace($FunctionsAppName)) {
    throw "Functions Container App name is missing."
}

if ([string]::IsNullOrWhiteSpace($StorageAccountName)) {
    throw "Storage Account name is missing."
}


# Configure Unsplash API key

Write-Host "`nUnsplash API configuration..." -ForegroundColor Cyan

$unsplashSecure = Read-Host `
    "Enter Unsplash API key (press Enter to skip)" `
    -AsSecureString

$unsplashPtr = [IntPtr]::Zero

try {

    if ($unsplashSecure.Length -gt 0) {

        $unsplashPtr = [Runtime.InteropServices.Marshal]::SecureStringToBSTR(
            $unsplashSecure
        )

        $unsplashPlain = [Runtime.InteropServices.Marshal]::PtrToStringBSTR(
            $unsplashPtr
        )

        az containerapp secret set `
            --name $ApiAppName `
            --resource-group $ResourceGroup `
            --secrets "unsplashapikey=$unsplashPlain" `
            --output none

        Assert-NativeCommand "Setting Unsplash secret on API failed."

        az containerapp secret set `
            --name $FunctionsAppName `
            --resource-group $ResourceGroup `
            --secrets "unsplashapikey=$unsplashPlain" `
            --output none

        Assert-NativeCommand "Setting Unsplash secret on Functions failed."

        $SecretVersion = Get-Date -Format "yyyyMMddHHmmss"

        az containerapp update `
            --name $ApiAppName `
            --resource-group $ResourceGroup `
            --set-env-vars `
                "UnsplashApiKey=secretref:unsplashapikey" `
                "SECRET_VERSION=$SecretVersion" `
            --output none

        Assert-NativeCommand "Updating API environment variables failed."

        az containerapp update `
            --name $FunctionsAppName `
            --resource-group $ResourceGroup `
            --set-env-vars `
                "UnsplashApiKey=secretref:unsplashapikey" `
                "SECRET_VERSION=$SecretVersion" `
            --output none

        Assert-NativeCommand "Updating Functions environment variables failed."

        Write-Host "Unsplash key configured on both Container Apps." -ForegroundColor Green
        Write-Host "New revisions triggered for API and Functions." -ForegroundColor Green
    }
    else {
        Write-Host "Skipping Unsplash configuration." -ForegroundColor Yellow
    }
}
finally {

    if ($unsplashPtr -ne [IntPtr]::Zero) {
        [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($unsplashPtr)
    }

    $unsplashPlain = $null
}


# Summary of deployment

Write-Host " Deployment completed" -ForegroundColor Green


Write-Host "`nResource Group: $ResourceGroup"
Write-Host "Location: $Location"

Write-Host "`nContainer Registry: $RegistryServer"

Write-Host "`nAPI Container:"
Write-Host "  Image: $ApiImage"
Write-Host "  App: $ApiAppName"

if ($ApiFqdn) {
    Write-Host "  URL: https://$ApiFqdn"
}

Write-Host "`nFunctions Container:"
Write-Host "  Image: $FunctionsImage"
Write-Host "  App: $FunctionsAppName"

if ($FunctionsFqdn) {
    Write-Host "  URL: https://$FunctionsFqdn"
}

Write-Host "`nContainer Apps Environment: $EnvironmentName"
Write-Host "Storage Account: $StorageAccountName"