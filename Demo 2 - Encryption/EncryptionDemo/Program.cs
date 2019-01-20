using System;
using System.Configuration;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.KeyVault;
using Microsoft.Azure.KeyVault.Core;
using Microsoft.Azure.KeyVault.WebKey;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.Blob;

namespace EncryptionDemo
{
    class Program
    {
        private static readonly string KeyVaultUrl = $"https://{ConfigurationManager.AppSettings["keyVaultName"]}.vault.azure.net";

        private static string GetKeyUrl(string keyName) => $"{KeyVaultUrl}/keys/{keyName}";

        static async Task Main()
        {
            TenantInfo tenant1 = await CreateTenantAsync(Guid.NewGuid());
            TenantInfo tenant2 = await CreateTenantAsync(Guid.NewGuid());
            TenantInfo tenant3 = await CreateTenantAsync(Guid.NewGuid());

            await UploadAndDownloadBlobAsync(tenant1);
        }

        private static async Task<TenantInfo> CreateTenantAsync(Guid id)
        {
            var containerName = $"tenant-{id}";
            await CreateBlobContainerAsync(containerName);

            var keyName = $"key-{id}";
            var keyVaultClient = new KeyVaultClient(GetTokenAsync);
            await keyVaultClient.CreateKeyAsync(KeyVaultUrl, keyName, JsonWebKeyType.Rsa);

            return new TenantInfo(id, containerName, keyName);
        }

        private static async Task CreateBlobContainerAsync(string containerName)
        {
            await GetBlobContainerAsync(containerName);
        }

        private static async Task<CloudBlobContainer> GetBlobContainerAsync(string containerName)
        {
            var creds = new StorageCredentials(ConfigurationManager.AppSettings["accountName"], ConfigurationManager.AppSettings["accountKey"]);
            var account = new CloudStorageAccount(creds, true);
            CloudBlobClient client = account.CreateCloudBlobClient();
            CloudBlobContainer container = client.GetContainerReference(containerName);
            await container.CreateIfNotExistsAsync();
            return container;
        }

        private static async Task UploadAndDownloadBlobAsync(TenantInfo tenant)
        {
            var container = await GetBlobContainerAsync(tenant.ContainerName);

            var cloudResolver = new KeyVaultKeyResolver(GetTokenAsync);
            IKey rsa = await cloudResolver.ResolveKeyAsync(GetKeyUrl(tenant.KeyName), CancellationToken.None);

            CloudBlob blob = await UploadBlobAsync(container, rsa);

            await DownloadBlobAsync(blob, cloudResolver);
        }

        private static async Task<CloudBlockBlob> UploadBlobAsync(CloudBlobContainer container, IKey rsa)
        {
            BlobEncryptionPolicy policy = new BlobEncryptionPolicy(rsa, null);
            BlobRequestOptions options = new BlobRequestOptions { EncryptionPolicy = policy };

            CloudBlockBlob blob = container.GetBlockBlobReference("MyFile.txt");
            using (var stream = File.OpenRead("Data.txt"))
            {
                 await blob.UploadFromStreamAsync(stream, stream.Length, null, options, null);
            }
            return blob;
        }

        private static async Task DownloadBlobAsync(CloudBlob blob, KeyVaultKeyResolver cloudResolver)
        {
            BlobEncryptionPolicy policy = new BlobEncryptionPolicy(null, cloudResolver);
            BlobRequestOptions options = new BlobRequestOptions {EncryptionPolicy = policy};

            using (var np = File.Open("DownloadedData.txt", FileMode.Create))
            {
                await blob.DownloadToStreamAsync(np, null, options, null);
            }
        }

        private static async Task<string> GetTokenAsync(string authority, string resource, string scope)
        {
            var authContext = new AuthenticationContext(authority);
            ClientCredential clientCred = new ClientCredential(
                ConfigurationManager.AppSettings["clientId"],
                ConfigurationManager.AppSettings["clientSecret"]);
            AuthenticationResult result = await authContext.AcquireTokenAsync(resource, clientCred);

            if (result == null)
                throw new InvalidOperationException("Failed to obtain the JWT token");

            return result.AccessToken;
        }
    }
}
