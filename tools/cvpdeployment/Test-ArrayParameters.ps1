


$subscriptionId = "51dab48c-8ef0-4acc-9710-d563d292ce2d"
$resourceGroupName = "self-host-prep"
#$templatePath = "C:\repos\connected-vehicle-samples\tools\cvpdeployment\ArrayParameterExperimentation.json"
$templatePath = "C:\repos\connected-vehicle-samples\tools\cvpdeployment\MultiExtensionCVPARMTemplate.json"

Set-AzContext -SubscriptionId $subscriptionId
$allFuncApps = Get-AzFunctionApp -ResourceGroupName $resourceGroupName

$extensionFunctionIds = $allFuncApps | Where-Object { $_.Name -match "ext" } | Select-Object -ExpandProperty Id
$claimsFuncApp = $allFuncApps | Where-Object { $_.Name -match "claim" } | Select-Object -First 1

$claimsProviderUri = "https://$($claimsFuncApp.DefaultHostName)"

$armParameters = @{
    "platformAccountName" = "self-host-test"
    "location" = "westus2"
    "extensionResourceIds" = $extensionFunctionIds
    "claimsProviderUri" = $claimsProviderUri
}

New-AzResourceGroupDeployment -ResourceGroupName $resourceGroupName -TemplateUri $templatePath -TemplateParameterObject $armParameters
