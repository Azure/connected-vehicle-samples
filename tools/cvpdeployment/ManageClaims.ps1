<#
 .SYNOPSIS
    Manages creating, updating, and reading claims from the Claims Provider

 .DESCRIPTION
    Manages creating/updating, reading and deleting claims from the Claims Provider

 .PARAMETER SubscriptionId
    The subscription id of the resource group that MCVP is deployed in

 .PARAMETER ResourceGroupName
    The name of the resource group that MCVP is deployed in

 .PARAMETER Mode
    The action to be performed on the claim. Options are "upsert", "read", and "delete"

 .PARAMETER VehicleId
    The VehicleId to be associated with the claim

 .PARAMETER UserId
    The UserId to be associated with the claim

 .PARAMETER ServiceId
    The ServiceId to be associated with the claim

 .PARAMETER ClaimName
    The name of the claim. A claim is a key/value pair, and the name is the key

 .PARAMETER ClaimValue
    The value of the claim. A claim is a key/value pair, and the value is this particular value

 .PARAMETER EnvironmentName
    The Azure Environment that the script will be executing against.   Defaults to AzureCloud

 .PARAMETER Automation
    Optional, support for suppression of Azure Login prompts.  The current Az Context will be used.   The script will switch to the subscription specified in the subscriptionid param
#>

param(
    [Parameter(Mandatory = $true)]
    [string] $SubscriptionId,

    [Parameter(Mandatory = $true)]
    [string] $ResourceGroupName,

    [Parameter(Mandatory = $true)]
    [string] $Mode,

    [Parameter(Mandatory = $false)]
    [string] $VehicleId = "",

    [Parameter(Mandatory = $false)]
    [string] $UserId = "",

    [Parameter(Mandatory = $false)]
    [string] $ServiceId = "",

    [Parameter(Mandatory = $false)]
    [string] $ClaimName = "",

    [Parameter(Mandatory = $false)]
    $ClaimValue = $null,

    [ValidateSet('AzureCloud','AzureChinaCloud')]
    [string] $EnvironmentName = "AzureCloud",

    [Parameter(Mandatory = $false)]
    [bool] $Automation = $true
)

#******************************************************************************
#
#                                IMPORTS
#
#******************************************************************************

$modulePath = $PSScriptRoot + "\..\infra\deployment\IotCCDevSetup.psd1"
if (!(Test-Path $modulePath))
{
   $modulePath = $PSScriptRoot + "\..\packages\Microsoft.Azure.ConnectedCar.*\IotCCDevSetup.psd1"
}

Import-Module -Name $modulePath -Force

#******************************************************************************
#
#                                 SCRIPT BEGIN
#
#******************************************************************************

Invoke-ValidateStartContext -SubscriptionId $SubscriptionId -Automation $Automation -RequireAdmin $false -EnvironmentName $EnvironmentName

$keyVaultName = Get-DevSetupKeyVaultName -SubscriptionId $SubscriptionId -ResourceGroupName $ResourceGroupName
$resource = Get-AzResource -ResourceGroupName $ResourceGroupName -ResourceName $keyVaultName

if ($resource -eq $null)
{
    $keyVaultName = Get-KeyVaultName -ResourceGroupName $ResourceGroupName
}

$claimsProviderDatabase = Get-AzKeyVaultSecretAsPlainText -VaultName $KeyVaultName -Name "ClaimsProviderDocDb-Database"
$claimsProviderCollection = Get-AzKeyVaultSecretAsPlainText -VaultName $KeyVaultName -Name "ClaimsProviderCollection"
$claimsCosmosDbConnectionStringPrimary = Get-AzKeyVaultSecretAsPlainText -VaultName $KeyVaultName -Name "Claims-DocumentDb-ConnectionString-Primary"

# Set up Claims params
$VehicleIdParameter = ""
if ($VehicleId)
{
   $VehicleIdParameter = "-v $VehicleId"
}

$UserIdParameter = ""
if ($UserId)
{
   $UserIdParameter = "-u $UserId"
}

$ServiceIdParameter = ""
if ($ServiceId)
{
   $ServiceIdParameter = "-s $ServiceId"
}

$ClaimNameParameter = ""
if ($ClaimName)
{
   $ClaimNameParameter = "-cn $ClaimName"
}

$ClaimValueParameter = ""
# Allows for specifying "" as $ClaimValue
if ($ClaimValue -eq "")
{
   $ClaimValueParameter = '-cv ""'
}
elseif ($ClaimValue)
{
    $ClaimValueParameter = "-cv $ClaimValue"
}

$filePath = $PSScriptRoot + "\Utilities\SampleClaims\bin\x64\Debug\Microsoft.Azure.ConnectedCar.SampleClaims.exe"
$argumentList = "$VehicleIdParameter $UserIdParameter $ServiceIdParameter $ClaimNameParameter $ClaimValueParameter -m $Mode -d $claimsProviderDatabase -c $claimsProviderCollection -p $claimsCosmosDbConnectionStringPrimary"
Write-Host "------------------------------------------------------------------"
$process = Start-Process -FilePath $filePath -ArgumentList $argumentList -NoNewWindow -PassThru -Wait
$process.WaitForExit()