Write-Output "Creating test target groups..."
# Create ElasticGroup target group
$ElasticGroup = $JobAgent | New-AzureRmSqlElasticJobTargetGroup -Name 'ElasticGroup'
$ElasticGroup | Add-AzureRmSqlElasticJobTarget -ServerName $TargetServerName -ElasticPoolName $ElasticPool.Name -RefreshCredentialName $MasterCred.CredentialName
