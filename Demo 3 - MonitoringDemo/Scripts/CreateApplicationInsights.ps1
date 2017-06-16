Param(
    [string]$ResourceGroupName = "MonitoringDemo",
    [string]$Location = "westeurope",
    [string]$AppInsightsName = "monitoringdemo"
)

# Login-AzureRmAccount

# Create a new resource group
$resourceGroup = Get-AzureRmResourceGroup -Name $ResourceGroupName -ErrorAction SilentlyContinue
if ($resourceGroup) {
    Write-Host 'Detected an existing resource group'
    Remove-AzureRmResourceGroup -Name $ResourceGroupName
}
Write-Host 'Creating a new resource group'
$resourceGroup = New-AzureRmResourceGroup -Name $ResourceGroupName -Location $Location

# Create Application Insights
Write-Host 'Creating Application Insights'
$resource = New-AzureRmResource `
  -ResourceName $AppInsightsName `
  -ResourceGroupName $ResourceGroupName `
  -Tag @{ applicationType = "web"; applicationName = 'MonitoringDemo'} `
  -ResourceType "Microsoft.Insights/components" `
  -Location $Location `
  -PropertyObject @{"Application_Type"="web"} `
  -Force

# Display iKey
Write-Host "App Insights Name = " $resource.Name
Write-Host "IKey = " $resource.Properties.InstrumentationKey