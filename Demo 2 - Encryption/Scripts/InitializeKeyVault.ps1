Param(
    [string]$ResourceGroupName = "EncryptionSessionDemo",
    [string]$Location = "westeurope",
    [string]$KeyVaultName = "encryptionsessiondemo",
	[string]$ServicePrincipalName = "encryptionsessiondemo",
	[string]$ServicePrincipalPassword = "VerySecurePassword",
    [string]$StorageAccountName = "encryptionsessiondemo"
)

# Connect-AzureRmAccount

# Create a new resource group
$resourceGroup = Get-AzureRmResourceGroup -Name $ResourceGroupName -ErrorAction SilentlyContinue
if ($resourceGroup) {
    Write-Host 'Detected an existing resource group'
    Remove-AzureRmResourceGroup -Name $ResourceGroupName
}
Write-Host 'Creating a new resource group'
$resourceGroup = New-AzureRmResourceGroup -Name $ResourceGroupName -Location $Location

# Create a key vault
Write-Host 'Creating a new KeyVault'
$kv = New-AzureRmKeyVault -VaultName $KeyVaultName -ResourceGroupName $ResourceGroupName -Location $Location

# Create a Service Principal
Write-Host 'Creating a service principal'
$ServicePrincipalPasswordSecure = ConvertTo-SecureString -String $ServicePrincipalPassword -AsPlainText -Force
$sp = New-AzureRmADServicePrincipal -DisplayName $ServicePrincipalName -Password $ServicePrincipalPasswordSecure
Sleep 20
Write-Host "Application ID is: $($sp.ApplicationId)"

Write-Host 'Provide access to SP on KeyVault'
Set-AzureRmKeyVaultAccessPolicy -VaultName $KeyVaultName -ServicePrincipalName $sp.ServicePrincipalNames[0] -PermissionsToKeys all

# Create a storage account
Write-Host 'Creating a storage account'
New-AzureRmStorageAccount -ResourceGroupName $ResourceGroupName -Location $Location -Name $StorageAccountName -SkuName Standard_LRS -Kind BlobStorage -AccessTier Hot
$key = Get-AzureRmStorageAccountKey -ResourceGroupName $ResourceGroupName -Name $StorageAccountName
Write-Host "Storage account key is: $($key[0].Value)"