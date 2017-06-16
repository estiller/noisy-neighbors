# Login-AzureRmAccount

Use-AzureSqlJobConnection -CurrentAzureSubscription

$credentialName = "MyCredential"
$credential = Get-AzureSqlJobCredential -CredentialName $credentialName
if (!$credential)
{
    $databaseCredential = Get-Credential -Message "Enter target credential"
    $credential = New-AzureSqlJobCredential -Credential $databaseCredential -CredentialName $credentialName
}
Write-Output $credential

$scriptName = "Create a TestTable"
$scriptCommandText = "
IF NOT EXISTS (SELECT name FROM sys.tables WHERE name = 'TestTable')
BEGIN
    CREATE TABLE TestTable(
        TestTableId INT PRIMARY KEY IDENTITY,
        InsertionTime DATETIME2
    );
END
GO
INSERT INTO TestTable(InsertionTime) VALUES (sysutcdatetime());
GO"
$script = Get-AzureSqlJobContent -ContentName $scriptName
if (!$script)
{
    $script = New-AzureSqlJobContent -ContentName $scriptName -CommandText $scriptCommandText
}
Write-Output $script

$jobName = "MyJob"
$shardMapServerName = "elasticpoolsessiondemo.database.windows.net"
$shardMapDatabaseName = "ElasticPools_ShardMapManagerDb"
$shardMapName = "CustomerIDShardMap"
try {
    $shardMapTarget = Get-AzureSqlJobTarget -ShardMapManagerServerName $shardMapServerName -ShardMapManagerDatabaseName $shardMapDatabaseName -ShardMapName $shardMapName 
}
catch {
    $shardMapTarget = New-AzureSqlJobTarget -ShardMapManagerServerName $shardMapServerName -ShardMapManagerDatabaseName $shardMapDatabaseName -ShardMapName $shardMapName -ShardMapManagerCredentialName $credential
}
$job = New-AzureSqlJob -ContentName $scriptName -CredentialName $credentialName -JobName $jobName -TargetId $shardMapTarget.TargetId
Write-Output $job

$jobExecution = Start-AzureSqlJobExecution -JobName $jobName 
Write-Output $jobExecution

$jobExecutions = Get-AzureSqlJobExecution -JobExecutionId $jobExecution.JobExecutionId -IncludeChildren
Write-Output $jobExecutions