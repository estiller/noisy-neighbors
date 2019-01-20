# Sign in to your Azure account
$SubscriptionId = Read-Host "Please enter a subscription ID"
Connect-AzureRmAccount -Subscription $SubscriptionId

. '.\Scripts\1 - CreateJobDatabase.ps1'
. '.\Scripts\2 - CreateTargetPool.ps1'
. '.\Scripts\3 - CreateJobAgentAndCredentials.ps1'
. '.\Scripts\4 - CreateTargetGroup.ps1'
. '.\Scripts\5 - CreateJob.ps1'
. '.\Scripts\6 - MonitorJob.ps1'