# Create a target server and some sample databases - uses the same admin credential as the agent server just for simplicity
Write-Output "Creating target server..."
$TargetServerName = "TargetServer-" + [guid]::NewGuid()
$TargetServer = New-AzureRmSqlServer -ResourceGroupName $ResourceGroupName -Location $Location -ServerName $TargetServerName -ServerVersion "12.0" -SqlAdministratorCredentials ($AdminCred)

# Set target server firewall rules to allow all Azure IPs
$TargetServer | New-AzureRmSqlServerFirewallRule -AllowAllAzureIPs
$TargetServer | New-AzureRmSqlServerFirewallRule -StartIpAddress 0.0.0.0 -EndIpAddress 255.255.255.255 -FirewallRuleName AllowAll
$TargetServer

# Create an elastic database pool
Write-Host 'Creating an elastic database pool'
$ElasticPoolName = "ElasticPool"
$ElasticPool = New-AzureRmSqlElasticPool -ResourceGroupName $ResourceGroupName `
    -ServerName $TargetServerName `
    -ElasticPoolName $ElasticPoolName `
    -Edition "Standard" `
    -Dtu 50 `
    -DatabaseDtuMin 10 `
    -DatabaseDtuMax 20

# Create some sample databases in pool to execute jobs against...
$TenantDbs = @()
For ($i=0; $i -lt 4; $i++)
{
    $TenantDb = New-AzureRmSqlDatabase -ResourceGroupName $ResourceGroupName -ServerName $TargetServerName -DatabaseName "TargetDb$i" -ElasticPoolName $ElasticPoolName
    $TenantDbs += $TenantDb
}
