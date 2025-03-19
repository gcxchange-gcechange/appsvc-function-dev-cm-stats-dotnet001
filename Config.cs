using Microsoft.Extensions.Configuration;

namespace appsvc_function_dev_cm_stats_dotnet001
{
    internal class Config
    {
        private IConfiguration _Config;

        public string AzureWebJobsStorage { get { return _Config["AzureWebJobsStorage"]; } }
        public string ClientId { get { return _Config["clientId"]; } }
        public string ContainerName { get { return _Config["containerName"]; } }
        public string PropertyId { get { return _Config["propertyId"]; } }
        public string KeyFileName { get { return _Config["keyFileName"]; } }
        public string KeyVaultUrl { get { return _Config["keyVaultUrl"]; } }
        public string ListId { get { return _Config["listId"]; } }
        public string SecretName { get { return _Config["secretName"]; } }
        public string SecretNamePassword { get { return _Config["delegatedUserSecret"]; } }
        public string SiteId { get { return _Config["siteId"]; } }
        public string TenantId { get { return _Config["tenantId"]; } }
        public string Username { get { return _Config["delegatedUserName"]; } }

        public Config()
        {
            _Config = new ConfigurationBuilder().AddJsonFile("appsettings.json", optional: true, reloadOnChange: true).AddEnvironmentVariables().Build();
        }
    }
}
