Param(
    [string]$ResourceGroupName = "ElasticPoolSessionDemo",
    [string]$Location = "westeurope",
    [string]$ServerName = "elasticpoolsessiondemo",
    [string]$FirewallStartIpAddress = "0.0.0.0",
    [string]$FirewallEndIpAddress = "255.255.255.255"
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

# Create a new logical server
Write-Host 'Creating a new logical SQL Server'
$adminCredentials = Get-Credential -Message 'Please enter credentials for the SQL Server admin'
$server = New-AzureRmSqlServer -ResourceGroupName $ResourceGroupName `
    -ServerName $ServerName `
    -Location $Location `
    -SqlAdministratorCredentials $adminCredentials
$serverFirewallRule = New-AzureRmSqlServerFirewallRule -ResourceGroupName $ResourceGroupName `
    -ServerName $ServerName `
    -FirewallRuleName "AllowedIPs" -StartIpAddress $FirewallStartIpAddress -EndIpAddress $FirewallEndIpAddress

# Create an elastic database pool
Write-Host 'Creating an elastic database pool'
$poolName = "MyElasticPool"
$pool = New-AzureRmSqlElasticPool -ResourceGroupName $ResourceGroupName `
    -ServerName $ServerName `
    -ElasticPoolName $poolName `
    -Edition "Standard" `
    -Dtu 50 `
    -DatabaseDtuMin 10 `
    -DatabaseDtuMax 20

