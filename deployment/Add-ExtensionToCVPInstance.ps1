<#
.SYNOPSIS
    Adds one or more new extension apps to an existing CVP instance.

.DESCRIPTION
    Adds one or more new extension apps to an existing CVP instance without modifying any existing properties or removing existing extensions.
    This will update your CVP instance with all the same properties as it currently has, including currently deployed extensions, and will add the extension resource ids provided.

    Additionally, your CVP instance will be granted Contributor access over the extensions provided.

.PARAMETER SubscriptionId
    The subscription id of the existing CVP instance.

.PARAMETER ResourceGroupName
    The resource group name of the existing CVP instance.

.PARAMETER PlatformAccountName
    The name of the existing CVP instance.

.PARAMETER ExtensionResourceIds
    One or more, comma seperated, resouce ids of the extensions to add to the existing CVP instance.
    A resource id is of the format "/subscriptions/{guid}/resourceGroups/{resource-group-name}/{resource-provider-namespace}/{resource-type}/{resource-name}"
    For a standard azure function an example is "/subscriptions/00000000-1111-2222-3333-444444444444/resourceGroups/contoso-rg/Microsoft.Web/sites/my-extensions"

.EXAMPLE
    To add a single additional extension to an existing CVP instance.

    PS C:\> ./Deploy-SampleClaimsProvider.ps1 -SubscriptionId 00000000-1111-2222-3333-444444444444 -ResourceGroupName contoso-rg -PlatformAccountName contoso-cvp -ExtensionResourceIds /subscriptions/00000000-1111-2222-3333-444444444444/resourceGroups/contoso-rg/Microsoft.Web/sites/my-extensions

.EXAMPLE
    To add a multiple additional extensions to an existing CVP instance.

    PS C:\> ./Deploy-SampleClaimsProvider.ps1 -SubscriptionId 00000000-1111-2222-3333-444444444444 -ResourceGroupName contoso-rg -PlatformAccountName contoso-cvp -ExtensionResourceIds /subscriptions/00000000-1111-2222-3333-444444444444/resourceGroups/contoso-rg/Microsoft.Web/sites/my-extensions1, /subscriptions/00000000-1111-2222-3333-444444444444/resourceGroups/contoso-rg/Microsoft.Web/sites/my-extensions2
#>
[cmdletbinding()]
PARAM (
    [parameter(mandatory = $true)]
    [string] $SubscriptionId,

    [parameter(mandatory = $true)]
    [string] $ResourceGroupName,

    [parameter(mandatory = $true)]
    [string] $PlatformAccountName,

    [parameter(mandatory = $true)]
    [string[]] $ExtensionResourceIds
)

$ErrorActionPreference = "Stop"
$templatePath = $PSScriptRoot + "\MultiExtensionCVPARMTemplate.json"
$diagnosticLogTemplatePath = $PSScriptRoot + "\DiagnosticLogsArmTemplate.json"

Set-AzContext -SubscriptionId $SubscriptionId

# Verify the given resources actually exist
$cvpResourceType = "Microsoft.ConnectedVehicle/platformAccounts"
$cvpInstance = Get-AzResource -ResourceGroupName $ResourceGroupName -Name $PlatformAccountName -ResourceType $cvpResourceType
if ($null -eq $cvpInstance)
{
    throw "Unable to find platform account $PlatformAccountName in resource group $ResourceGroupName"
}

$ExtensionResourceIds | ForEach-Object {
    $extensionResource = Get-AzResource -ResourceId $_ -ErrorAction Continue
    if ($null -eq $extensionResource)
    {
        throw "Unable to find extension resource $_"
    }
}

# Merge existing extensions with new extensions
$existingExtensionResourceIds = $cvpInstance.Properties.extensions | Select-Object -ExpandProperty resourceId

$allUniqueExtensions = [System.Collections.Generic.HashSet[string]]($existingExtensionResourceIds)
$allUniqueExtensions.UnionWith($ExtensionResourceIds)

# Setup ARM parameters, maintaining the configuration of the existing instance
$analyticsStorageAccount = Get-AzResource -ResourceId $cvpInstance.Properties.analytics.storageAccountResourceId 
$armParameters = @{
    "platformAccountName" = $cvpInstance.ResourceName 
    "location" = $cvpInstance.Location 
    "platformAccountSku" = $cvpInstance.Sku.Name
    "connectedVehicleStorageAccountType" = $analyticsStorageAccount.Sku.Name
    "analyticsStorageAccountName" = $analyticsStorageAccount.Name
    "enablePrivateEndpointConnectionToAnalytics" = $cvpInstance.Properties.analytics.enablePrivateEndpointConnection
    "claimsProviderUri" = $cvpInstance.Properties.claimsProvider.baseUrl
    "extensionResourceIds" = $allUniqueExtensions
}

New-AzResourceGroupDeployment -ResourceGroupName $resourceGroupName -TemplateUri $templatePath -TemplateParameterObject $armParameters

$armParameters = @{
    "platformAccountName" = $cvpInstance.ResourceName 
    "location" = $cvpInstance.Location 
}

New-AzResourceGroupDeployment -ResourceGroupName $resourceGroupName -TemplateUri $diagnosticLogTemplatePath -TemplateParameterObject $armParameters