[cmdletbinding()]
PARAM (
    [parameter(mandatory = $true)]
    [string] $SubscriptionId,

    [parameter(mandatory = $true)]
    [string] $ResourceGroupName,

    [parameter(mandatory = $true)]
    [string] $FunctionAppName,

    [parameter(mandatory = $false)]
    [switch] $CreateFunctionApp
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
if ($CreateFunctionApp)
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