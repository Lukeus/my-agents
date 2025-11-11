<#
.SYNOPSIS
    Deploys the AI Agents infrastructure to Azure using Bicep.

.DESCRIPTION
    This script deploys all Azure resources required for the AI Agents multi-agent framework including:
    - Azure Kubernetes Service (AKS)
    - Azure Container Registry (ACR)
    - Cosmos DB
    - Azure SQL Database
    - Event Hub
    - Service Bus
    - Key Vault
    - Application Insights

.PARAMETER Environment
    The target environment (dev, staging, prod)

.PARAMETER Location
    The Azure region for deployment (default: eastus)

.PARAMETER SubscriptionId
    The Azure subscription ID

.PARAMETER WhatIf
    Run the deployment in validation mode without making changes

.EXAMPLE
    .\Deploy-Infrastructure.ps1 -Environment dev -SubscriptionId "your-subscription-id"

.EXAMPLE
    .\Deploy-Infrastructure.ps1 -Environment prod -Location westus2 -WhatIf
#>

[CmdletBinding(SupportsShouldProcess)]
param(
    [Parameter(Mandatory = $true)]
    [ValidateSet('dev', 'staging', 'prod')]
    [string]$Environment,

    [Parameter(Mandatory = $false)]
    [string]$Location = 'eastus',

    [Parameter(Mandatory = $true)]
    [string]$SubscriptionId,

    [Parameter(Mandatory = $false)]
    [switch]$WhatIf
)

$ErrorActionPreference = 'Stop'

# Set script location
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$InfraDir = Split-Path -Parent $ScriptDir
$BicepDir = Join-Path $InfraDir 'bicep'
$ParametersFile = Join-Path $BicepDir "parameters\$Environment.parameters.json"
$MainBicepFile = Join-Path $BicepDir 'main.bicep'

Write-Host "================================================" -ForegroundColor Cyan
Write-Host "  AI Agents Infrastructure Deployment" -ForegroundColor Cyan
Write-Host "================================================" -ForegroundColor Cyan
Write-Host "Environment:     $Environment" -ForegroundColor Yellow
Write-Host "Location:        $Location" -ForegroundColor Yellow
Write-Host "Subscription ID: $SubscriptionId" -ForegroundColor Yellow
Write-Host "Parameters File: $ParametersFile" -ForegroundColor Yellow
Write-Host "================================================`n" -ForegroundColor Cyan

# Validate prerequisites
Write-Host "Validating prerequisites..." -ForegroundColor Green

# Check if Azure CLI is installed
try {
    $azVersion = az version --output json | ConvertFrom-Json
    Write-Host "✓ Azure CLI version: $($azVersion.'azure-cli')" -ForegroundColor Green
}
catch {
    Write-Error "Azure CLI is not installed. Please install from https://aka.ms/installazurecliwindows"
    exit 1
}

# Check if Bicep CLI is installed
try {
    $bicepVersion = az bicep version
    Write-Host "✓ Bicep CLI version: $bicepVersion" -ForegroundColor Green
}
catch {
    Write-Host "Installing Bicep CLI..." -ForegroundColor Yellow
    az bicep install
}

# Validate Bicep files exist
if (-not (Test-Path $MainBicepFile)) {
    Write-Error "Main Bicep file not found: $MainBicepFile"
    exit 1
}

if (-not (Test-Path $ParametersFile)) {
    Write-Error "Parameters file not found: $ParametersFile"
    exit 1
}

Write-Host "✓ All prerequisites validated`n" -ForegroundColor Green

# Login to Azure (if not already logged in)
Write-Host "Checking Azure authentication..." -ForegroundColor Green
$currentAccount = az account show --output json 2>$null | ConvertFrom-Json

if (-not $currentAccount) {
    Write-Host "Not logged in to Azure. Initiating login..." -ForegroundColor Yellow
    az login
}

# Set subscription
Write-Host "Setting active subscription..." -ForegroundColor Green
az account set --subscription $SubscriptionId

$currentSub = az account show --output json | ConvertFrom-Json
Write-Host "✓ Active subscription: $($currentSub.name) ($($currentSub.id))`n" -ForegroundColor Green

# Build Bicep file
Write-Host "Building Bicep template..." -ForegroundColor Green
try {
    az bicep build --file $MainBicepFile
    Write-Host "✓ Bicep build successful`n" -ForegroundColor Green
}
catch {
    Write-Error "Bicep build failed: $_"
    exit 1
}

# Validate deployment
Write-Host "Validating deployment template..." -ForegroundColor Green
$deploymentName = "agents-$Environment-$(Get-Date -Format 'yyyyMMdd-HHmmss')"

try {
    $validateCmd = "az deployment sub validate ``
        --name $deploymentName ``
        --location $Location ``
        --template-file '$MainBicepFile' ``
        --parameters '@$ParametersFile' ``
        --output json"
    
    $validation = Invoke-Expression $validateCmd | ConvertFrom-Json
    
    if ($validation.properties.provisioningState -eq 'Succeeded') {
        Write-Host "✓ Template validation successful`n" -ForegroundColor Green
    }
    else {
        Write-Error "Template validation failed: $($validation.properties.error.message)"
        exit 1
    }
}
catch {
    Write-Error "Template validation failed: $_"
    exit 1
}

# Deploy infrastructure
if ($WhatIf) {
    Write-Host "=== WHAT-IF MODE: No changes will be made ===" -ForegroundColor Yellow
    
    $whatIfCmd = "az deployment sub what-if ``
        --name $deploymentName ``
        --location $Location ``
        --template-file '$MainBicepFile' ``
        --parameters '@$ParametersFile'"
    
    Invoke-Expression $whatIfCmd
    Write-Host "`nWhat-If analysis complete. No changes were made." -ForegroundColor Yellow
}
else {
    Write-Host "Deploying infrastructure to Azure..." -ForegroundColor Green
    Write-Host "This may take 15-30 minutes...`n" -ForegroundColor Yellow
    
    if ($PSCmdlet.ShouldProcess("Azure Subscription $SubscriptionId", "Deploy infrastructure")) {
        try {
            $deployCmd = "az deployment sub create ``
                --name $deploymentName ``
                --location $Location ``
                --template-file '$MainBicepFile' ``
                --parameters '@$ParametersFile' ``
                --output json"
            
            $deployment = Invoke-Expression $deployCmd | ConvertFrom-Json
            
            if ($deployment.properties.provisioningState -eq 'Succeeded') {
                Write-Host "`n================================================" -ForegroundColor Green
                Write-Host "  Deployment Successful!" -ForegroundColor Green
                Write-Host "================================================" -ForegroundColor Green
                
                Write-Host "`nDeployment Outputs:" -ForegroundColor Cyan
                $deployment.properties.outputs.PSObject.Properties | ForEach-Object {
                    Write-Host "  $($_.Name): $($_.Value.value)" -ForegroundColor Yellow
                }
                
                Write-Host "`n✓ Infrastructure deployed successfully!" -ForegroundColor Green
            }
            else {
                Write-Error "Deployment failed with state: $($deployment.properties.provisioningState)"
                exit 1
            }
        }
        catch {
            Write-Error "Deployment failed: $_"
            exit 1
        }
    }
}

Write-Host "`nDeployment complete!" -ForegroundColor Green
