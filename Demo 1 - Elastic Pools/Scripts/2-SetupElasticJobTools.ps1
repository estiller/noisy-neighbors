Param(
    [string]$Location = "westeurope"
)

# Login-AzureRmAccount

Invoke-WebRequest 'https://nuget.org/nuget.exe' -OutFile 'nuget.exe'
.\nuget.exe install Microsoft.Azure.SqlDatabase.Jobs -prerelease
$dir = Get-ChildItem -Directory -Filter 'Microsoft.Azure.SqlDatabase.Jobs.*'
&".\$dir\tools\InstallElasticDatabaseJobs.ps1" -ResourceGroupLocation $Location