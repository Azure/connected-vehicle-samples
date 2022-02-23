<#
.SYNOPSIS
    Deploys the sample claims provider to the given function app. If -CreateClaimsProviderResources is provided it will deploy a new function app and cosmos db to use for the sample claims provider.

.DESCRIPTION
    Deploys the sample claims provider to the given function app. The sample claims provider requires a Cosmos DB instance to run and the function app it is deployed to must have the application setting 'ConnectionStrings__CosmosDBConnectionString' set with a connection string to that CosmosDB instance.
    If the -CreateClaimsProviderResources parameter is provided then an ARM template will be ran deploying a new function app and Cosmos DB instance. In addition it will configure the connection strings between the function app and the Cosmos DB.

.PARAMETER SubscriptionId
    The id of the subscription to deploy the sample claims provider to.

.PARAMETER ResourceGroupName
    The name of the resource group to deploy the sample claims provider to.

.PARAMETER FunctionAppName
    The name of the function app to deploy the sample claims provider to.
    If -CreateFunctionApp is provided this will be the name of the newly created function app and supporting resources.

.PARAMETER CreateClaimsProviderResources
    If provided, an ARM template will run deploying a new function app, cosmos db, and required supporting resources.
    In addition the function app will be configured appropriately with a connection string to the cosmos db instance.
    The sample claims provider will then be deployed to this newly created function app

.EXAMPLE
    The simplest use case starts with an existing resource group and no function app deployed.
    This example will create a new function app called 'sample-claims-provider' in the 'contoso-rg' under the subscription 0d82dd27-098f-414c-9259-2436d030609d.
    In addition all supporting resources, such as a cosmos db, are created and the sample claims provider will be deployed to the newly created 'smaple-claims-provider' function app.
    Once deployed the sample claims provider should be fully functional.

    PS C:\> ./Deploy-SampleClaimsProvider.ps1 -SubscriptionId 00000000-1111-2222-3333-444444444444 -ResourceGroupName contoso-rg -FunctionAppName sample-claims-provider -CreateClaimsProviderResources

.EXAMPLE
    If you already have a function app setup that you would prefer to use, you can omit the -CreateClaimsProviderResources paramter.
    In this case the sample claims provider will be deployed to the function app 'my-existing-function-app'. No additional setup or configuration will be done.

    PS C:\> ./Deploy-SampleClaimsProvider.ps1 -SubscriptionId 00000000-1111-2222-3333-444444444444 -ResourceGroupName contoso-rg -FunctionAppName my-existing-function-app
#>
[cmdletbinding()]
PARAM (
    [parameter(mandatory = $true)]
    [string] $SubscriptionId,

    [parameter(mandatory = $true)]
    [string] $ResourceGroupName,

    [parameter(mandatory = $true)]
    [string] $FunctionAppName,

    [parameter(mandatory = $false)]
    [switch] $CreateClaimsProviderResources
)
$ErrorActionPreference = "Stop"
Set-AzContext -SubscriptionId $SubscriptionId

# All paths we'll use
$sampleClaimsProviderPath = $PSScriptRoot + "/SampleClaimsProvider"
$sampleClaimsProviderSolutionPath = $PSScriptRoot + "/SampleClaimsProvider/SampleClaimsProvider.sln"
$sampleClaimsProviderBinariesPath = $sampleClaimsProviderPath + "/SampleClaimsProvider/bin/Debug/net6.0"
$zipPath = $PSScriptRoot + "/sampleClaimProvider.zip"
$functionAppArmTemplatePath = $PSScriptRoot + "/SampleClaimsProviderARMTemplate.json"

# Ensure resource group exists
$rg = Get-AzResourceGroup -Name $ResourceGroupName
if ($null -eq $rg)
{
    throw "Resource group $ResourceGroupName does not exist"
}

# Create function app if specified
if ($CreateClaimsProviderResources)
{
    Write-Host "Deploying claims provider function app and cosmos database"
    $armParameters = @{
        "appName" = $FunctionAppName
    }
    $deploymentOutputs = New-AzResourceGroupDeployment -ResourceGroupName $ResourceGroupName -TemplateUri $functionAppArmTemplatePath -TemplateParameterObject $armParameters

    # It can sometimes take a moment for the new function app to be recognized, if we move too quickly it won't be ready by the time we want to deploy to it
    $bailoutTime = (Get-Date).AddMinutes(2)
    $functionApp = Get-AzFunctionApp -ResourceGroupName $ResourceGroupName -Name $FunctionAppName
    while((Get-Date) -lt $bailoutTime -and $null -eq $functionApp)
    {
        Start-Sleep -s 5
        $functionApp = Get-AzFunctionApp -ResourceGroupName $ResourceGroupName -Name $FunctionAppName
    }
    
    if ($null -eq $functionApp)
    {
        throw "Timed out waiting for function app to finish deployment"
    }
}
elseif ($null -eq (Get-AzFunctionApp -ResourceGroupName $ResourceGroupName -Name $FunctionAppName))
{
    throw "Function app $FunctionAppName does not exist in $ResourceGroupName"
}


# Build Sample Claims Provider
if ((Test-Path $sampleClaimsProviderSolutionPath) -eq $false)
{
    throw "Unable to find SampleClaimsProvider.sln at $sampleClaimsProviderSolutionPath"
}

Write-Host "Building SampleClaimsProvider"
$buildResult = dotnet build $sampleClaimsProviderSolutionPath /p:Configuration=Debug /t:"Restore;Build" *>&1
if ($LASTEXITCODE -eq 0) {
    Write-Host ("Build completed successfully.")
}
else {
    throw ("Build failed.\n$($buildResult -join [System.Environment]::NewLine)")
}

# Package and Deploy Sample Claims Provider
try
{
    Write-Host "Packaging SampleClaimsProvider"
    if ((Test-Path $sampleClaimsProviderBinariesPath) -eq $false)
    {
        throw "Unable to find SampleClaimsProvider binaries at $sampleClaimsProviderPath"
    }
    Compress-Archive -Path "$sampleClaimsProviderBinariesPath/*" -Update -DestinationPath $zipPath

    Write-Host "Uploading SampleClaimsProvider to $FunctionAppName"
    $publishOutputs = Publish-AzWebApp -ResourceGroupName $ResourceGroupName -Name $FunctionAppName -ArchivePath $zipPath -Force
    Write-Host "Finished deploying SampleClaimsProvider"
}
finally 
{
    if ((Test-Path -Path $zipPath) -eq $true)
    {
        Remove-Item -Path $zipPath -Force
    }
}