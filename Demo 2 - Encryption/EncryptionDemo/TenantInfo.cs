using System;

namespace EncryptionDemo
{
    public class TenantInfo
    {
        public TenantInfo(Guid id, string containerName, string keyName)
        {
            Id = id;
            ContainerName = containerName;
            KeyName = keyName;
        }

        public Guid Id { get; }

        public string ContainerName { get; }

        public string KeyName { get; }
    }
}