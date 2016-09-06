param
(
    [Parameter(Mandatory=$True)]
    [string] $SubscriptionId,

    [Parameter(Mandatory=$True)]
    [string] $ResourceGroupName,

    [Parameter(Mandatory=$True)]
    [string] $ResourceGroupLocation,

    [Parameter(Mandatory=$True)]
    [string] $TemplateFilePath,

    [Parameter(Mandatory=$True)]
    [string] $ParameterFilePath
)

Login-AzureRmAccount
Set-AzureRmContext -SubscriptionId $SubscriptionId
New-AzureRmResourceGroup -Name $ResourceGroupName -Location $ResourceGroupLocation
New-AzureRmResourceGroupDeployment -ResourceGroupName $ResourceGroupName -TemplateFile $TemplateFilePath -TemplateParameterFile $ParameterFilePath