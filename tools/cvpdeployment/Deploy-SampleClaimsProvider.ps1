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

Set-AzContext -SubscriptionId $SubscriptionId
$samplesClaimsProviderPath = $PSScriptRoot + "/SamplesClaimsProvider"

$rg = Get-AzResourceGroup -Name $ResourceGroupName
if ($null -eq $rg)
{
    throw "Resource group $ResourceGroupName does not exist"
}

$functionApp = Get-AzFunctionApp -ResourceGroupName $ResourceGroupName -Name $FunctionAppName
if ($null -eq $functionApp)
{
    if (!$CreateFunctionApp)
    {
        throw "Function app $FunctionAppName does not exist in $ResourceGroupName"
    }

    $functionAppArmTemplatePath = $PSScriptRoot + "/FunctionAppARMTemplate.json"
    $armParameters = @{
        "appName" = $FunctionAppName
    }
    New-AzResourceGroupDeployment -ResourceGroupName $ResourceGroupName -TemplateUri $functionAppArmTemplatePath -TemplateParameterObject $armParameters
}

$samplesClaimsProviderPath = $PSScriptRoot + "/SamplesClaimsProvider"
# Build Sample Claims Provider
